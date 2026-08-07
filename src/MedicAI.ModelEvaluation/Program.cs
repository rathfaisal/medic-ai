using System.Globalization;
using System.Text.Json;
using MedicAI.Api.MachineLearning.Models;
using Microsoft.ML;

var root = FindProjectRoot();
var modelPath = args.ElementAtOrDefault(0)
    ?? Path.Combine(root, "src", "MedicAI.Api", "MachineLearning", "Artifacts", "triage-model.zip");
var validationPath = args.ElementAtOrDefault(1)
    ?? Path.Combine(root, "src", "MedicAI.Api", "MachineLearning", "Data", "doctor-validation-template.csv");
var outputPath = args.ElementAtOrDefault(2)
    ?? Path.Combine(root, "docs", "doctor-validation-results.json");

if (!File.Exists(modelPath))
{
    Console.Error.WriteLine($"Model file not found: {modelPath}");
    Console.Error.WriteLine("Start the API once so the ML.NET model is trained, then run this evaluator again.");
    return 2;
}
if (!File.Exists(validationPath))
{
    Console.Error.WriteLine($"Validation file not found: {validationPath}");
    return 2;
}

var rows = LoadRows(validationPath)
    .Where(x => EvaluationConstants.SupportedLabels.Contains(x.DoctorLabel, StringComparer.OrdinalIgnoreCase))
    .ToList();

if (rows.Count == 0)
{
    Console.Error.WriteLine("No independently labelled rows were found.");
    Console.Error.WriteLine("Ask Dr. Prem Rayan to complete DoctorLabel with Routine, Priority or Urgent without seeing the generated label or model output.");
    return 3;
}

var mlContext = new MLContext(seed: 4242);
var model = mlContext.Model.Load(modelPath, out _);
var engine = mlContext.Model.CreatePredictionEngine<TriageModelInput, TriageModelOutput>(model);

var observations = rows.Select(row => new Observation(
    row.ScenarioId,
    Normalise(row.DoctorLabel),
    Normalise(engine.Predict(row.ToInput()).PredictedLabel))).ToList();

var labels = EvaluationConstants.SupportedLabels;
var matrix = labels.ToDictionary(
    actual => actual,
    actual => labels.ToDictionary(predicted => predicted, _ => 0));
foreach (var item in observations) matrix[item.Actual][item.Predicted]++;

var correct = observations.Count(x => x.Actual == x.Predicted);
var agreement = (double)correct / observations.Count;
var perClass = new Dictionary<string, object>();
var weightedF1Sum = 0d;

foreach (var label in labels)
{
    var tp = matrix[label][label];
    var fp = labels.Where(x => x != label).Sum(actual => matrix[actual][label]);
    var fn = labels.Where(x => x != label).Sum(predicted => matrix[label][predicted]);
    var support = labels.Sum(predicted => matrix[label][predicted]);
    var precision = Divide(tp, tp + fp);
    var recall = Divide(tp, tp + fn);
    var f1 = precision + recall == 0 ? 0 : 2 * precision * recall / (precision + recall);
    weightedF1Sum += f1 * support;
    perClass[label] = new { support, precision, recall, f1 };
}

var expectedAgreement = labels.Sum(label =>
{
    var actualCount = observations.Count(x => x.Actual == label);
    var predictedCount = observations.Count(x => x.Predicted == label);
    return ((double)actualCount / observations.Count) * ((double)predictedCount / observations.Count);
});
var kappa = Math.Abs(1 - expectedAgreement) < 0.000001
    ? 0
    : (agreement - expectedAgreement) / (1 - expectedAgreement);

var report = new
{
    generatedAtUtc = DateTimeOffset.UtcNow,
    validationSource = "Blind independent labels supplied by Dr. Prem Rayan or another qualified clinic reviewer",
    rowsEvaluated = observations.Count,
    exactAgreement = agreement,
    weightedF1 = weightedF1Sum / observations.Count,
    cohensKappa = kappa,
    confusionMatrix = matrix,
    perClass,
    disagreements = observations.Where(x => x.Actual != x.Predicted).Take(50),
    limitations = new[]
    {
        "This measures agreement with the independent reviewer, not clinical validity or safety for real-world autonomous use.",
        "Emergency-warning cases are evaluated separately through deterministic red-flag safety tests because the ML model classifies only Routine, Priority and Urgent."
    }
};

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Doctor-labelled rows evaluated: {observations.Count}");
Console.WriteLine($"Exact agreement: {agreement:P2}");
Console.WriteLine($"Weighted F1: {weightedF1Sum / observations.Count:F4}");
Console.WriteLine($"Cohen's kappa: {kappa:F4}");
Console.WriteLine($"Report: {outputPath}");
return 0;

static double Divide(int numerator, int denominator) => denominator == 0 ? 0 : (double)numerator / denominator;
static string Normalise(string value) => EvaluationConstants.SupportedLabels.FirstOrDefault(x => x.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase)) ?? "Priority";

static List<ValidationRow> LoadRows(string path)
{
    var result = new List<ValidationRow>();
    foreach (var line in File.ReadLines(path).Skip(1))
    {
        if (string.IsNullOrWhiteSpace(line)) continue;
        var cells = ParseCsvLine(line);
        if (cells.Count < 13) continue;
        result.Add(new ValidationRow(
            cells[0], ParseFloat(cells[1]), cells[2], ParseFloat(cells[3]), ParseFloat(cells[4]),
            ParseFloat(cells[5]), ParseFloat(cells[6]), ParseFloat(cells[7]), ParseFloat(cells[8]),
            ParseFloat(cells[9]), ParseFloat(cells[10]), cells[11], cells[12]));
    }
    return result;
}

static List<string> ParseCsvLine(string line)
{
    var cells = new List<string>();
    var current = new System.Text.StringBuilder();
    var quoted = false;
    for (var i = 0; i < line.Length; i++)
    {
        var ch = line[i];
        if (ch == '"')
        {
            if (quoted && i + 1 < line.Length && line[i + 1] == '"')
            {
                current.Append('"');
                i++;
            }
            else quoted = !quoted;
        }
        else if (ch == ',' && !quoted)
        {
            cells.Add(current.ToString());
            current.Clear();
        }
        else current.Append(ch);
    }
    cells.Add(current.ToString());
    return cells;
}

static float ParseFloat(string value) => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;

static string FindProjectRoot()
{
    var current = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "MEDIC-AI.sln"))) return current.FullName;
        current = current.Parent;
    }
    return Directory.GetCurrentDirectory();
}

public sealed record ValidationRow(
    string ScenarioId,
    float Age,
    string MainSymptom,
    float DurationHours,
    float PainSeverity,
    float Fever,
    float Vomiting,
    float Dizziness,
    float WorseningSymptoms,
    float ExistingCondition,
    float MissingInformation,
    string DoctorLabel,
    string DoctorNotes)
{
    public TriageModelInput ToInput() => new()
    {
        Age = Age,
        MainSymptom = MainSymptom,
        DurationHours = DurationHours,
        PainSeverity = PainSeverity,
        Fever = Fever,
        Vomiting = Vomiting,
        Dizziness = Dizziness,
        WorseningSymptoms = WorseningSymptoms,
        ExistingCondition = ExistingCondition,
        MissingInformation = MissingInformation
    };
}

public sealed record Observation(string ScenarioId, string Actual, string Predicted);

internal static class EvaluationConstants
{
    public static readonly string[] SupportedLabels = ["Routine", "Priority", "Urgent"];
}
