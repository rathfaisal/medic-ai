using MedicAI.Api.Contracts;
using MedicAI.Api.Data;
using MedicAI.Api.Domain;
using MedicAI.Api.Extensions;
using MedicAI.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController(
    MedicAiDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IAuditService auditService) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<UserListResponse>>> Users(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.PatientProfile)
            .Include(x => x.StaffProfile)
            .Include(x => x.DoctorProfile)
            .OrderBy(x => x.Role)
            .ThenBy(x => x.Email)
            .ToListAsync(cancellationToken);

        return Ok(users.Select(x => new UserListResponse(
            x.Id,
            x.Email,
            x.GetDisplayName(),
            x.Role,
            x.Status,
            x.LastLoginAt,
            x.CreatedAt,
            x.DoctorProfile?.IsAvailable)).ToList());
    }

    [HttpPost("users")]
    public async Task<ActionResult<UserListResponse>> CreateUser(AdminCreateUserRequest request, CancellationToken cancellationToken)
    {
        if (request.Role == UserRole.Patient)
        {
            return BadRequest(new { error = "Patients must be created through patient registration so required demographic fields can be collected." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            return Conflict(new { error = "An account with this email already exists." });
        }

        if (request.Role == UserRole.Doctor)
        {
            if (string.IsNullOrWhiteSpace(request.RegistrationNo))
                return BadRequest(new { error = "Registration number is required for a doctor account." });
            if (await dbContext.DoctorProfiles.AnyAsync(x => x.RegistrationNo == request.RegistrationNo, cancellationToken))
                return Conflict(new { error = "This doctor registration number already exists." });
        }

        var user = new User
        {
            Email = email,
            PasswordHash = "temporary",
            Role = request.Role,
            Status = UserStatus.Active
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        if (request.Role == UserRole.Doctor)
        {
            user.DoctorProfile = new DoctorProfile
            {
                User = user,
                FullName = request.FullName.Trim(),
                RegistrationNo = request.RegistrationNo!.Trim(),
                Speciality = request.Speciality?.Trim(),
                IsAvailable = true
            };
        }
        else
        {
            user.StaffProfile = new StaffProfile
            {
                User = user,
                FullName = request.FullName.Trim(),
                Position = request.Role == UserRole.Admin ? "System Administrator" : request.Position?.Trim() ?? "Clinic Staff",
                Phone = request.Phone?.Trim()
            };
        }

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(User.GetUserId(), "CREATE", "User", user.Id,
            new { user.Email, role = user.Role.ToString() }, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        return CreatedAtAction(nameof(Users), new UserListResponse(
            user.Id, user.Email, user.GetDisplayName(), user.Role, user.Status, user.LastLoginAt, user.CreatedAt, user.DoctorProfile?.IsAvailable));
    }

    [HttpPatch("users/{id:int}")]
    public async Task<ActionResult<UserListResponse>> UpdateUser(int id, AdminUpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(x => x.PatientProfile)
            .Include(x => x.StaffProfile)
            .Include(x => x.DoctorProfile)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");

        if (user.Role == UserRole.Admin && (request.Status != UserStatus.Active || request.Role != UserRole.Admin))
        {
            var activeAdmins = await dbContext.Users.CountAsync(x => x.Role == UserRole.Admin && x.Status == UserStatus.Active, cancellationToken);
            if (activeAdmins <= 1) return BadRequest(new { error = "The last active administrator cannot be disabled or reassigned." });
        }

        if (request.Role != user.Role)
        {
            var supportedTransition = (user.Role == UserRole.Staff && request.Role == UserRole.Admin)
                                      || (user.Role == UserRole.Admin && request.Role == UserRole.Staff);
            if (!supportedTransition)
            {
                return BadRequest(new { error = "For data integrity, only Staff and Admin roles can be changed directly. Create a new doctor account when needed." });
            }
            user.Role = request.Role;
            if (user.StaffProfile is not null)
            {
                user.StaffProfile.Position = request.Role == UserRole.Admin ? "System Administrator" : "Clinic Staff";
            }
        }

        user.Status = request.Status;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        if (user.DoctorProfile is not null && request.IsDoctorAvailable.HasValue)
        {
            user.DoctorProfile.IsAvailable = request.IsDoctorAvailable.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(User.GetUserId(), "UPDATE", "User", user.Id,
            new { status = user.Status.ToString(), role = user.Role.ToString(), doctorAvailable = user.DoctorProfile?.IsAvailable },
            HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        return Ok(new UserListResponse(user.Id, user.Email, user.GetDisplayName(), user.Role, user.Status, user.LastLoginAt, user.CreatedAt, user.DoctorProfile?.IsAvailable));
    }
}
