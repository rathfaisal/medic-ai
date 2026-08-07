using MedicAI.Api.Contracts;
using MedicAI.Api.Data;
using MedicAI.Api.Domain;
using MedicAI.Api.Extensions;
using MedicAI.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    MedicAiDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokenService,
    IAuditService auditService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthUserResponse>> Register(RegisterPatientRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            return Conflict(new { error = "An account with this email already exists." });
        }

        if (request.DateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new { error = "Date of birth must be in the past." });
        }

        var user = new User
        {
            Email = email,
            PasswordHash = "temporary",
            Role = UserRole.Patient,
            Status = UserStatus.Active
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        user.PatientProfile = new PatientProfile
        {
            User = user,
            FullName = request.FullName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender?.Trim(),
            Phone = request.Phone.Trim()
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            user.Id,
            "REGISTER",
            "User",
            user.Id,
            new { role = user.Role.ToString() },
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        var token = jwtTokenService.CreateToken(user, user.PatientProfile.FullName);
        return CreatedAtAction(nameof(Me), new AuthUserResponse(user.Id, user.Email, user.Role, user.PatientProfile.FullName, token));
    }

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<ActionResult<AuthUserResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .Include(x => x.PatientProfile)
            .Include(x => x.StaffProfile)
            .Include(x => x.DoctorProfile)
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null)
        {
            await auditService.WriteAsync(null, "LOGIN_FAILED", "User", null, new { email }, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            await auditService.WriteAsync(user.Id, "LOGIN_FAILED", "User", user.Id, null, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
            return Unauthorized(new { error = "Invalid email or password." });
        }

        if (user.Status != UserStatus.Active)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = $"This account is {user.Status}. Contact the administrator." });
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(user.Id, "LOGIN", "User", user.Id, null, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        var displayName = user.GetDisplayName();
        var token = jwtTokenService.CreateToken(user, displayName);
        return Ok(new AuthUserResponse(user.Id, user.Email, user.Role, displayName, token));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<object>> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await dbContext.Users
            .Include(x => x.PatientProfile)
            .Include(x => x.StaffProfile)
            .Include(x => x.DoctorProfile)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        return user is null
            ? NotFound()
            : Ok(new { user.Id, user.Email, user.Role, user.Status, DisplayName = user.GetDisplayName() });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await auditService.WriteAsync(userId, "LOGOUT", "User", userId, null, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok(new { message = "Logged out. Remove the access token from the client." });
    }
}
