using MedicAI.Api.Contracts;
using MedicAI.Api.Data;
using MedicAI.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Api.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize]
public sealed class DoctorsController(MedicAiDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DoctorSummaryResponse>>> Get(CancellationToken cancellationToken)
    {
        var doctors = await dbContext.DoctorProfiles
            .AsNoTracking()
            .Where(x => x.User.Status == UserStatus.Active)
            .OrderBy(x => x.FullName)
            .Select(x => new DoctorSummaryResponse(x.Id, x.FullName, x.RegistrationNo, x.Speciality, x.IsAvailable))
            .ToListAsync(cancellationToken);
        return Ok(doctors);
    }
}
