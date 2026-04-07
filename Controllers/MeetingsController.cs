using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/projects/{projectId}/meetings"), Authorize]
public class MeetingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public MeetingsController(AppDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    // GET /api/v1/projects/{projectId}/meetings
    [HttpGet]
    public async Task<ActionResult<List<ProjectMeetingDto>>> GetAll(Guid projectId)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var meetings = await _db.ProjectMeetings
            .Include(m => m.CreatedBy)
            .Where(m => m.ProjectId == projectId)
            .OrderByDescending(m => m.MeetingDate)
            .ToListAsync();

        return Ok(meetings.Select(MapMeeting).ToList());
    }

    // POST /api/v1/projects/{projectId}/meetings
    [HttpPost]
    public async Task<ActionResult<ProjectMeetingDto>> Create(Guid projectId, [FromBody] CreateProjectMeetingRequest req)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var meeting = new ProjectMeeting
        {
            ProjectId = projectId,
            CreatedByUserId = UserId,
            Title = req.Title,
            MeetingDate = req.MeetingDate,
            Participants = req.Participants ?? "",
            Location = req.Location ?? "",
            TeamsJoinUrl = req.TeamsJoinUrl ?? "",
            TeamsOnlineMeetingId = req.TeamsOnlineMeetingId ?? "",
            Notes = req.Notes ?? ""
        };

        _db.ProjectMeetings.Add(meeting);
        await _db.SaveChangesAsync();

        await _db.Entry(meeting).Reference(m => m.CreatedBy).LoadAsync();
        return CreatedAtAction(nameof(GetAll), new { projectId }, MapMeeting(meeting));
    }

    // PUT /api/v1/projects/{projectId}/meetings/{meetingId}
    [HttpPut("{meetingId}")]
    public async Task<ActionResult<ProjectMeetingDto>> Update(Guid projectId, Guid meetingId, [FromBody] UpdateProjectMeetingRequest req)
    {
        var meeting = await _db.ProjectMeetings
            .Include(m => m.CreatedBy)
            .FirstOrDefaultAsync(m => m.Id == meetingId && m.ProjectId == projectId);

        if (meeting == null) return NotFound();

        // Verify project belongs to tenant
        var projectBelongsToTenant = await _db.Projects.AnyAsync(p => p.Id == projectId && p.TenantId == TenantId);
        if (!projectBelongsToTenant) return NotFound();

        if (req.Title != null) meeting.Title = req.Title;
        if (req.MeetingDate.HasValue) meeting.MeetingDate = req.MeetingDate.Value;
        if (req.Participants != null) meeting.Participants = req.Participants;
        if (req.Location != null) meeting.Location = req.Location;
        if (req.TeamsJoinUrl != null) meeting.TeamsJoinUrl = req.TeamsJoinUrl;
        if (req.TeamsOnlineMeetingId != null) meeting.TeamsOnlineMeetingId = req.TeamsOnlineMeetingId;
        if (req.Notes != null) meeting.Notes = req.Notes;
        meeting.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapMeeting(meeting));
    }

    // DELETE /api/v1/projects/{projectId}/meetings/{meetingId}
    [HttpDelete("{meetingId}")]
    public async Task<ActionResult> Delete(Guid projectId, Guid meetingId)
    {
        var meeting = await _db.ProjectMeetings.FirstOrDefaultAsync(m => m.Id == meetingId && m.ProjectId == projectId);
        if (meeting == null) return NotFound();

        var projectBelongsToTenant = await _db.Projects.AnyAsync(p => p.Id == projectId && p.TenantId == TenantId);
        if (!projectBelongsToTenant) return NotFound();

        _db.ProjectMeetings.Remove(meeting);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/v1/projects/{projectId}/meetings/{meetingId}/transcript
    [HttpPost("{meetingId}/transcript")]
    public async Task<ActionResult<ProjectMeetingDto>> AddTranscript(Guid projectId, Guid meetingId, [FromBody] AddTranscriptRequest req)
    {
        var meeting = await _db.ProjectMeetings
            .Include(m => m.CreatedBy)
            .FirstOrDefaultAsync(m => m.Id == meetingId && m.ProjectId == projectId);
        if (meeting == null) return NotFound();

        var projectBelongsToTenant = await _db.Projects.AnyAsync(p => p.Id == projectId && p.TenantId == TenantId);
        if (!projectBelongsToTenant) return NotFound();

        meeting.TranscriptRaw = req.TranscriptText;
        meeting.TranscriptSource = "manual";
        meeting.TranscriptFetchedAt = DateTime.UtcNow;
        meeting.ExtractionStatus = "none";
        meeting.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapMeeting(meeting));
    }

    // POST /api/v1/projects/{projectId}/meetings/{meetingId}/transcript/fetch
    [HttpPost("{meetingId}/transcript/fetch")]
    public async Task<ActionResult<ProjectMeetingDto>> FetchTranscript(Guid projectId, Guid meetingId)
    {
        var meeting = await _db.ProjectMeetings
            .Include(m => m.CreatedBy)
            .FirstOrDefaultAsync(m => m.Id == meetingId && m.ProjectId == projectId);
        if (meeting == null) return NotFound();

        var projectBelongsToTenant = await _db.Projects.AnyAsync(p => p.Id == projectId && p.TenantId == TenantId);
        if (!projectBelongsToTenant) return NotFound();

        if (string.IsNullOrWhiteSpace(meeting.TeamsOnlineMeetingId))
        {
            return BadRequest("Kein Teams Meeting-ID hinterlegt. Bitte TeamsOnlineMeetingId im Termin eintragen.");
        }

        // Load Graph token for this tenant
        var tokenRecord = await _db.GraphTokens
            .Where(t => t.TenantId == TenantId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (tokenRecord == null || tokenRecord.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest("Kein gueltiger Microsoft Graph Token vorhanden. Bitte zuerst unter Einstellungen → Integrationen die Graph-Verbindung einrichten.");
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenRecord.AccessToken);

        // Try to get transcripts list for this meeting
        var transcriptListUrl = $"https://graph.microsoft.com/v1.0/me/onlineMeetings/{meeting.TeamsOnlineMeetingId}/transcripts";
        using var listResponse = await client.GetAsync(transcriptListUrl);

        if (listResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return BadRequest("Zugriff verweigert. Stellen Sie sicher, dass der Scope 'OnlineMeetingTranscript.Read.All' gewährt wurde und die Aufzeichnung in Teams aktiviert ist.");
        }

        if (listResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return BadRequest("Meeting nicht gefunden oder kein Transkript verfügbar. Prüfen Sie ob die Aufzeichnung/Transkription in Teams aktiviert war.");
        }

        if (!listResponse.IsSuccessStatusCode)
        {
            var errorBody = await listResponse.Content.ReadAsStringAsync();
            return BadRequest($"Fehler beim Abrufen des Transkripts: {errorBody}");
        }

        var listJson = await listResponse.Content.ReadAsStringAsync();
        var listDoc = JsonDocument.Parse(listJson);

        var transcripts = listDoc.RootElement.GetProperty("value").EnumerateArray().ToList();
        if (transcripts.Count == 0)
        {
            return BadRequest("Kein Transkript für dieses Meeting vorhanden. Prüfen Sie ob die Transkription aktiviert war.");
        }

        var firstTranscriptId = transcripts[0].GetProperty("id").GetString();
        var contentUrl = $"https://graph.microsoft.com/v1.0/me/onlineMeetings/{meeting.TeamsOnlineMeetingId}/transcripts/{firstTranscriptId}/content?$format=text/vtt";

        using var contentResponse = await client.GetAsync(contentUrl);
        if (!contentResponse.IsSuccessStatusCode)
        {
            return BadRequest("Transkript-Inhalt konnte nicht geladen werden.");
        }

        var transcriptContent = await contentResponse.Content.ReadAsStringAsync();

        meeting.TranscriptRaw = transcriptContent;
        meeting.TranscriptSource = "graph";
        meeting.TranscriptFetchedAt = DateTime.UtcNow;
        meeting.ExtractionStatus = "none";
        meeting.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapMeeting(meeting));
    }

    // POST /api/v1/projects/{projectId}/meetings/{meetingId}/extract
    [HttpPost("{meetingId}/extract")]
    public async Task<ActionResult<MeetingCommitResponse>> Extract(Guid projectId, Guid meetingId)
    {
        var meeting = await _db.ProjectMeetings
            .Include(m => m.CreatedBy)
            .FirstOrDefaultAsync(m => m.Id == meetingId && m.ProjectId == projectId);
        if (meeting == null) return NotFound();

        var projectBelongsToTenant = await _db.Projects.AnyAsync(p => p.Id == projectId && p.TenantId == TenantId);
        if (!projectBelongsToTenant) return NotFound();

        if (string.IsNullOrWhiteSpace(meeting.TranscriptRaw))
        {
            return BadRequest("Kein Transkript vorhanden. Bitte zuerst ein Transkript hinzufügen.");
        }

        meeting.ExtractionStatus = "pending";
        await _db.SaveChangesAsync();

        var extracted = ExtractMeeting(meeting.Title, meeting.TranscriptRaw);

        var note = new ProjectNote
        {
            ProjectId = projectId,
            AuthorId = UserId,
            Title = $"Meeting: {meeting.Title}",
            Content = extracted.Summary,
            Category = "meeting",
            Participants = meeting.Participants,
            MeetingDate = DateOnly.FromDateTime(meeting.MeetingDate)
        };
        _db.ProjectNotes.Add(note);

        foreach (var action in extracted.Actions)
        {
            _db.Tasks.Add(new ProjectTask
            {
                ProjectId = projectId,
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
                ProjectId = projectId,
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
                ProjectId = projectId,
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
                ProjectId = projectId,
                AuthorId = UserId,
                Title = item.Title,
                SourceType = "meeting",
                SourceLabel = meeting.Title,
                Content = item.Detail,
                TagsCsv = "teams|meeting|knowledge",
                Importance = 4
            });
        }

        meeting.ExtractionStatus = "extracted";
        meeting.UpdatedAt = DateTime.UtcNow;

        _db.ActivityLogs.Add(new ActivityLog
        {
            TenantId = TenantId,
            UserId = UserId,
            ProjectId = projectId,
            EntityId = meeting.Id,
            EntityType = "Meeting",
            Action = $"hat Meeting-Transkript analysiert: {meeting.Title}"
        });

        await _db.SaveChangesAsync();

        return Ok(new MeetingCommitResponse(
            projectId,
            meeting.Title,
            extracted.Actions.Count,
            extracted.Decisions.Count,
            extracted.Risks.Count,
            extracted.Knowledge.Count,
            $"Meeting '{meeting.Title}' verarbeitet: {extracted.Actions.Count} Tasks, {extracted.Decisions.Count} Entscheidungen, {extracted.Risks.Count} Risiken, {extracted.Knowledge.Count} Knowledge-Items."
        ));
    }

    private static ProjectMeetingDto MapMeeting(ProjectMeeting m) => new(
        m.Id,
        m.Title,
        m.MeetingDate,
        m.Participants,
        m.Location,
        m.TeamsJoinUrl,
        m.TeamsOnlineMeetingId,
        m.TranscriptSource,
        m.TranscriptFetchedAt,
        m.ExtractionStatus,
        m.Notes,
        m.CreatedBy?.DisplayName ?? "",
        m.CreatedAt,
        !string.IsNullOrWhiteSpace(m.TranscriptRaw)
    );

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
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var clean = value.Trim();
        return clean.Length <= 80 ? clean : clean[..80].TrimEnd() + "...";
    }
}
