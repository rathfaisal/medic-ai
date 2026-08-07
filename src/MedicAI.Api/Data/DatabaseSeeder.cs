using System.Text.Json;
using MedicAI.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Api.Data;

public static class DatabaseSeeder
{
    public static async Task InitialiseAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MedicAiDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

        const int maxAttempts = 20;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Database is not ready. Attempt {Attempt}/{MaxAttempts}.", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }

        if (!await dbContext.Users.AnyAsync(cancellationToken))
        {
            var hasher = new PasswordHasher<User>();

            var admin = CreateUser("admin@medic-ai.local", UserRole.Admin);
            admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");
            admin.StaffProfile = new StaffProfile
            {
                User = admin,
                FullName = "MEDIC-AI Administrator",
                Position = "System Administrator",
                Phone = "0100000001"
            };

            var doctor = CreateUser("doctor@medic-ai.local", UserRole.Doctor);
            doctor.PasswordHash = hasher.HashPassword(doctor, "Doctor123!");
            doctor.DoctorProfile = new DoctorProfile
            {
                User = doctor,
                FullName = "Dr. Prem Rayan",
                RegistrationNo = "DEMO-MMC-001",
                Speciality = "General Practice",
                IsAvailable = true
            };

            var staff = CreateUser("staff@medic-ai.local", UserRole.Staff);
            staff.PasswordHash = hasher.HashPassword(staff, "Staff123!");
            staff.StaffProfile = new StaffProfile
            {
                User = staff,
                FullName = "Demo Receptionist",
                Position = "Receptionist",
                Phone = "0100000002"
            };

            var patient = CreateUser("patient@medic-ai.local", UserRole.Patient);
            patient.PasswordHash = hasher.HashPassword(patient, "Patient123!");
            patient.PatientProfile = new PatientProfile
            {
                User = patient,
                FullName = "Demo Patient",
                DateOfBirth = new DateOnly(1996, 5, 15),
                Gender = "Prefer not to say",
                Phone = "0100000003",
                Address = "Sample address for demonstration only",
                Allergies = "No known allergies",
                ExistingConditions = "None reported",
                EmergencyContact = "Demo Contact - 0100000004"
            };

            dbContext.Users.AddRange(admin, doctor, staff, patient);
            await dbContext.SaveChangesAsync(cancellationToken);

            var appointment = new Appointment
            {
                PatientId = patient.PatientProfile.Id,
                Patient = patient.PatientProfile,
                AssignedDoctorId = doctor.DoctorProfile.Id,
                AssignedDoctor = doctor.DoctorProfile,
                AppointmentDateTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
                ServiceType = "General Consultation",
                Reason = "Demonstration appointment",
                Status = AppointmentStatus.Booked,
                CreatedByUserId = patient.Id,
                CreatedByUser = patient
            };
            dbContext.Appointments.Add(appointment);
            dbContext.Notifications.Add(new Notification
            {
                UserId = patient.Id,
                User = patient,
                Appointment = appointment,
                Message = "Your demonstration appointment has been booked.",
                Channel = NotificationChannel.InApp,
                DeliveryStatus = DeliveryStatus.Sent,
                SentAt = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.RedFlagRules.AnyAsync(cancellationToken))
        {
            var rules = new[]
            {
                Rule("SEVERE_BREATHING", "Severe breathing difficulty", "SevereBreathingDifficulty == true"),
                Rule("LOSS_OF_CONSCIOUSNESS", "Loss of consciousness", "LossOfConsciousness == true"),
                Rule("MAJOR_BLEEDING", "Major uncontrolled bleeding", "MajorBleeding == true"),
                Rule("SEIZURE", "Seizure", "Seizure == true"),
                Rule("SEVERE_ALLERGY", "Severe allergic reaction", "SevereAllergicReaction == true"),
                Rule("SUDDEN_WEAKNESS", "Sudden severe weakness", "SuddenSevereWeakness == true"),
                Rule("SEVERE_CHEST_PAIN", "Severe or worsening chest pain", "ChestPain == true AND (PainSeverity >= 7 OR WorseningSymptoms == true)")
            };
            dbContext.RedFlagRules.AddRange(rules);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        static User CreateUser(string email, UserRole role) => new()
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = "temporary",
            Role = role,
            Status = UserStatus.Active
        };

        static RedFlagRule Rule(string code, string name, string expression) => new()
        {
            RuleCode = code,
            RuleName = name,
            ConditionJson = JsonSerializer.Serialize(new { expression }),
            WarningMessage = "Seek immediate professional medical or emergency assistance. Do not wait for the normal clinic queue.",
            ApprovedBy = "Client review required before real use",
            IsActive = true
        };
    }
}
