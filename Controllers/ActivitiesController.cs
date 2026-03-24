using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using System.Security.Claims;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1"), Authorize]
public class ActivitiesController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    public ActivitiesController(AppDbContext db) => _db = db;

    [HttpGet("activities")]
    public async Task<ActionResult<List<ActivityDto>>> GetAll()
    {
        var logs = await _db.ActivityLogs.Where(a => a.TenantId == TenantId).Include(a => a.User).OrderByDescending(a => a.CreatedAt).Take(50).ToListAsync();
        return Ok(logs.Select(l => new ActivityDto(l.Id, l.User?.DisplayName ?? "", l.Action, l.EntityType, l.CreatedAt)));
    }

    [HttpGet("projects/{projectId}/activities")]
    public async Task<ActionResult<List<ActivityDto>>> GetByProject(Guid projectId)
    {
        var logs = await _db.ActivityLogs.Where(a => a.ProjectId == projectId && a.TenantId == TenantId).Include(a => a.User).OrderByDescending(a => a.CreatedAt).Take(30).ToListAsync();
        return Ok(logs.Select(l => new ActivityDto(l.Id, l.User?.DisplayName ?? "", l.Action, l.EntityType, l.CreatedAt)));
    }
}
