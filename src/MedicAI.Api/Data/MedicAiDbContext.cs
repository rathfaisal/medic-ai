using MedicAI.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Api.Data;

public sealed class MedicAiDbContext(DbContextOptions<MedicAiDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    public DbSet<DoctorProfile> DoctorProfiles => Set<DoctorProfile>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<SymptomSubmission> SymptomSubmissions => Set<SymptomSubmission>();
    public DbSet<TriageAssessment> TriageAssessments => Set<TriageAssessment>();
    public DbSet<QueueEntry> QueueEntries => Set<QueueEntry>();
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AiModelVersion> AiModelVersions => Set<AiModelVersion>();
    public DbSet<RedFlagRule> RedFlagRules => Set<RedFlagRule>();
    public DbSet<EmergencyAlert> EmergencyAlerts => Set<EmergencyAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("medic_ai");

        ConfigureUsers(modelBuilder);
        ConfigureProfiles(modelBuilder);
        ConfigureAppointments(modelBuilder);
        ConfigureTriage(modelBuilder);
        ConfigureQueueAndConsultations(modelBuilder);
        ConfigureSupportingEntities(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();
        entity.ToTable("users");
        entity.HasIndex(x => x.Email).IsUnique();
        entity.Property(x => x.Email).HasColumnName("email");
        entity.Property(x => x.PasswordHash).HasColumnName("password_hash");
        entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(30).HasColumnName("role");
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).HasColumnName("status");
        entity.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }

    private static void ConfigureProfiles(ModelBuilder modelBuilder)
    {
        var patient = modelBuilder.Entity<PatientProfile>();
        patient.ToTable("patient_profiles");
        patient.HasIndex(x => x.UserId).IsUnique();
        patient.HasOne(x => x.User).WithOne(x => x.PatientProfile).HasForeignKey<PatientProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        var staff = modelBuilder.Entity<StaffProfile>();
        staff.ToTable("staff_profiles");
        staff.HasIndex(x => x.UserId).IsUnique();
        staff.HasOne(x => x.User).WithOne(x => x.StaffProfile).HasForeignKey<StaffProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        var doctor = modelBuilder.Entity<DoctorProfile>();
        doctor.ToTable("doctor_profiles");
        doctor.HasIndex(x => x.UserId).IsUnique();
        doctor.HasIndex(x => x.RegistrationNo).IsUnique();
        doctor.HasOne(x => x.User).WithOne(x => x.DoctorProfile).HasForeignKey<DoctorProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureAppointments(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Appointment>();
        entity.ToTable("appointments");
        entity.HasIndex(x => x.AppointmentDateTime);
        entity.HasIndex(x => x.Status);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        entity.HasOne(x => x.Patient).WithMany(x => x.Appointments).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.AssignedDoctor).WithMany(x => x.Appointments).HasForeignKey(x => x.AssignedDoctorId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureTriage(ModelBuilder modelBuilder)
    {
        var submission = modelBuilder.Entity<SymptomSubmission>();
        submission.ToTable("symptom_submissions");
        submission.Property(x => x.DurationUnit).HasConversion<string>().HasMaxLength(30);
        submission.HasOne(x => x.Patient).WithMany(x => x.SymptomSubmissions).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        submission.HasOne(x => x.Appointment).WithOne(x => x.SymptomSubmission).HasForeignKey<SymptomSubmission>(x => x.AppointmentId).OnDelete(DeleteBehavior.SetNull);

        var assessment = modelBuilder.Entity<TriageAssessment>();
        assessment.ToTable("triage_assessments");
        assessment.HasIndex(x => x.SubmissionId).IsUnique();
        assessment.HasIndex(x => x.UrgencyLevel);
        assessment.Property(x => x.UrgencyLevel).HasConversion<string>().HasMaxLength(30);
        assessment.Property(x => x.Source).HasConversion<string>().HasMaxLength(30);
        assessment.Property(x => x.OriginalUrgency).HasConversion<string>().HasMaxLength(30);
        assessment.Property(x => x.OverriddenUrgency).HasConversion<string>().HasMaxLength(30);
        assessment.Property(x => x.ConfidenceScore).HasPrecision(5, 4);
        assessment.Property(x => x.RuleFlagsJson).HasColumnType("jsonb");
        assessment.HasOne(x => x.Submission).WithOne(x => x.Assessment).HasForeignKey<TriageAssessment>(x => x.SubmissionId).OnDelete(DeleteBehavior.Cascade);
        assessment.HasOne(x => x.ModelVersion).WithMany(x => x.Assessments).HasForeignKey(x => x.ModelVersionId).OnDelete(DeleteBehavior.SetNull);
        assessment.HasOne(x => x.OverriddenByUser).WithMany().HasForeignKey(x => x.OverriddenByUserId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureQueueAndConsultations(ModelBuilder modelBuilder)
    {
        var queue = modelBuilder.Entity<QueueEntry>();
        queue.ToTable("queue_entries");
        queue.HasIndex(x => x.CheckInTime);
        queue.HasIndex(x => x.PriorityScore);
        queue.HasIndex(x => x.ConfirmedUrgency);
        queue.HasIndex(x => x.QueueStatus);
        queue.Property(x => x.PriorityScore).HasPrecision(8, 2);
        queue.Property(x => x.ConfirmedUrgency).HasConversion<string>().HasMaxLength(30);
        queue.Property(x => x.QueueStatus).HasConversion<string>().HasMaxLength(30);
        queue.HasOne(x => x.Patient).WithMany(x => x.QueueEntries).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        queue.HasOne(x => x.Appointment).WithOne(x => x.QueueEntry).HasForeignKey<QueueEntry>(x => x.AppointmentId).OnDelete(DeleteBehavior.SetNull);
        queue.HasOne(x => x.Assessment).WithOne(x => x.QueueEntry).HasForeignKey<QueueEntry>(x => x.AssessmentId).OnDelete(DeleteBehavior.SetNull);
        queue.HasOne(x => x.HandledByStaff).WithMany().HasForeignKey(x => x.HandledByStaffId).OnDelete(DeleteBehavior.SetNull);

        var consultation = modelBuilder.Entity<Consultation>();
        consultation.ToTable("consultations");
        consultation.HasIndex(x => x.AppointmentId).IsUnique();
        consultation.HasOne(x => x.Patient).WithMany(x => x.Consultations).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        consultation.HasOne(x => x.Doctor).WithMany(x => x.Consultations).HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
        consultation.HasOne(x => x.Appointment).WithOne(x => x.Consultation).HasForeignKey<Consultation>(x => x.AppointmentId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureSupportingEntities(ModelBuilder modelBuilder)
    {
        var notification = modelBuilder.Entity<Notification>();
        notification.ToTable("notifications");
        notification.HasIndex(x => x.DeliveryStatus);
        notification.Property(x => x.Channel).HasConversion<string>().HasMaxLength(30);
        notification.Property(x => x.DeliveryStatus).HasConversion<string>().HasMaxLength(30);
        notification.HasOne(x => x.User).WithMany(x => x.Notifications).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        notification.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.SetNull);

        var audit = modelBuilder.Entity<AuditLog>();
        audit.ToTable("audit_logs");
        audit.HasIndex(x => x.Action);
        audit.HasIndex(x => x.EntityType);
        audit.HasIndex(x => x.CreatedAt);
        audit.Property(x => x.DetailsJson).HasColumnType("jsonb");
        audit.HasOne(x => x.User).WithMany(x => x.AuditLogs).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);

        var modelVersion = modelBuilder.Entity<AiModelVersion>();
        modelVersion.ToTable("ai_model_versions");
        modelVersion.HasIndex(x => x.VersionName).IsUnique();
        modelVersion.HasIndex(x => x.IsActive);
        modelVersion.Property(x => x.FeatureSchemaJson).HasColumnType("jsonb");
        modelVersion.Property(x => x.MetricsJson).HasColumnType("jsonb");

        var rule = modelBuilder.Entity<RedFlagRule>();
        rule.ToTable("red_flag_rules");
        rule.HasIndex(x => x.RuleCode).IsUnique();
        rule.HasIndex(x => x.IsActive);
        rule.Property(x => x.ConditionJson).HasColumnType("jsonb");

        var alert = modelBuilder.Entity<EmergencyAlert>();
        alert.ToTable("emergency_alerts");
        alert.HasIndex(x => x.Status);
        alert.HasIndex(x => x.CreatedAt);
        alert.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        alert.Property(x => x.AssignedRole).HasConversion<string>().HasMaxLength(30);
        alert.HasOne(x => x.Patient).WithMany(x => x.EmergencyAlerts).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        alert.HasOne(x => x.Assessment).WithOne(x => x.EmergencyAlert).HasForeignKey<EmergencyAlert>(x => x.AssessmentId).OnDelete(DeleteBehavior.Cascade);
        alert.HasOne(x => x.AcknowledgedByUser).WithMany().HasForeignKey(x => x.AcknowledgedByUserId).OnDelete(DeleteBehavior.SetNull);
        alert.HasOne(x => x.ResolvedByUser).WithMany().HasForeignKey(x => x.ResolvedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
