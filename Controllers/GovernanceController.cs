using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using System.Security.Claims;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/[controller]"), Authorize]
public class GovernanceController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);

    public GovernanceController(AppDbContext db) => _db = db;

    [HttpGet("overview")]
    public async Task<ActionResult<GovernanceOverviewDto>> GetOverview()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var projects = await _db.Projects
            .Where(p => p.TenantId == TenantId)
            .Include(p => p.Decisions)
            .Include(p => p.Milestones)
            .Include(p => p.GovernanceChecks)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var items = projects.Select(project => new GovernanceOverviewProjectDto(
            project.Id,
            project.Name,
            project.Category,
            project.Stage,
            project.Status,
            project.GovernanceChecks.Count(check => check.Status != "done"),
            project.Decisions.Count(decision => decision.Status == "open" || decision.Status == "review"),
            project.Milestones.Count(milestone => milestone.DueDate < today && milestone.Status != "done")
        )).ToList();

        return Ok(new GovernanceOverviewDto(
            items,
            items.Count,
            items.Sum(item => item.OpenGovernanceChecks),
            items.Sum(item => item.OpenDecisions),
            items.Sum(item => item.OverdueMilestones)
        ));
    }
}
