using MedicAI.Api.Domain;
using MedicAI.Api.Services;

namespace MedicAI.Tests;

public sealed class RedFlagRuleEngineTests
{
    private readonly RedFlagRuleEngine _engine = new();

    [Fact]
    public void SevereBreathingDifficulty_AlwaysTriggersEmergency()
    {
        var result = _engine.Evaluate(CreateSubmission(severeBreathingDifficulty: true));

        Assert.True(result.IsEmergency);
        Assert.Contains("SEVERE_BREATHING", result.RuleCodes);
        Assert.Contains("immediate", result.WarningMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SevereOrWorseningChestPain_TriggersEmergency()
    {
        var severe = _engine.Evaluate(CreateSubmission(chestPain: true, painSeverity: 8));
        var worsening = _engine.Evaluate(CreateSubmission(chestPain: true, painSeverity: 4, worsening: true));

        Assert.True(severe.IsEmergency);
        Assert.True(worsening.IsEmergency);
        Assert.Contains("SEVERE_CHEST_PAIN", severe.RuleCodes);
    }

    [Fact]
    public void MildNonRedFlagSymptoms_DoNotTriggerEmergency()
    {
        var result = _engine.Evaluate(CreateSubmission(painSeverity: 2));

        Assert.False(result.IsEmergency);
        Assert.Empty(result.RuleCodes);
    }

    private static SymptomSubmission CreateSubmission(
        bool severeBreathingDifficulty = false,
        bool chestPain = false,
        short painSeverity = 0,
        bool worsening = false) => new()
    {
        Patient = null!,
        MainSymptom = "test symptom",
        DurationValue = 1,
        DurationUnit = DurationUnit.Days,
        PainSeverity = painSeverity,
        ConsentConfirmed = true,
        SevereBreathingDifficulty = severeBreathingDifficulty,
        ChestPain = chestPain,
        WorseningSymptoms = worsening
    };
}
