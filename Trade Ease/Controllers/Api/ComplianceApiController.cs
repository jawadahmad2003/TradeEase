using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeEase.Data;
using TradeEase.Models.Dtos;
using TradeEase.Models.Entities;

namespace TradeEase.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Trader")]
public class ComplianceController : ApiControllerBase
{
    private readonly TradeEaseDbContext _db;

    public ComplianceController(TradeEaseDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ComplianceStepDto>>> List()
    {
        var userId = User.GetUserId();
        var steps = await _db.ComplianceSteps.AsNoTracking().OrderBy(s => s.StepOrder).ToListAsync();
        var progress = await _db.UserComplianceProgress.AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.StepId);

        return Ok(steps.Select(s =>
        {
            progress.TryGetValue(s.StepId, out var p);
            return new ComplianceStepDto(
                s.StepId, s.StepOrder, s.StepName, s.Description,
                s.EstimatedTime, s.PortalUrl,
                p?.IsCompleted ?? false, p?.CompletedAt, p?.Notes);
        }));
    }

    [HttpPatch("{stepId:int}")]
    public async Task<ActionResult<ComplianceStepDto>> Update(int stepId, [FromBody] UpdateComplianceRequest request)
    {
        var step = await _db.ComplianceSteps.FindAsync(stepId);
        if (step is null) return NotFound();

        var userId = User.GetUserId();
        var progress = await _db.UserComplianceProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.StepId == stepId);

        if (progress is null)
        {
            progress = new UserComplianceProgress
            {
                UserId = userId,
                StepId = stepId,
                IsCompleted = request.IsCompleted,
                CompletedAt = request.IsCompleted ? DateTime.UtcNow : null,
                Notes = request.Notes,
            };
            _db.UserComplianceProgress.Add(progress);
        }
        else
        {
            progress.IsCompleted = request.IsCompleted;
            progress.CompletedAt = request.IsCompleted ? DateTime.UtcNow : null;
            progress.Notes = request.Notes;
        }

        await _db.SaveChangesAsync();

        return Ok(new ComplianceStepDto(
            step.StepId, step.StepOrder, step.StepName, step.Description,
            step.EstimatedTime, step.PortalUrl,
            progress.IsCompleted, progress.CompletedAt, progress.Notes));
    }
}
