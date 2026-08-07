using MedicAI.Api.Data;
using MedicAI.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Api.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize(Roles = "Staff,Doctor,Admin")]
public sealed class PatientsController(MedicAiDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<object>>> Search([FromQuery] string? search, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        var query = dbContext.PatientProfiles.AsNoTracking().Include(x => x.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();
            query = query.Where(x => x.FullName.ToLower().Contains(value) || x.Phone.Contains(value) || x.User.Email.ToLower().Contains(value));
        }

        var patients = await query.OrderBy(x => x.FullName).Take(take)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.Phone,
                x.User.Email,
                x.DateOfBirth,
                x.Allergies,
                x.ExistingConditions
            })
            .ToListAsync(cancellationToken);
        return Ok(patients);
    }
}
