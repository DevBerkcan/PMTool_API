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

    [HttpPost("meetings/analyze")]
    public async Task<ActionResult<MeetingAnalyzeResponse>> AnalyzeMeeting([FromBody] MeetingAnalyzeRequest req)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var extracted = ExtractMeeting(req.Title, req.Content);

        return Ok(new MeetingAnalyzeResponse(
            req.ProjectId,
            req.SourceType,
            req.Title,
            extracted.Summary,
            extracted.Actions,
            extracted.Decisions,
            extracted.Risks,
            extracted.Knowledge
        ));
    }

    [HttpPost("meetings/commit")]
    public async Task<ActionResult<MeetingCommitResponse>> CommitMeeting([FromBody] MeetingCommitRequest req)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var extracted = ExtractMeeting(req.Title, req.Content);

        var note = new ProjectNote
        {
            ProjectId = req.ProjectId,
            AuthorId = UserId,
            Title = $"Meeting: {req.Title}",
            Content = extracted.Summary
        };
        _db.ProjectNotes.Add(note);

        foreach (var action in extracted.Actions)
        {
            _db.Tasks.Add(new ProjectTask
            {
                ProjectId = req.ProjectId,
                Title = action.Title,
                Description = action.Detail,
                Status = "todo",
                Priority = "medium",
                AssigneeId = UserId,
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                EstimatedHours = 4
            });
        }

        foreach (var decision in extracted.Decisions)
        {
            _db.ProjectDecisions.Add(new ProjectDecision
            {
                ProjectId = req.ProjectId,
                OwnerId = UserId,
                Title = decision.Title,
                Context = decision.Detail,
                Decision = "Aus Meeting erkannt, noch zu finalisieren.",
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                Status = "open"
            });
        }

        foreach (var risk in extracted.Risks)
        {
            _db.Risks.Add(new Risk
            {
                ProjectId = req.ProjectId,
                OwnerId = UserId,
                Title = risk.Title,
                Description = risk.Detail,
                Impact = 4,
                Probability = 3,
                Status = "open",
                Mitigation = "Im Folgemeeting bewerten und Massnahme festlegen."
            });
        }

        foreach (var item in extracted.Knowledge)
        {
            _db.ProjectKnowledgeItems.Add(new ProjectKnowledgeItem
            {
                ProjectId = req.ProjectId,
                AuthorId = UserId,
                Title = item.Title,
                SourceType = "meeting",
                SourceLabel = req.Title,
                Content = item.Detail,
                TagsCsv = "teams|meeting|knowledge",
                Importance = 4
            });
        }

        _db.ActivityLogs.Add(new ActivityLog
        {
            TenantId = TenantId,
            UserId = UserId,
            ProjectId = req.ProjectId,
            EntityId = note.Id,
            EntityType = "MeetingImport",
            Action = $"hat Meeting importiert: {req.Title}"
        });

        await _db.SaveChangesAsync();

        return Ok(new MeetingCommitResponse(
            req.ProjectId,
            req.Title,
            extracted.Actions.Count,
            extracted.Decisions.Count,
            extracted.Risks.Count,
            extracted.Knowledge.Count,
            $"Meeting '{req.Title}' verarbeitet: {extracted.Actions.Count} Tasks, {extracted.Decisions.Count} Entscheidungen, {extracted.Risks.Count} Risiken, {extracted.Knowledge.Count} Knowledge-Items."
        ));
    }

    private static (string Summary, List<MeetingExtractedItemDto> Actions, List<MeetingExtractedItemDto> Decisions, List<MeetingExtractedItemDto> Risks, List<MeetingExtractedItemDto> Knowledge) ExtractMeeting(string title, string content)
    {
        var actions = new List<MeetingExtractedItemDto>();
        var decisions = new List<MeetingExtractedItemDto>();
        var risks = new List<MeetingExtractedItemDto>();
        var knowledge = new List<MeetingExtractedItemDto>();

        var lines = content
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        foreach (var line in lines)
        {
            var normalized = line.Trim().TrimStart('-', '*', '•').Trim();

            if (TryExtract(normalized, "todo:", out var todoValue) || TryExtract(normalized, "action:", out todoValue) || normalized.Contains("naechster schritt", StringComparison.OrdinalIgnoreCase))
            {
                actions.Add(new MeetingExtractedItemDto("task", ShortTitle(todoValue, "Follow-up aus Meeting"), todoValue));
                continue;
            }

            if (TryExtract(normalized, "decision:", out var decisionValue) || TryExtract(normalized, "beschluss:", out decisionValue))
            {
                decisions.Add(new MeetingExtractedItemDto("decision", ShortTitle(decisionValue, "Entscheidung aus Meeting"), decisionValue));
                continue;
            }

            if (TryExtract(normalized, "risk:", out var riskValue) || normalized.Contains("risiko", StringComparison.OrdinalIgnoreCase) || normalized.Contains("blocker", StringComparison.OrdinalIgnoreCase))
            {
                risks.Add(new MeetingExtractedItemDto("risk", ShortTitle(riskValue, "Risiko aus Meeting"), riskValue));
                continue;
            }

            if (TryExtract(normalized, "info:", out var infoValue) || TryExtract(normalized, "note:", out infoValue) || normalized.Contains("erkenntnis", StringComparison.OrdinalIgnoreCase))
            {
                knowledge.Add(new MeetingExtractedItemDto("knowledge", ShortTitle(infoValue, "Erkenntnis aus Meeting"), infoValue));
            }
        }

        if (knowledge.Count == 0 && lines.Count > 0)
        {
            knowledge.Add(new MeetingExtractedItemDto("knowledge", $"Zusammenfassung {title}", string.Join(" ", lines.Take(3))));
        }

        var summary = $"Meeting '{title}' analysiert: {actions.Count} Tasks, {decisions.Count} Entscheidungen, {risks.Count} Risiken, {knowledge.Count} Knowledge-Items erkannt.";
        return (summary, actions, decisions, risks, knowledge);
    }

    private static bool TryExtract(string line, string prefix, out string value)
    {
        if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = line[prefix.Length..].Trim();
            return true;
        }

        value = line;
        return false;
    }

    private static string ShortTitle(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var clean = value.Trim();
        return clean.Length <= 80 ? clean : clean[..80].TrimEnd() + "...";
    }
}
