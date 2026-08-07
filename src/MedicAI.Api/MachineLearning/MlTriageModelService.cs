using System.Text.Json;
using MedicAI.Api.Data;
using MedicAI.Api.Domain;
using MedicAI.Api.MachineLearning.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

namespace MedicAI.Api.MachineLearning;

public sealed class MlTriageModelService
{
    private readonly MLContext _mlContext = new(seed: 4242);
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<MlTriageModelService> _logger;
    private readonly object _predictionLock = new();
    private ITransformer? _model;

    public MlTriageModelService(IConfiguration configuration, IWebHostEnvironment environment, ILogger<MlTriageModelService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public bool IsAvailable => _model is not null;
    public int? ActiveModelVersionId { get; private set; }
    public string ModelVersionName { get; private set; } = "Unavailable";

    public async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        try
        {
            var modelPath = ResolvePath(_configuration["Triage:ModelPath"] ?? "MachineLearning/Artifacts/triage-model.zip");
            var trainingPath = ResolvePath(_configuration["Triage:TrainingDataPath"] ?? "MachineLearning/Data/triage-training-data.csv");
            var validationPath = Path.Combine(Path.GetDirectoryName(trainingPath)!, "doctor-validation-template.csv");
            var minimumRows = _configuration.GetValue<int?>("Triage:MinimumTrainingRows") ?? 1500;

            SyntheticTrainingDataGenerator.EnsureDatasets(trainingPath, minimumRows, validationPath);
            Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);

            string metricsJson;
            if (File.Exists(modelPath))
            {
                _model = _mlContext.Model.Load(modelPath, out _);
                metricsJson = JsonSerializer.Serialize(new { loadedExistingModel = true });
                _logger.LogInformation("Loaded ML.NET triage model from {ModelPath}", modelPath);
            }
            else
            {
                metricsJson = TrainAndSave(trainingPath, modelPath);
                _logger.LogInformation("Trained and saved ML.NET triage model to {ModelPath}", modelPath);
            }

            ModelVersionName = $"synthetic-sdca-v1-{File.GetLastWriteTimeUtc(modelPath):yyyyMMddHHmmss}";

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MedicAiDbContext>();
            var existing = await dbContext.AiModelVersions.FirstOrDefaultAsync(x => x.VersionName == ModelVersionName, cancellationToken);

            if (existing is null)
            {
                await dbContext.AiModelVersions.Where(x => x.IsActive).ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.IsActive, false), cancellationToken);

                existing = new AiModelVersion
                {
                    VersionName = ModelVersionName,
                    Algorithm = "ML.NET SDCA Maximum Entropy Multiclass",
                    FeatureSchemaJson = JsonSerializer.Serialize(new
                    {
                        features = new[]
                        {
                            "Age", "MainSymptom", "DurationHours", "PainSeverity", "Fever", "Vomiting",
                            "Dizziness", "WorseningSymptoms", "ExistingCondition", "MissingInformation"
                        },
                        classes = new[] { "Routine", "Priority", "Urgent" }
                    }),
                    MetricsJson = metricsJson,
                    FilePath = modelPath,
                    IsActive = true
                };
                dbContext.AiModelVersions.Add(existing);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else if (!existing.IsActive)
            {
                existing.IsActive = true;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            ActiveModelVersionId = existing.Id;
        }
        catch (Exception ex)
        {
            _model = null;
            _logger.LogError(ex, "The ML.NET triage model could not be initialised. Non-emergency submissions will require manual review.");
        }
    }

    public MlPredictionResult Predict(TriageModelInput input)
    {
        if (_model is null)
        {
            throw new InvalidOperationException("The ML.NET model is unavailable.");
        }

        TriageModelOutput output;
        lock (_predictionLock)
        {
            var engine = _mlContext.Model.CreatePredictionEngine<TriageModelInput, TriageModelOutput>(_model);
            output = engine.Predict(input);
        }

        var probabilities = Softmax(output.Score);
        var confidence = probabilities.Length == 0 ? 0m : (decimal)probabilities.Max();
        var factors = BuildFactors(input);

        return new MlPredictionResult(output.PredictedLabel, decimal.Round(confidence, 4), factors);
    }

    private string TrainAndSave(string trainingPath, string modelPath)
    {
        var data = _mlContext.Data.LoadFromTextFile<TriageModelInput>(trainingPath, hasHeader: true, separatorChar: ',', allowQuoting: true, trimWhitespace: true);
        var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.20, seed: 4242);

        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding("MainSymptomEncoded", "MainSymptom"))
            .Append(_mlContext.Transforms.Concatenate(
                "Features",
                "Age",
                "MainSymptomEncoded",
                "DurationHours",
                "PainSeverity",
                "Fever",
                "Vomiting",
                "Dizziness",
                "WorseningSymptoms",
                "ExistingCondition",
                "MissingInformation"))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        _model = pipeline.Fit(split.TrainSet);
        var predictions = _model.Transform(split.TestSet);
        var metrics = _mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "Label");
        _mlContext.Model.Save(_model, split.TrainSet.Schema, modelPath);

        return JsonSerializer.Serialize(new
        {
            metrics.MacroAccuracy,
            metrics.MicroAccuracy,
            metrics.LogLoss,
            PerClassLogLoss = metrics.PerClassLogLoss,
            trainingSource = "Synthetic ambiguous scenarios",
            limitation = "Internal synthetic performance is not independent clinical validation."
        });
    }

    private string ResolvePath(string path) => Path.IsPathRooted(path) ? path : Path.Combine(_environment.ContentRootPath, path);

    private static double[] Softmax(IReadOnlyList<float> scores)
    {
        if (scores.Count == 0) return Array.Empty<double>();
        var max = scores.Max();
        var exponents = scores.Select(score => Math.Exp(score - max)).ToArray();
        var sum = exponents.Sum();
        return sum <= 0 ? Array.Empty<double>() : exponents.Select(value => value / sum).ToArray();
    }

    private static IReadOnlyList<string> BuildFactors(TriageModelInput input)
    {
        var factors = new List<string>();
        if (input.PainSeverity >= 7) factors.Add("High reported pain severity");
        else if (input.PainSeverity >= 4) factors.Add("Moderate reported pain severity");
        if (input.WorseningSymptoms > 0) factors.Add("Symptoms reported as worsening");
        if (input.Fever > 0) factors.Add("Fever reported");
        if (input.Vomiting > 0) factors.Add("Vomiting reported");
        if (input.Dizziness > 0) factors.Add("Dizziness reported");
        if (input.ExistingCondition > 0) factors.Add("Relevant existing condition reported");
        if (input.Age >= 65 || input.Age <= 10) factors.Add("Age group requires additional attention");
        if (input.MissingInformation > 0) factors.Add("Some optional information was not provided");
        if (factors.Count == 0) factors.Add("Reported symptom pattern and duration");
        return factors;
    }
}
