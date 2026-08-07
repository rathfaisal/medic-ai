using MedicAI.Api.Data;
using MedicAI.Api.Domain;
using MedicAI.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Tests;

public sealed class QueueServiceTests
{
    [Fact]
    public async Task UrgentPatient_IsOrderedBeforeRoutinePatient()
    {
        await using var db = CreateDb();
        var staff = CreateStaff();
        var urgentAppointment = CreateAppointment("Urgent Test Patient", "urgent@test.local", DateTimeOffset.UtcNow.AddHours(2), staff.User);
        var routineAppointment = CreateAppointment("Routine Test Patient", "routine@test.local", DateTimeOffset.UtcNow.AddHours(3), staff.User);
        db.AddRange(staff.User, urgentAppointment.Patient.User, routineAppointment.Patient.User, urgentAppointment, routineAppointment);
        await db.SaveChangesAsync();

        var service = new QueueService(db);
        await service.CheckInAsync(routineAppointment.Id, UrgencyLevel.Routine, staff.User.Id);
        await service.CheckInAsync(urgentAppointment.Id, UrgencyLevel.Urgent, staff.User.Id);

        var queue = await service.GetActiveQueueAsync();

        Assert.Equal(2, queue.Count);
        Assert.Equal(UrgencyLevel.Urgent, queue[0].ConfirmedUrgency);
        Assert.Equal("Urgent Test Patient", queue[0].Patient.FullName);
    }


    [Fact]
    public async Task RemovedAppointmentQueueEntry_IsReusedOnCheckIn()
    {
        await using var db = CreateDb();
        var staff = CreateStaff();
        var appointment = CreateAppointment("Returning Patient", "returning@test.local", DateTimeOffset.UtcNow.AddHours(2), staff.User);
        db.AddRange(staff.User, appointment.Patient.User, appointment);
        await db.SaveChangesAsync();

        var service = new QueueService(db);
        var first = await service.CheckInAsync(appointment.Id, UrgencyLevel.Routine, staff.User.Id);
        await service.UpdateStatusAsync(first.Id, QueueStatus.Removed, staff.User.Id);
        var second = await service.CheckInAsync(appointment.Id, UrgencyLevel.Priority, staff.User.Id);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(QueueStatus.Waiting, second.QueueStatus);
        Assert.Equal(UrgencyLevel.Priority, second.ConfirmedUrgency);
        Assert.Single(db.QueueEntries);
    }

    [Fact]
    public async Task EmergencyWarning_CannotEnterNormalQueue()
    {
        await using var db = CreateDb();
        var staff = CreateStaff();
        var appointment = CreateAppointment("Emergency Test", "emergency@test.local", DateTimeOffset.UtcNow.AddHours(2), staff.User);
        db.AddRange(staff.User, appointment.Patient.User, appointment);
        await db.SaveChangesAsync();
        var service = new QueueService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CheckInAsync(appointment.Id, UrgencyLevel.EmergencyWarning, staff.User.Id));
    }

    private static MedicAiDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<MedicAiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MedicAiDbContext(options);
    }

    private static StaffProfile CreateStaff()
    {
        var user = new User { Email = "staff@test.local", PasswordHash = "hash", Role = UserRole.Staff };
        var profile = new StaffProfile { User = user, FullName = "Test Staff", Position = "Receptionist" };
        user.StaffProfile = profile;
        return profile;
    }

    private static Appointment CreateAppointment(string name, string email, DateTimeOffset when, User createdBy)
    {
        var patientUser = new User { Email = email, PasswordHash = "hash", Role = UserRole.Patient };
        var patient = new PatientProfile
        {
            User = patientUser,
            FullName = name,
            DateOfBirth = new DateOnly(1990, 1, 1),
            Phone = "0100000000"
        };
        patientUser.PatientProfile = patient;
        return new Appointment
        {
            Patient = patient,
            AppointmentDateTime = when,
            ServiceType = "General Consultation",
            CreatedByUser = createdBy,
            Status = AppointmentStatus.Booked
        };
    }
}
