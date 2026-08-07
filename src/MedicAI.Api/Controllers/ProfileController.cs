using MedicAI.Api.Contracts;
using MedicAI.Api.Data;
using MedicAI.Api.Domain;
using MedicAI.Api.Extensions;
using MedicAI.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize(Roles = "Patient")]
public sealed class ProfileController(MedicAiDbContext dbContext, IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PatientProfileResponse>> Get(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var profile = await dbContext.PatientProfiles.Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        return profile is null ? NotFound() : Ok(ToResponse(profile));
    }

    [HttpPut]
    public async Task<ActionResult<PatientProfileResponse>> Update(UpdatePatientProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var profile = await dbContext.PatientProfiles.Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Patient profile was not found.");

        if (request.DateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new { error = "Date of birth must be in the past." });
        }

        profile.FullName = request.FullName.Trim();
        profile.DateOfBirth = request.DateOfBirth;
        profile.Gender = request.Gender?.Trim();
        profile.Phone = request.Phone.Trim();
        profile.Address = request.Address?.Trim();
        profile.Allergies = request.Allergies?.Trim();
        profile.ExistingConditions = request.ExistingConditions?.Trim();
        profile.EmergencyContact = request.EmergencyContact?.Trim();
        profile.User.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(userId, "UPDATE", "PatientProfile", profile.Id, new { fields = "patient-permitted-profile-fields" }, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok(ToResponse(profile));
    }

    private static PatientProfileResponse ToResponse(PatientProfile profile) => new(
        profile.Id,
        profile.UserId,
        profile.User.Email,
        profile.FullName,
        profile.DateOfBirth,
        profile.Gender,
        profile.Phone,
        profile.Address,
        profile.Allergies,
        profile.ExistingConditions,
        profile.EmergencyContact);
}
