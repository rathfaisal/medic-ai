using System.Text.Json;
using MedicAI.Api.Data;
using MedicAI.Api.Domain;
using MedicAI.Api.MachineLearning;
using MedicAI.Api.MachineLearning.Models;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Api.Services;

public interface ITriageService
{
    Task<TriageAssessment> AssessAsync(SymptomSubmission submission, CancellationToken cancellationToken = default);
    string GetGuidance(UrgencyLevel urgency);
}

public sealed class TriageService(
    MedicAiDbContext dbContext,
    IRedFlagRuleEngine ruleEngine,
    MlTriageModelService modelService,
    INotificationService notificationService,
    IConfiguration configuration) : ITriageService
{
    public async Task<TriageAssessment> AssessAsync(SymptomSubmission submission, CancellationToken cancellationToken = default)
    {
        var patient = await dbContext.PatientProfiles
            .AsNoTracking()
            .FirstAsync(x => x.Id == submission.PatientId, cancellationToken);

        var rules = ruleEngine.Evaluate(submission);
        if (rules.IsEmergency)
        {
            var emergencyAssessment = new TriageAssessment
            {
                Submission = submission,
                SubmissionId = submission.Id,
                UrgencyLevel = UrgencyLevel.EmergencyWarning,
                Source = AssessmentSource.RuleEngine,
                OriginalUrgency = UrgencyLevel.EmergencyWarning,
                RuleFlagsJson = JsonSerializer.Serialize(rules.RuleCodes),
                Explanation = string.Join(" ", rules.Reasons) + " " + rules.WarningMessage,
                ReviewRequired = true,
                LowConfidenceFlag = false,
                ModelVersionId = null
            };

            dbContext.TriageAssessments.Add(emergencyAssessment);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.EmergencyAlerts.Add(new EmergencyAlert
            {
                PatientId = submission.PatientId,
                Patient = submission.Patient,
                AssessmentId = emergencyAssessment.Id,
                Assessment = emergencyAssessment,
                WarningReason = string.Join(" ", rules.Reasons),
                Status = AlertStatus.Created,
                AssignedRole = UserRole.Staff
            });
            await dbContext.SaveChangesAsync(cancellationToken);

            await notificationService.CreateForRolesAsync(
                new[] { UserRole.Staff, UserRole.Doctor, UserRole.Admin },
                $"EMERGENCY WARNING: {patient.FullName} submitted red-flag symptoms. Immediate staff review is required.",
                cancellationToken);

            return emergencyAssessment;
        }

        if (!modelService.IsAvailable)
        {
            var fallbackAssessment = new TriageAssessment
            {
                Submission = submission,
                SubmissionId = submission.Id,
                ModelVersionId = null,
                UrgencyLevel = UrgencyLevel.Priority,
                OriginalUrgency = UrgencyLevel.Priority,
                Source = AssessmentSource.ManualFallback,
                ConfidenceScore = null,
                RuleFlagsJson = "[]",
                Explanation = "The ML.NET model was unavailable, so no machine-learning prediction was generated. A conservative temporary Priority level was assigned only to route this submission for manual staff or doctor review.",
                LowConfidenceFlag = true,
                ReviewRequired = true,
                AssessedAt = DateTimeOffset.UtcNow
            };

            dbContext.TriageAssessments.Add(fallbackAssessment);
            await dbContext.SaveChangesAsync(cancellationToken);
            await notificationService.CreateForRolesAsync(
                new[] { UserRole.Staff, UserRole.Doctor, UserRole.Admin },
                $"MANUAL REVIEW REQUIRED: AI assessment was unavailable for {patient.FullName}'s submission #{submission.Id}.",
                cancellationToken);
            return fallbackAssessment;
        }

        var input = BuildInput(submission, patient);
        var prediction = modelService.Predict(input);
        var predictedUrgency = ParseUrgency(prediction.Label);
        var threshold = configuration.GetValue<decimal?>("Triage:LowConfidenceThreshold") ?? 0.65m;
        var lowConfidence = prediction.Confidence < threshold;
        var finalUrgency = lowConfidence ? EscalateOneLevel(predictedUrgency) : predictedUrgency;

        var explanationParts = prediction.ContributingFactors.ToList();
        if (lowConfidence)
        {
            explanationParts.Add($"Model confidence ({prediction.Confidence:P0}) was below the configured threshold ({threshold:P0}); urgency was escalated for staff review.");
        }

        var assessment = new TriageAssessment
        {
            Submission = submission,
            SubmissionId = submission.Id,
            ModelVersionId = modelService.ActiveModelVersionId,
            UrgencyLevel = finalUrgency,
            Source = AssessmentSource.MachineLearning,
            OriginalUrgency = predictedUrgency,
            ConfidenceScore = prediction.Confidence,
            RuleFlagsJson = "[]",
            Explanation = string.Join(" ", explanationParts),
            LowConfidenceFlag = lowConfidence,
            ReviewRequired = lowConfidence,
            AssessedAt = DateTimeOffset.UtcNow
        };

        dbContext.TriageAssessments.Add(assessment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return assessment;
    }

    public string GetGuidance(UrgencyLevel urgency) => urgency switch
    {
        UrgencyLevel.Routine => "Your information suggests a routine clinic review. Keep your appointment and seek help sooner if symptoms worsen.",
        UrgencyLevel.Priority => "Your information suggests that clinic staff should review you with higher priority than routine cases.",
        UrgencyLevel.Urgent => "Your information suggests urgent clinic assessment. Contact the clinic promptly and seek emergency help if your condition deteriorates.",
        UrgencyLevel.EmergencyWarning => "Seek immediate professional medical or emergency assistance. Do not wait for the normal clinic queue.",
        _ => "Manual review is required."
    };

    private static TriageModelInput BuildInput(SymptomSubmission submission, PatientProfile patient)
    {
        var age = CalculateAge(patient.DateOfBirth, DateOnly.FromDateTime(DateTime.UtcNow));
        return new TriageModelInput
        {
            Age = age,
            MainSymptom = NormalizeSymptom(submission.MainSymptom),
            DurationHours = ConvertDurationToHours(submission.DurationValue, submission.DurationUnit),
            PainSeverity = submission.PainSeverity,
            Fever = submission.Fever ? 1 : 0,
            Vomiting = submission.Vomiting ? 1 : 0,
            Dizziness = submission.Dizziness ? 1 : 0,
            WorseningSymptoms = submission.WorseningSymptoms ? 1 : 0,
            ExistingCondition = string.IsNullOrWhiteSpace(submission.ExistingConditionsSnapshot) ? 0 : 1,
            MissingInformation = string.IsNullOrWhiteSpace(submission.AdditionalSymptoms) ? 1 : 0
        };
    }

    private static int CalculateAge(DateOnly dateOfBirth, DateOnly today)
    {
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age)) age--;
        return Math.Max(age, 0);
    }

    private static float ConvertDurationToHours(int value, DurationUnit unit) => unit switch
    {
        DurationUnit.Hours => value,
        DurationUnit.Days => value * 24f,
        DurationUnit.Weeks => value * 168f,
        DurationUnit.Months => value * 730f,
        _ => value
    };

    private static string NormalizeSymptom(string symptom)
    {
        var text = symptom.Trim().ToLowerInvariant();
        var known = new[]
        {
            "cough", "fever", "headache", "abdominal pain", "back pain", "dizziness",
            "vomiting", "sore throat", "skin rash", "minor injury", "urinary symptoms",
            "fatigue", "ear pain", "diarrhoea", "joint pain"
        };

        return known.FirstOrDefault(text.Contains) ?? "other symptom";
    }

    private static UrgencyLevel ParseUrgency(string label) => label.Trim().ToLowerInvariant() switch
    {
        "routine" => UrgencyLevel.Routine,
        "priority" => UrgencyLevel.Priority,
        "urgent" => UrgencyLevel.Urgent,
        _ => UrgencyLevel.Priority
    };

    private static UrgencyLevel EscalateOneLevel(UrgencyLevel urgency) => urgency switch
    {
        UrgencyLevel.Routine => UrgencyLevel.Priority,
        UrgencyLevel.Priority => UrgencyLevel.Urgent,
        _ => urgency
    };
}
