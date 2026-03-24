using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using System.Security.Claims;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/projects/{projectId}/risks"), Authorize]
public class RisksController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId   => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public RisksController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<RiskDto>>> GetAll(Guid projectId)
    {
        var risks = await _db.Risks.Where(r => r.ProjectId == projectId).Include(r => r.Owner).OrderByDescending(r => r.Impact * r.Probability).ToListAsync();
        return Ok(risks.Select(r => new RiskDto(r.Id, r.ProjectId, r.Title, r.Description, r.Impact, r.Probability, r.Impact * r.Probability, r.Status, r.Mitigation, r.Owner?.DisplayName ?? "", r.CreatedAt)));
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateRiskRequest req)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == TenantId);
        if (project == null) return NotFound();
        var risk = new Risk { ProjectId = projectId, Title = req.Title, Description = req.Description, Impact = req.Impact, Probability = req.Probability, Mitigation = req.Mitigation, OwnerId = req.OwnerId };
        _db.Risks.Add(risk);
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = projectId, EntityId = risk.Id, EntityType = "Risk", Action = $"Risiko erfasst: {risk.Title}" });
        await _db.SaveChangesAsync(); return Ok();
    }

    [HttpDelete("{riskId}")]
    public async Task<IActionResult> Delete(Guid projectId, Guid riskId)
    {
        var risk = await _db.Risks.FirstOrDefaultAsync(r => r.Id == riskId && r.ProjectId == projectId);
        if (risk == null) return NotFound();
        _db.Risks.Remove(risk); await _db.SaveChangesAsync(); return NoContent();
    }
}
