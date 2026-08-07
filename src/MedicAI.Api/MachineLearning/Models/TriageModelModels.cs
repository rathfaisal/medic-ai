using Microsoft.ML.Data;

namespace MedicAI.Api.MachineLearning.Models;

public sealed class TriageModelInput
{
    [LoadColumn(0)]
    public string Label { get; set; } = string.Empty;

    [LoadColumn(1)]
    public float Age { get; set; }

    [LoadColumn(2)]
    public string MainSymptom { get; set; } = string.Empty;

    [LoadColumn(3)]
    public float DurationHours { get; set; }

    [LoadColumn(4)]
    public float PainSeverity { get; set; }

    [LoadColumn(5)]
    public float Fever { get; set; }

    [LoadColumn(6)]
    public float Vomiting { get; set; }

    [LoadColumn(7)]
    public float Dizziness { get; set; }

    [LoadColumn(8)]
    public float WorseningSymptoms { get; set; }

    [LoadColumn(9)]
    public float ExistingCondition { get; set; }

    [LoadColumn(10)]
    public float MissingInformation { get; set; }
}

public sealed class TriageModelOutput
{
    [ColumnName("PredictedLabel")]
    public string PredictedLabel { get; set; } = string.Empty;

    public float[] Score { get; set; } = Array.Empty<float>();
}

public sealed record MlPredictionResult(string Label, decimal Confidence, IReadOnlyList<string> ContributingFactors);
