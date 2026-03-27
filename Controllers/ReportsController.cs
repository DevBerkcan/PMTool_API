using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using PmTool.Api.Security;
using System.Security.Claims;
using System.Text;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/reports"), Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? "";

    public ReportsController(AppDbContext db) => _db = db;

    [HttpGet("audit.csv")]
    public async Task<IActionResult> ExportAuditCsv([FromQuery] string? entityType = null, [FromQuery] Guid? projectId = null, [FromQuery] string? userRole = null, [FromQuery] string? changeType = null, [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null)
    {
        if (!RoleAccess.CanManagePmo(CurrentRole)) return Forbid();

        var entries = await BuildAuditQuery(entityType, projectId, userRole, changeType, dateFrom, dateTo)
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(500)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("CreatedAt,ProjectId,UserName,UserRole,EntityType,ChangeType,Title,FromValue,ToValue,Detail");
        foreach (var entry in entries)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                Csv(entry.CreatedAt.ToString("s")),
                Csv(entry.ProjectId?.ToString() ?? ""),
                Csv(entry.User?.DisplayName ?? ""),
                Csv(entry.User?.Role ?? ""),
                Csv(entry.EntityType),
                Csv(entry.ChangeType),
                Csv(entry.Title),
                Csv(entry.FromValue),
                Csv(entry.ToValue),
                Csv(entry.Detail),
            }));
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "audit-export.csv");
    }

    [HttpGet("audit.pdf")]
    public async Task<IActionResult> ExportAuditPdf([FromQuery] string? entityType = null, [FromQuery] Guid? projectId = null, [FromQuery] string? userRole = null, [FromQuery] string? changeType = null, [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null)
    {
        if (!RoleAccess.CanManagePmo(CurrentRole)) return Forbid();

        var entries = await BuildAuditQuery(entityType, projectId, userRole, changeType, dateFrom, dateTo)
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(20)
            .ToListAsync();

        var lines = new List<string> { "Audit Export" };
        lines.AddRange(entries.Select(entry => $"{entry.CreatedAt:dd.MM.yyyy} | {entry.User?.DisplayName} | {entry.EntityType} | {entry.Title}"));
        return File(BuildSimplePdf(lines), "application/pdf", "audit-export.pdf");
    }

    [HttpGet("projects/{projectId}/forecast.csv")]
    public async Task<IActionResult> ExportForecastCsv(Guid projectId)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(item => item.Id == projectId && item.TenantId == TenantId);
        if (project == null) return NotFound();

        var snapshots = await _db.ProjectForecastSnapshots
            .Where(snapshot => snapshot.ProjectId == projectId)
            .OrderBy(snapshot => snapshot.SnapshotDate)
            .Take(52)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("SnapshotDate,BudgetAtCompletion,ActualCost,EarnedValue,PlannedValue,EstimateAtCompletion,EstimateToComplete,CPI,SPI,RemainingHours");
        foreach (var snapshot in snapshots)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                Csv(snapshot.SnapshotDate.ToString("yyyy-MM-dd")),
                Csv(snapshot.BudgetAtCompletion.ToString("0.##")),
                Csv(snapshot.ActualCost.ToString("0.##")),
                Csv(snapshot.EarnedValue.ToString("0.##")),
                Csv(snapshot.PlannedValue.ToString("0.##")),
                Csv(snapshot.EstimateAtCompletion.ToString("0.##")),
                Csv(snapshot.EstimateToComplete.ToString("0.##")),
                Csv(snapshot.CostPerformanceIndex.ToString("0.####")),
                Csv(snapshot.SchedulePerformanceIndex.ToString("0.####")),
                Csv(snapshot.RemainingHours.ToString())
            }));
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"{project.Name}-forecast.csv");
    }

    [HttpGet("projects/{projectId}/forecast.pdf")]
    public async Task<IActionResult> ExportForecastPdf(Guid projectId)
    {
        var project = await _db.Projects
            .Include(item => item.ForecastSnapshots)
            .FirstOrDefaultAsync(item => item.Id == projectId && item.TenantId == TenantId);

        if (project == null) return NotFound();

        var latest = project.ForecastSnapshots.OrderByDescending(snapshot => snapshot.SnapshotDate).FirstOrDefault();
        var lines = new List<string> { $"Forecast Export - {project.Name}" };
        if (latest == null)
        {
            lines.Add("Keine Forecast-Snapshots verfuegbar.");
        }
        else
        {
            lines.Add($"Snapshot: {latest.SnapshotDate:dd.MM.yyyy}");
            lines.Add($"BAC: {latest.BudgetAtCompletion:0.##}");
            lines.Add($"AC: {latest.ActualCost:0.##}");
            lines.Add($"EV: {latest.EarnedValue:0.##}");
            lines.Add($"PV: {latest.PlannedValue:0.##}");
            lines.Add($"EAC: {latest.EstimateAtCompletion:0.##}");
            lines.Add($"CPI: {latest.CostPerformanceIndex:0.##}");
            lines.Add($"SPI: {latest.SchedulePerformanceIndex:0.##}");
            lines.Add($"Remaining Hours: {latest.RemainingHours}");
        }

        return File(BuildSimplePdf(lines), "application/pdf", $"{project.Name}-forecast.pdf");
    }

    private IQueryable<AuditEntry> BuildAuditQuery(string? entityType, Guid? projectId, string? userRole, string? changeType, DateTime? dateFrom, DateTime? dateTo)
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

        return query;
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static byte[] BuildSimplePdf(IEnumerable<string> lines)
    {
        var safeLines = lines.Select(line => line.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)")).ToList();
        var content = "BT /F1 12 Tf 40 800 Td 14 TL " + string.Join(" T* ", safeLines.Select(line => $"({line}) Tj")) + " ET";
        var objects = new List<string>
        {
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj",
            "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj",
            "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj",
            "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj",
            $"5 0 obj << /Length {Encoding.ASCII.GetByteCount(content)} >> stream\n{content}\nendstream endobj"
        };

        var builder = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(obj).Append('\n');
        }

        var xrefStart = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n0 ").Append(objects.Count + 1).Append('\n');
        builder.Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        builder.Append("trailer << /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\nstartxref\n").Append(xrefStart).Append("\n%%EOF");
        return Encoding.ASCII.GetBytes(builder.ToString());
    }
}
