using MedicAI.Api.Contracts;
using MedicAI.Api.Data;
using MedicAI.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicAI.Api.Controllers;

[ApiController]
[Route("api/consultations")]
[Authorize]
public sealed class ConsultationsController(MedicAiDbContext dbContext) : ControllerBase
{
    [HttpGet("mine")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<IReadOnlyList<ConsultationResponse>>> Mine(CancellationToken cancellationToken)
    {
        var patientId = await dbContext.PatientProfiles
            .Where(x => x.UserId == User.GetUserId())
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);

        var consultations = await dbContext.Consultations
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return Ok(consultations.Select(x => x.ToResponse()).ToList());
    }
}
