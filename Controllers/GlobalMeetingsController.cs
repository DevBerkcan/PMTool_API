using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using System.Security.Claims;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/meetings"), Authorize]
public class GlobalMeetingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public GlobalMeetingsController(AppDbContext db) => _db = db;

    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId") ?? Guid.Empty.ToString());

    // GET /api/v1/meetings
    [HttpGet]
    public async Task<ActionResult<List<GlobalMeetingDto>>> GetAll(
        [FromQuery] Guid? projectId,
        [FromQuery] string? status)
    {
        var query = _db.ProjectMeetings
            .Include(m => m.Project)
            .Include(m => m.CreatedBy)
            .Where(m => m.Project != null && m.Project.TenantId == TenantId);

        if (projectId.HasValue)
            query = query.Where(m => m.ProjectId == projectId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(m => m.ExtractionStatus == status);

        var meetings = await query
            .OrderByDescending(m => m.MeetingDate)
            .ToListAsync();

        var result = meetings.Select(m => new GlobalMeetingDto(
            Id: m.Id,
            ProjectId: m.ProjectId,
            ProjectName: m.Project?.Name ?? "",
            Title: m.Title,
            MeetingDate: m.MeetingDate,
            Participants: m.Participants,
            Location: m.Location,
            ExtractionStatus: m.ExtractionStatus,
            HasTranscript: !string.IsNullOrWhiteSpace(m.TranscriptRaw),
            Notes: m.Notes,
            CreatedByName: m.CreatedBy?.DisplayName ?? "",
            CreatedAt: m.CreatedAt,
            ExtractedTasksCount: null,
            ExtractedDecisionsCount: null,
            Summary: "",
            TeamsOnlineMeetingId: m.TeamsOnlineMeetingId
        )).ToList();

        return Ok(result);
    }
}
