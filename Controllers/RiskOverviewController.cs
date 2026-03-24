using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using System.Security.Claims;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/risks"), Authorize]
public class RiskOverviewController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);

    public RiskOverviewController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<RiskDto>>> GetAll()
    {
        var risks = await _db.Risks
            .Where(r => r.Project != null && r.Project.TenantId == TenantId)
            .Include(r => r.Owner)
            .Include(r => r.Project)
            .OrderByDescending(r => r.Impact * r.Probability)
            .ToListAsync();

        return Ok(risks.Select(r => new RiskDto(
            r.Id,
            r.ProjectId,
            r.Title,
            r.Description,
            r.Impact,
            r.Probability,
            r.Impact * r.Probability,
            r.Status,
            r.Mitigation,
            r.Owner?.DisplayName ?? "",
            r.CreatedAt
        )));
    }
}
