using MedicAI.Api.Domain;

namespace MedicAI.Api.Services;

public sealed record RuleEvaluationResult(
    bool IsEmergency,
    IReadOnlyList<string> RuleCodes,
    IReadOnlyList<string> Reasons,
    string WarningMessage);

public interface IRedFlagRuleEngine
{
    RuleEvaluationResult Evaluate(SymptomSubmission submission);
}

public sealed class RedFlagRuleEngine : IRedFlagRuleEngine
{
    public RuleEvaluationResult Evaluate(SymptomSubmission submission)
    {
        var codes = new List<string>();
        var reasons = new List<string>();

        Add(submission.SevereBreathingDifficulty, "SEVERE_BREATHING", "Severe breathing difficulty was reported.");
        Add(submission.LossOfConsciousness, "LOSS_OF_CONSCIOUSNESS", "Loss of consciousness was reported.");
        Add(submission.MajorBleeding, "MAJOR_BLEEDING", "Major or uncontrolled bleeding was reported.");
        Add(submission.Seizure, "SEIZURE", "A seizure was reported.");
        Add(submission.SevereAllergicReaction, "SEVERE_ALLERGY", "A severe allergic reaction was reported.");
        Add(submission.SuddenSevereWeakness, "SUDDEN_WEAKNESS", "Sudden severe weakness was reported.");
        Add(submission.ChestPain && (submission.PainSeverity >= 7 || submission.WorseningSymptoms),
            "SEVERE_CHEST_PAIN", "Severe or worsening chest pain was reported.");

        return new RuleEvaluationResult(
            codes.Count > 0,
            codes,
            reasons,
            codes.Count > 0
                ? "Seek immediate professional medical or emergency assistance. Do not wait for a normal clinic queue."
                : string.Empty);

        void Add(bool condition, string code, string reason)
        {
            if (!condition) return;
            codes.Add(code);
            reasons.Add(reason);
        }
    }
}
