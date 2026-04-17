using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Hubs;
using PmTool.Api.Models;
using PmTool.Api.Services;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/projects/{projectId}/meetings"), Authorize]
public class MeetingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AnthropicService _ai;
    private readonly EmbeddingService _embed;
    private readonly IHubContext<PmHub> _hub;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public MeetingsController(AppDbContext db, IHttpClientFactory httpClientFactory, AnthropicService ai, EmbeddingService embed, IHubContext<PmHub> hub)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _ai = ai;
        _embed = embed;
        _hub = hub;
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

        var project = await _db.Projects
            .Include(p => p.TeamAssignments).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == TenantId);
        if (project == null) return NotFound();

        if (string.IsNullOrWhiteSpace(meeting.TranscriptRaw))
            return BadRequest("Kein Transkript vorhanden. Bitte zuerst ein Transkript hinzufügen.");

        meeting.ExtractionStatus = "pending";
        await _db.SaveChangesAsync();

        MeetingExtractionResult extracted;

        if (_ai.IsConfigured)
        {
            var teamNames = project.TeamAssignments.Select(r => r.User?.DisplayName ?? "").Where(n => n.Length > 0).ToList();
            var transcriptPreview = meeting.TranscriptRaw.Length > 12000 ? meeting.TranscriptRaw[..12000] : meeting.TranscriptRaw;

            var systemPrompt = $$"""
                You are an expert meeting analyst for the project management tool RealCore PM.
                Analyze the meeting transcript and extract structured information.
                Project: {{project.Name}}
                Known team members: {{string.Join(", ", teamNames)}}
                Meeting: {{meeting.Title}} on {{meeting.MeetingDate:yyyy-MM-dd}}

                Return ONLY a JSON object with this exact structure:
                {
                  "tasks": [
                    {"title": "...", "description": "...", "assigneeName": "...", "priority": "high|medium|low", "dueDaysFromNow": 7}
                  ],
                  "decisions": [
                    {"title": "...", "context": "...", "ownerName": "...", "dueDaysFromNow": 5}
                  ],
                  "risks": [
                    {"title": "...", "description": "...", "ownerName": "...", "impact": 3, "probability": 3}
                  ],
                  "knowledge": [
                    {"title": "...", "content": "...", "category": "meeting", "importance": 4}
                  ],
                  "summary": "Brief 2-3 sentence summary of the meeting",
                  "sentiment": "positive|neutral|concerning",
                  "confidence": 0.85
                }
                Extract only what is clearly present. Do not invent items.
                Use the same language as the transcript (German or English).
                """;

            extracted = await _ai.ExtractStructuredAsync<MeetingExtractionResult>(systemPrompt, transcriptPreview, 3000)
                ?? FallbackExtract(meeting.Title, meeting.TranscriptRaw);
        }
        else
        {
            extracted = FallbackExtract(meeting.Title, meeting.TranscriptRaw);
        }

        // Create entities
        var note = new ProjectNote
        {
            ProjectId = projectId, AuthorId = UserId,
            Title = $"Meeting: {meeting.Title}",
            Content = extracted.Summary,
            Category = "meeting", Participants = meeting.Participants,
            MeetingDate = DateOnly.FromDateTime(meeting.MeetingDate)
        };
        _db.ProjectNotes.Add(note);

        var createdTaskIds = new List<Guid>();
        foreach (var t in extracted.Tasks)
        {
            var assigneeId = ResolveAssignee(project, t.AssigneeName) ?? UserId;
            var task = new ProjectTask
            {
                ProjectId = projectId, Title = t.Title, Description = t.Description,
                Status = "todo", Priority = t.Priority, AssigneeId = assigneeId,
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(t.DueDaysFromNow)),
                EstimatedHours = 4
            };
            _db.Tasks.Add(task);
            createdTaskIds.Add(task.Id);
        }

        foreach (var d in extracted.Decisions)
        {
            var ownerId = ResolveAssignee(project, d.OwnerName) ?? UserId;
            _db.ProjectDecisions.Add(new ProjectDecision
            {
                ProjectId = projectId, OwnerId = ownerId, Title = d.Title, Context = d.Context,
                Decision = "Aus Meeting-KI erkannt, noch zu finalisieren.",
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(d.DueDaysFromNow)), Status = "open"
            });
        }

        foreach (var r in extracted.Risks)
        {
            var ownerId = ResolveAssignee(project, r.OwnerName) ?? UserId;
            _db.Risks.Add(new Risk
            {
                ProjectId = projectId, OwnerId = ownerId, Title = r.Title, Description = r.Description,
                Impact = r.Impact, Probability = r.Probability, Status = "open",
                Mitigation = "Im Folgemeeting bewerten und Massnahme festlegen."
            });
        }

        var knowledgeIds = new List<Guid>();
        foreach (var k in extracted.Knowledge)
        {
            var ki = new ProjectKnowledgeItem
            {
                ProjectId = projectId, AuthorId = UserId, Title = k.Title,
                SourceType = "meeting", SourceLabel = meeting.Title,
                Content = k.Content, Category = k.Category,
                TagsCsv = "teams|meeting|ki-extraktion", Importance = k.Importance
            };
            _db.ProjectKnowledgeItems.Add(ki);
            knowledgeIds.Add(ki.Id);
        }

        meeting.ExtractionStatus = "extracted";
        meeting.UpdatedAt = DateTime.UtcNow;

        _db.ActivityLogs.Add(new ActivityLog
        {
            TenantId = TenantId, UserId = UserId, ProjectId = projectId,
            EntityId = meeting.Id, EntityType = "Meeting",
            Action = $"hat Meeting-Transkript analysiert (KI): {meeting.Title} — {extracted.Tasks.Count} Tasks, {extracted.Decisions.Count} Entscheidungen"
        });

        await _db.SaveChangesAsync();

        // Embed knowledge items and meeting summary asynchronously
        _ = Task.Run(async () =>
        {
            foreach (var id in knowledgeIds)
            {
                var ki = await _db.ProjectKnowledgeItems.FindAsync(id);
                if (ki != null)
                    await _embed.StoreEmbeddingAsync(TenantId, projectId, "knowledge", id, $"{ki.Title}\n{ki.Content}", ki.Importance);
            }
            await _embed.StoreEmbeddingAsync(TenantId, projectId, "meeting", meeting.Id, $"{meeting.Title}\n{extracted.Summary}");
        });

        // SignalR notification
        await _hub.Clients.Group(TenantId.ToString()).SendAsync("MeetingExtracted", new
        {
            projectId,
            meetingId = meeting.Id,
            meetingTitle = meeting.Title,
            tasksCreated = extracted.Tasks.Count,
            decisionsCreated = extracted.Decisions.Count,
            risksCreated = extracted.Risks.Count,
            sentiment = extracted.Sentiment,
            confidence = extracted.Confidence
        });

        return Ok(new MeetingCommitResponse(
            projectId, meeting.Title,
            extracted.Tasks.Count, extracted.Decisions.Count, extracted.Risks.Count, extracted.Knowledge.Count,
            $"Meeting '{meeting.Title}' verarbeitet: {extracted.Tasks.Count} Tasks, {extracted.Decisions.Count} Entscheidungen, {extracted.Risks.Count} Risiken, {extracted.Knowledge.Count} Knowledge-Items. Sentiment: {extracted.Sentiment}."
        ));
    }

    private static Guid? ResolveAssignee(Project project, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return project.TeamAssignments
            .FirstOrDefault(r => r.User?.DisplayName?.Contains(name, StringComparison.OrdinalIgnoreCase) == true)
            ?.UserId;
    }

    private static MeetingExtractionResult FallbackExtract(string title, string content)
    {
        var actions = new List<ExtractedTask>();
        var decisions = new List<ExtractedDecision>();
        var risks = new List<ExtractedRisk>();
        var knowledge = new List<ExtractedKnowledge>();

        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

        foreach (var line in lines)
        {
            var normalized = line.Trim().TrimStart('-', '*', '•').Trim();
            if (TryExtract(normalized, "todo:", out var todoVal) || TryExtract(normalized, "action:", out todoVal))
                actions.Add(new ExtractedTask { Title = ShortTitle(todoVal), Description = todoVal });
            else if (TryExtract(normalized, "decision:", out var decVal) || TryExtract(normalized, "beschluss:", out decVal))
                decisions.Add(new ExtractedDecision { Title = ShortTitle(decVal), Context = decVal });
            else if (TryExtract(normalized, "risk:", out var riskVal) || normalized.Contains("risiko", StringComparison.OrdinalIgnoreCase))
                risks.Add(new ExtractedRisk { Title = ShortTitle(riskVal), Description = riskVal });
        }

        if (knowledge.Count == 0 && lines.Count > 0)
            knowledge.Add(new ExtractedKnowledge { Title = $"Zusammenfassung {title}", Content = string.Join(" ", lines.Take(3)) });

        return new MeetingExtractionResult
        {
            Tasks = actions, Decisions = decisions, Risks = risks, Knowledge = knowledge,
            Summary = $"Meeting '{title}': {actions.Count} Tasks, {decisions.Count} Entscheidungen, {risks.Count} Risiken erkannt.",
            Sentiment = "neutral", Confidence = 0.6f
        };
    }

    private static bool TryExtract(string line, string prefix, out string value)
    {
        if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        { value = line[prefix.Length..].Trim(); return true; }
        value = line; return false;
    }

    private static string ShortTitle(string v) => string.IsNullOrWhiteSpace(v) ? "Item" : v.Length <= 80 ? v.Trim() : v.Trim()[..80] + "...";

    private static ProjectMeetingDto MapMeeting(ProjectMeeting m) => new(
        m.Id, m.Title, m.MeetingDate, m.Participants, m.Location,
        m.TeamsJoinUrl, m.TeamsOnlineMeetingId, m.TranscriptSource,
        m.TranscriptFetchedAt, m.ExtractionStatus, m.Notes,
        m.CreatedBy?.DisplayName ?? "", m.CreatedAt,
        !string.IsNullOrWhiteSpace(m.TranscriptRaw)
    );
}
