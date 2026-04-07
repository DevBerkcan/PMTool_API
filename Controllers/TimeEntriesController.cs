using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using PmTool.Api.Security;
using System.Security.Claims;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/time-entries"), Authorize]
public class TimeEntriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? "";
    private bool IsManager => RoleAccess.CanViewAllTimeEntries(CurrentRole);

    public TimeEntriesController(AppDbContext db) => _db = db;

    // ──────────────────────────────────────────────────────────────────
    // GET /api/v1/time-entries?projectId=&year=&month=
    // Employee: own entries only (project must be assigned)
    // Manager: any project in tenant
    // ──────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<ActionResult<TimeEntriesResponse>> GetEntries(
        [FromQuery] Guid projectId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
            return BadRequest("Invalid year or month.");

        // Verify access
        if (!IsManager)
        {
            var isAssigned = await _db.ResourceAllocations
                .AnyAsync(ra => ra.ProjectId == projectId && ra.UserId == UserId);
            if (!isAssigned)
                return Forbid();
        }

        var project = await _db.Projects
            .Where(p => p.TenantId == TenantId && p.Id == projectId)
            .Select(p => new { p.Id, p.Name, p.BudgetTotal, p.Customer })
            .FirstOrDefaultAsync();

        if (project == null) return NotFound("Project not found.");

        // Load existing entries for this user/project/month
        var targetUserId = IsManager ? UserId : UserId; // employees always see their own
        var entries = await _db.TimeEntries
            .Where(t => t.TenantId == TenantId && t.ProjectId == projectId
                     && t.UserId == UserId && t.Year == year && t.Month == month)
            .ToListAsync();

        // Build full month grid (all days)
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var dayEntries = new List<TimeEntryDayDto>();

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            var existing = entries.FirstOrDefault(e => e.Day == day);
            var isoWeek = GetIsoWeek(date);

            dayEntries.Add(new TimeEntryDayDto
            {
                Day = day,
                Weekday = GetWeekdayAbbrev(date.DayOfWeek),
                WeekNumber = isoWeek,
                IsWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                GeleistetHours = existing?.GeleistetHours ?? 0,
                FakturiertHours = existing?.FakturiertHours ?? 0,
                Comment = existing?.Comment ?? "",
                Id = existing?.Id
            });
        }

        // Total sums
        var totalGeleistet = entries.Sum(e => e.GeleistetHours);
        var totalFakturiert = entries.Sum(e => e.FakturiertHours);

        // Determine submission status for this month
        var hasEntries = entries.Count > 0;
        var allSubmitted = hasEntries && entries.All(e => e.Status == "submitted");
        var status = allSubmitted ? "submitted" : "draft";
        DateTime? submittedAt = allSubmitted ? entries.Max(e => e.SubmittedAt) : null;

        // Service type from first entry or default
        var serviceType = entries.FirstOrDefault()?.ServiceType ?? "";

        return Ok(new TimeEntriesResponse
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            Customer = project.Customer,
            BudgetTotal = project.BudgetTotal,
            Year = year,
            Month = month,
            ServiceType = serviceType,
            TotalGeleistet = totalGeleistet,
            TotalFakturiert = totalFakturiert,
            Status = status,
            SubmittedAt = submittedAt,
            Days = dayEntries
        });
    }

    // ──────────────────────────────────────────────────────────────────
    // PUT /api/v1/time-entries/bulk  – Save draft entries
    // ──────────────────────────────────────────────────────────────────
    [HttpPut("bulk")]
    public async Task<IActionResult> SaveBulk([FromBody] BulkSaveRequest req)
    {
        if (req.Year < 2000 || req.Year > 2100 || req.Month < 1 || req.Month > 12)
            return BadRequest("Invalid year or month.");

        // Verify project access
        if (!IsManager)
        {
            var isAssigned = await _db.ResourceAllocations
                .AnyAsync(ra => ra.ProjectId == req.ProjectId && ra.UserId == UserId);
            if (!isAssigned) return Forbid();
        }

        // Block editing already-submitted months
        var alreadySubmitted = await _db.TimeEntries
            .AnyAsync(t => t.TenantId == TenantId && t.ProjectId == req.ProjectId
                        && t.UserId == UserId && t.Year == req.Year
                        && t.Month == req.Month && t.Status == "submitted");
        if (alreadySubmitted)
            return BadRequest("Diese Periode wurde bereits eingereicht und kann nicht mehr bearbeitet werden.");

        var existing = await _db.TimeEntries
            .Where(t => t.TenantId == TenantId && t.ProjectId == req.ProjectId
                     && t.UserId == UserId && t.Year == req.Year && t.Month == req.Month)
            .ToListAsync();

        foreach (var dayReq in req.Days)
        {
            if (dayReq.GeleistetHours == 0 && dayReq.FakturiertHours == 0 && string.IsNullOrWhiteSpace(dayReq.Comment))
            {
                // Remove empty entries
                var toDelete = existing.FirstOrDefault(e => e.Day == dayReq.Day);
                if (toDelete != null) _db.TimeEntries.Remove(toDelete);
                continue;
            }

            var entry = existing.FirstOrDefault(e => e.Day == dayReq.Day);
            if (entry == null)
            {
                entry = new TimeEntry
                {
                    TenantId = TenantId,
                    ProjectId = req.ProjectId,
                    UserId = UserId,
                    Year = req.Year,
                    Month = req.Month,
                    Day = dayReq.Day,
                    Status = "draft"
                };
                _db.TimeEntries.Add(entry);
            }

            entry.GeleistetHours = dayReq.GeleistetHours;
            entry.FakturiertHours = dayReq.FakturiertHours;
            entry.Comment = dayReq.Comment ?? "";
            entry.ServiceType = req.ServiceType ?? "";
            entry.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Gespeichert." });
    }

    // ──────────────────────────────────────────────────────────────────
    // POST /api/v1/time-entries/submit  – Submit month for review
    // ──────────────────────────────────────────────────────────────────
    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitRequest req)
    {
        if (req.Year < 2000 || req.Year > 2100 || req.Month < 1 || req.Month > 12)
            return BadRequest("Invalid year or month.");

        if (!IsManager)
        {
            var isAssigned = await _db.ResourceAllocations
                .AnyAsync(ra => ra.ProjectId == req.ProjectId && ra.UserId == UserId);
            if (!isAssigned) return Forbid();
        }

        var entries = await _db.TimeEntries
            .Where(t => t.TenantId == TenantId && t.ProjectId == req.ProjectId
                     && t.UserId == UserId && t.Year == req.Year
                     && t.Month == req.Month && t.Status == "draft")
            .ToListAsync();

        if (entries.Count == 0)
            return BadRequest("Keine Einträge zum Einreichen gefunden.");

        var now = DateTime.UtcNow;
        foreach (var e in entries)
        {
            e.Status = "submitted";
            e.SubmittedAt = now;
            e.UpdatedAt = now;
        }

        // Load submitter info
        var submitter = await _db.Users.FindAsync(UserId);
        var project = await _db.Projects.FindAsync(req.ProjectId);
        var monthName = new DateTime(req.Year, req.Month, 1).ToString("MMMM yyyy");

        var totalGeleistet = entries.Sum(e => e.GeleistetHours);
        var message = $"{submitter?.DisplayName ?? "Unbekannt"} hat Zeiten für {project?.Name ?? "Projekt"} ({monthName}) eingereicht – {totalGeleistet}h geleistet";

        // Create notifications for all managers in this tenant
        var managers = await _db.Users
            .Where(u => u.TenantId == TenantId
                     && (u.Role == "Admin" || u.Role == "Management" || u.Role == "PMO" || u.Role == "Projektleiter")
                     && u.Id != UserId)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var managerId in managers)
        {
            _db.TimeEntryNotifications.Add(new TimeEntryNotification
            {
                TenantId = TenantId,
                ForUserId = managerId,
                SubmittedByUserId = UserId,
                ProjectId = req.ProjectId,
                Year = req.Year,
                Month = req.Month,
                Message = message,
                IsRead = false
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Zeiten erfolgreich eingereicht." });
    }

    // ──────────────────────────────────────────────────────────────────
    // GET /api/v1/time-entries/dashboard?year=&month=
    // Manager only: all entries across all projects
    // ──────────────────────────────────────────────────────────────────
    [HttpGet("dashboard")]
    public async Task<ActionResult<List<TimeEntryDashboardRow>>> GetDashboard(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        if (!IsManager) return Forbid();

        if (year < 2000 || year > 2100 || month < 1 || month > 12)
            return BadRequest("Invalid year or month.");

        var entries = await _db.TimeEntries
            .Where(t => t.TenantId == TenantId && t.Year == year && t.Month == month)
            .Include(t => t.User)
            .Include(t => t.Project)
            .ToListAsync();

        var rows = entries
            .GroupBy(e => new { e.UserId, e.ProjectId })
            .Select(g => new TimeEntryDashboardRow
            {
                UserId = g.Key.UserId,
                UserName = g.First().User?.DisplayName ?? "",
                UserEmail = g.First().User?.Email ?? "",
                ProjectId = g.Key.ProjectId,
                ProjectName = g.First().Project?.Name ?? "",
                TotalGeleistet = g.Sum(e => e.GeleistetHours),
                TotalFakturiert = g.Sum(e => e.FakturiertHours),
                Status = g.All(e => e.Status == "submitted") ? "submitted" : "draft",
                SubmittedAt = g.Where(e => e.Status == "submitted").Max(e => e.SubmittedAt),
                DayEntries = g.OrderBy(e => e.Day).Select(e => new TimeEntryDayDto
                {
                    Day = e.Day,
                    Weekday = GetWeekdayAbbrev(new DateTime(e.Year, e.Month, e.Day).DayOfWeek),
                    WeekNumber = GetIsoWeek(new DateTime(e.Year, e.Month, e.Day)),
                    IsWeekend = new DateTime(e.Year, e.Month, e.Day).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                    GeleistetHours = e.GeleistetHours,
                    FakturiertHours = e.FakturiertHours,
                    Comment = e.Comment,
                    Id = e.Id
                }).ToList()
            })
            .OrderBy(r => r.UserName)
            .ThenBy(r => r.ProjectName)
            .ToList();

        return Ok(rows);
    }

    // ──────────────────────────────────────────────────────────────────
    // GET /api/v1/time-entries/my-projects
    // Employee: list projects assigned to me (for navigation)
    // ──────────────────────────────────────────────────────────────────
    [HttpGet("my-projects")]
    public async Task<ActionResult<List<MyProjectDto>>> GetMyProjects()
    {
        var projectIds = await _db.ResourceAllocations
            .Where(ra => ra.UserId == UserId)
            .Select(ra => ra.ProjectId)
            .ToListAsync();

        var projects = await _db.Projects
            .Where(p => p.TenantId == TenantId && projectIds.Contains(p.Id))
            .Select(p => new MyProjectDto { Id = p.Id, Name = p.Name, Customer = p.Customer, Status = p.Status })
            .OrderBy(p => p.Name)
            .ToListAsync();

        return Ok(projects);
    }

    // ──────────────────────────────────────────────────────────────────
    // GET /api/v1/time-entries/notifications
    // Manager only: unread submission notifications
    // ──────────────────────────────────────────────────────────────────
    [HttpGet("notifications")]
    public async Task<ActionResult<List<TimeEntryNotificationDto>>> GetNotifications()
    {
        if (!IsManager) return Ok(new List<TimeEntryNotificationDto>());

        var notifs = await _db.TimeEntryNotifications
            .Where(n => n.TenantId == TenantId && n.ForUserId == UserId)
            .Include(n => n.SubmittedBy)
            .Include(n => n.Project)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new TimeEntryNotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                ProjectId = n.ProjectId,
                ProjectName = n.Project != null ? n.Project.Name : "",
                SubmittedByName = n.SubmittedBy != null ? n.SubmittedBy.DisplayName : "",
                SubmittedByUserId = n.SubmittedByUserId,
                Year = n.Year,
                Month = n.Month
            })
            .ToListAsync();

        return Ok(notifs);
    }

    // ──────────────────────────────────────────────────────────────────
    // POST /api/v1/time-entries/notifications/{id}/read
    // ──────────────────────────────────────────────────────────────────
    [HttpPost("notifications/{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        if (!IsManager) return Forbid();

        var notif = await _db.TimeEntryNotifications
            .FirstOrDefaultAsync(n => n.Id == id && n.TenantId == TenantId && n.ForUserId == UserId);

        if (notif == null) return NotFound();

        notif.IsRead = true;
        notif.ReadAt = DateTime.UtcNow;
        notif.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok();
    }

    // ──────────────────────────────────────────────────────────────────
    // POST /api/v1/time-entries/notifications/read-all
    // ──────────────────────────────────────────────────────────────────
    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        if (!IsManager) return Forbid();

        var unread = await _db.TimeEntryNotifications
            .Where(n => n.TenantId == TenantId && n.ForUserId == UserId && !n.IsRead)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
            n.UpdatedAt = now;
        }

        await _db.SaveChangesAsync();
        return Ok(new { count = unread.Count });
    }

    // ──────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────
    private static string GetWeekdayAbbrev(DayOfWeek dow) => dow switch
    {
        DayOfWeek.Monday => "Mo",
        DayOfWeek.Tuesday => "Di",
        DayOfWeek.Wednesday => "Mi",
        DayOfWeek.Thursday => "Do",
        DayOfWeek.Friday => "Fr",
        DayOfWeek.Saturday => "Sa",
        DayOfWeek.Sunday => "So",
        _ => ""
    };

    private static int GetIsoWeek(DateTime date)
    {
        var day = (int)date.DayOfWeek;
        if (day == 0) day = 7;
        var thursday = date.AddDays(4 - day);
        var jan1 = new DateTime(thursday.Year, 1, 1);
        return (int)Math.Ceiling((thursday - jan1).TotalDays / 7) + 1;
    }
}

// ──────────────────────────────────────────────────────────────────────
// DTOs
// ──────────────────────────────────────────────────────────────────────

public record TimeEntryDayDto
{
    public Guid? Id { get; init; }
    public int Day { get; init; }
    public string Weekday { get; init; } = "";
    public int WeekNumber { get; init; }
    public bool IsWeekend { get; init; }
    public decimal GeleistetHours { get; set; }
    public decimal FakturiertHours { get; set; }
    public string Comment { get; set; } = "";
}

public record TimeEntriesResponse
{
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = "";
    public string Customer { get; init; } = "";
    public decimal BudgetTotal { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public string ServiceType { get; init; } = "";
    public decimal TotalGeleistet { get; init; }
    public decimal TotalFakturiert { get; init; }
    public string Status { get; init; } = "draft";
    public DateTime? SubmittedAt { get; init; }
    public List<TimeEntryDayDto> Days { get; init; } = new();
}

public record TimeEntryDashboardRow
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = "";
    public string UserEmail { get; init; } = "";
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = "";
    public decimal TotalGeleistet { get; init; }
    public decimal TotalFakturiert { get; init; }
    public string Status { get; init; } = "draft";
    public DateTime? SubmittedAt { get; init; }
    public List<TimeEntryDayDto> DayEntries { get; init; } = new();
}

public record TimeEntryNotificationDto
{
    public Guid Id { get; init; }
    public string Message { get; init; } = "";
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = "";
    public string SubmittedByName { get; init; } = "";
    public Guid SubmittedByUserId { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
}

public record BulkSaveRequest
{
    public Guid ProjectId { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public string? ServiceType { get; init; }
    public List<DaySaveDto> Days { get; init; } = new();
}

public record DaySaveDto
{
    public int Day { get; init; }
    public decimal GeleistetHours { get; init; }
    public decimal FakturiertHours { get; init; }
    public string? Comment { get; init; }
}

public record SubmitRequest
{
    public Guid ProjectId { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
}

public record MyProjectDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Customer { get; init; } = "";
    public string Status { get; init; } = "";
}
