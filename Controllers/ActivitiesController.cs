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

    [HttpGet("audit")]
    public async Task<ActionResult<List<AuditEntryDto>>> GetAudit([FromQuery] string? entityType = null, [FromQuery] Guid? projectId = null, [FromQuery] string? userRole = null, [FromQuery] string? changeType = null, [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null)
    {
        var query = _db.AuditEntries
            .Where(entry => entry.TenantId == TenantId)
            .Include(entry => entry.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(entry => entry.EntityType == entityType);
        if (projectId.HasValue) query = query.Where(entry => entry.ProjectId == projectId.Value);
        if (!string.IsNullOrWhiteSpace(userRole)) query = query.Where(entry => entry.User!.Role == userRole);
        if (!string.IsNullOrWhiteSpace(changeType)) query = query.Where(entry => entry.ChangeType == changeType);
        if (dateFrom.HasValue) query = query.Where(entry => entry.CreatedAt >= dateFrom.Value);
        if (dateTo.HasValue) query = query.Where(entry => entry.CreatedAt <= dateTo.Value);

        var entries = await query
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(80)
            .ToListAsync();

        return Ok(entries.Select(MapAudit));
    }

    [HttpGet("projects/{projectId}/audit")]
    public async Task<ActionResult<List<AuditEntryDto>>> GetProjectAudit(Guid projectId)
    {
        var entries = await _db.AuditEntries
            .Where(entry => entry.ProjectId == projectId && entry.TenantId == TenantId)
            .Include(entry => entry.User)
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(40)
            .ToListAsync();

        return Ok(entries.Select(MapAudit));
    }

    private static AuditEntryDto MapAudit(AuditEntry entry) => new(
        entry.Id,
        entry.ProjectId,
        entry.User?.DisplayName ?? "",
        entry.User?.Role ?? "",
        entry.EntityType,
        entry.ChangeType,
        entry.Title,
        entry.FromValue,
        entry.ToValue,
        entry.Detail,
        entry.CreatedAt
    );
}
