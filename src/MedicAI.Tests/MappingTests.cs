using MedicAI.Api.Domain;
using MedicAI.Api.Extensions;

namespace MedicAI.Tests;

public sealed class MappingTests
{
    [Fact]
    public void TriageResponse_UsesDoctorOverrideAndPreservesAssessmentSource()
    {
        var assessment = new TriageAssessment
        {
            Submission = null!,
            SubmissionId = 10,
            UrgencyLevel = UrgencyLevel.Priority,
            OriginalUrgency = UrgencyLevel.Priority,
            OverriddenUrgency = UrgencyLevel.Urgent,
            Source = AssessmentSource.MachineLearning,
            Explanation = "Doctor reviewed worsening symptoms.",
            RuleFlagsJson = "[]"
        };

        var response = assessment.ToResponse("Urgent review recommended.");

        Assert.Equal(UrgencyLevel.Urgent, response.EffectiveUrgency);
        Assert.Equal(AssessmentSource.MachineLearning, response.Source);
        Assert.Equal(assessment.ReviewRequired, response.ReviewRequired);
    }

    [Fact]
    public void AppointmentResponse_OnlyTreatsActiveQueueStatesAsCheckedIn()
    {
        var patient = new PatientProfile
        {
            User = null!,
            FullName = "Test Patient",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Phone = "0100000000"
        };
        var appointment = new Appointment
        {
            Patient = patient,
            ServiceType = "General Consultation",
            CreatedByUser = null!,
            QueueEntry = new QueueEntry
            {
                Patient = patient,
                ConfirmedUrgency = UrgencyLevel.Routine,
                QueueStatus = QueueStatus.Completed
            }
        };

        var response = appointment.ToResponse();

        Assert.False(response.IsCheckedIn);
    }
}
