using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using System.Security.Claims;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/[controller]"), Authorize]
public class ImportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public ImportsController(AppDbContext db) => _db = db;

    [HttpPost("analyze")]
    public async Task<ActionResult<ImportAnalyzeResponse>> Analyze([FromBody] ImportAnalyzeRequest req)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var lines = req.Content
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (lines.Count == 0)
        {
            return Ok(new ImportAnalyzeResponse(req.ProjectId, req.SourceType, 0, 0, new List<string>(), new List<ImportPreviewRowDto>(), "Keine importierbaren Zeilen erkannt."));
        }

        var rows = lines.Select((line, index) =>
        {
            var values = line.Split(new[] { ';', ',', '\t' }, StringSplitOptions.TrimEntries).ToList();
            return new ImportPreviewRowDto(index + 1, values);
        }).ToList();

        var headers = rows.First().Values;
        var dataRows = rows.Skip(1).Take(8).ToList();
        var columnCount = rows.Max(row => row.Values.Count);

        return Ok(new ImportAnalyzeResponse(
            req.ProjectId,
            req.SourceType,
            Math.Max(rows.Count - 1, 0),
            columnCount,
            headers,
            dataRows,
            $"{Math.Max(rows.Count - 1, 0)} Datensaetze erkannt aus Quelle {req.SourceType}."
        ));
    }

    [HttpPost("commit")]
    public async Task<ActionResult<ImportCommitResponse>> Commit([FromBody] ImportCommitRequest req)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var lines = req.Content
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var importedRows = Math.Max(lines.Count - 1, 0);
        var summary = $"Import aus {req.SourceType}: {importedRows} Datensaetze analysiert. Quelle: {req.Title}";

        var note = new ProjectNote
        {
            ProjectId = req.ProjectId,
            AuthorId = UserId,
            Title = $"Import: {req.Title}",
            Content = summary
        };

        _db.ProjectNotes.Add(note);
        _db.ActivityLogs.Add(new ActivityLog
        {
            TenantId = TenantId,
            UserId = UserId,
            ProjectId = req.ProjectId,
            EntityId = note.Id,
            EntityType = "Import",
            Action = $"hat Import verarbeitet: {req.Title}"
        });
        await _db.SaveChangesAsync();

        return Ok(new ImportCommitResponse(req.ProjectId, req.Title, req.SourceType, importedRows, summary));
    }
}
