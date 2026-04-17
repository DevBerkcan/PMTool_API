using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Hubs;
using PmTool.Api.Models;
using PmTool.Api.Services;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PmTool.Api.Controllers;

/// <summary>
/// Handles Microsoft Graph change notification webhooks for automatic meeting transcript processing.
///
/// Setup:
/// 1. Register subscription: POST /api/v1/webhooks/graph/subscribe
/// 2. Graph sends validation token to this endpoint first (GET with validationToken param)
/// 3. Graph sends change notifications here when new transcripts are available
/// 4. We fetch the transcript, match to project, extract with LLM, create entities
/// </summary>
[ApiController, Route("api/v1/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AnthropicService _ai;
    private readonly EmbeddingService _embed;
    private readonly ProjectMatcherService _matcher;
    private readonly IHubContext<PmHub> _hub;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<WebhooksController> _log;

    public WebhooksController(AppDbContext db, AnthropicService ai, EmbeddingService embed,
        ProjectMatcherService matcher, IHubContext<PmHub> hub, IHttpClientFactory httpFactory,
        IConfiguration config, ILogger<WebhooksController> log)
    {
        _db = db; _ai = ai; _embed = embed; _matcher = matcher;
        _hub = hub; _httpFactory = httpFactory; _config = config; _log = log;
    }

    // ─── Graph validation handshake ───────────────────────────────────────────

    [HttpPost("graph/transcript")]
    public async Task<IActionResult> GraphTranscriptWebhook(
        [FromQuery] string? validationToken,
        [FromBody] GraphNotificationPayload? payload)
    {
        // Validation handshake (Graph sends this when registering subscription)
        if (!string.IsNullOrWhiteSpace(validationToken))
        {
            _log.LogInformation("Graph webhook validation token received");
            return Content(validationToken, "text/plain");
        }

        if (payload?.Value == null) return BadRequest();

        foreach (var notification in payload.Value)
        {
            _ = Task.Run(() => ProcessTranscriptNotificationAsync(notification));
        }

        return Accepted();
    }

    // ─── Register Graph webhook subscription ──────────────────────────────────

    [HttpPost("graph/subscribe")]
    public async Task<ActionResult<GraphSubscriptionResult>> RegisterSubscription([FromBody] RegisterWebhookRequest req)
    {
        var tokenRecord = await _db.GraphTokens
            .Where(t => t.TenantId == req.TenantId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (tokenRecord == null || tokenRecord.ExpiresAt < DateTime.UtcNow)
            return BadRequest("Kein gültiger Graph-Token vorhanden. Bitte zuerst Graph-Verbindung einrichten.");

        var notificationUrl = _config["GraphWebhook:NotificationUrl"];
        if (string.IsNullOrWhiteSpace(notificationUrl))
            return BadRequest("GraphWebhook:NotificationUrl nicht konfiguriert.");

        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenRecord.AccessToken);

        var expirationDays = int.TryParse(_config["GraphWebhook:SubscriptionLifetimeDays"], out var days) ? days : 2;
        var expirationDateTime = DateTime.UtcNow.AddDays(expirationDays).ToString("o");

        var subscriptionPayload = new
        {
            changeType = "created",
            notificationUrl,
            resource = "communications/onlineMeetings/getAllTranscripts",
            expirationDateTime,
            clientState = $"pmtool-{req.TenantId:N}"
        };

        var json = JsonSerializer.Serialize(subscriptionPayload);
        var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://graph.microsoft.com/v1.0/subscriptions", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _log.LogError("Graph subscription failed: {Error}", error);
            return BadRequest($"Graph-Subscription fehlgeschlagen: {error}");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(responseBody);
        var subscriptionId = doc.RootElement.GetProperty("id").GetString() ?? "";
        var expiry = doc.RootElement.GetProperty("expirationDateTime").GetString() ?? "";

        // Store subscription record
        var existing = await _db.GraphWebhookSubscriptions.FirstOrDefaultAsync(s => s.TenantId == req.TenantId && s.Status == "active");
        if (existing != null)
        {
            existing.SubscriptionId = subscriptionId;
            existing.ExpirationDateTime = DateTime.Parse(expiry);
            existing.LastRenewedAt = DateTime.UtcNow;
            existing.Status = "active";
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.GraphWebhookSubscriptions.Add(new GraphWebhookSubscription
            {
                TenantId = req.TenantId,
                SubscriptionId = subscriptionId,
                Resource = "communications/onlineMeetings/getAllTranscripts",
                ChangeType = "created",
                ExpirationDateTime = DateTime.Parse(expiry),
                Status = "active",
                LastRenewedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        _log.LogInformation("Graph webhook subscription registered: {SubscriptionId}", subscriptionId);

        return Ok(new GraphSubscriptionResult(subscriptionId, expiry, "active"));
    }

    // ─── Manual transcript processing trigger ────────────────────────────────

    [HttpPost("graph/process-meeting")]
    public async Task<ActionResult<WebhookProcessResult>> ProcessMeetingManually([FromBody] ManualMeetingProcessRequest req)
    {
        var tokenRecord = await _db.GraphTokens
            .Where(t => t.TenantId == req.TenantId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (tokenRecord == null) return BadRequest("Kein Graph-Token vorhanden.");

        var result = await FetchAndProcessTranscriptAsync(req.TenantId, req.TeamsOnlineMeetingId, tokenRecord.AccessToken);
        return Ok(result);
    }

    // ─── Internal processing ──────────────────────────────────────────────────

    private async Task ProcessTranscriptNotificationAsync(GraphNotificationItem notification)
    {
        try
        {
            // Parse tenant ID from clientState: "pmtool-{tenantId}"
            var clientState = notification.ClientState ?? "";
            if (!clientState.StartsWith("pmtool-")) return;
            var tenantIdStr = clientState["pmtool-".Length..];
            if (!Guid.TryParse(tenantIdStr, out var tenantId)) return;

            var tokenRecord = await _db.GraphTokens
                .Where(t => t.TenantId == tenantId)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (tokenRecord == null || tokenRecord.ExpiresAt < DateTime.UtcNow)
            {
                _log.LogWarning("No valid Graph token for tenant {TenantId}", tenantId);
                return;
            }

            // Extract meeting ID from resource path
            var resource = notification.Resource ?? "";
            var meetingId = ExtractMeetingIdFromResource(resource);
            if (string.IsNullOrWhiteSpace(meetingId)) return;

            var result = await FetchAndProcessTranscriptAsync(tenantId, meetingId, tokenRecord.AccessToken);

            await _hub.Clients.Group(tenantId.ToString()).SendAsync(HubEvents.WebhookProcessed, result);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to process transcript notification");
        }
    }

    private async Task<WebhookProcessResult> FetchAndProcessTranscriptAsync(Guid tenantId, string meetingId, string accessToken)
    {
        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Fetch transcript list
        var transcriptListUrl = $"https://graph.microsoft.com/v1.0/me/onlineMeetings/{meetingId}/transcripts";
        var listResponse = await client.GetAsync(transcriptListUrl);
        if (!listResponse.IsSuccessStatusCode)
            return new WebhookProcessResult(false, null, 0, 0, 0, $"Failed to fetch transcripts: {listResponse.StatusCode}");

        var listJson = await listResponse.Content.ReadAsStringAsync();
        var listDoc = JsonDocument.Parse(listJson);
        var transcripts = listDoc.RootElement.GetProperty("value").EnumerateArray().ToList();
        if (transcripts.Count == 0)
            return new WebhookProcessResult(false, null, 0, 0, 0, "No transcripts available");

        var transcriptId = transcripts[0].GetProperty("id").GetString();
        var contentUrl = $"https://graph.microsoft.com/v1.0/me/onlineMeetings/{meetingId}/transcripts/{transcriptId}/content?$format=text/vtt";
        var contentResponse = await client.GetAsync(contentUrl);
        if (!contentResponse.IsSuccessStatusCode)
            return new WebhookProcessResult(false, null, 0, 0, 0, "Failed to fetch transcript content");

        var transcriptText = await contentResponse.Content.ReadAsStringAsync();

        // Fetch meeting metadata
        var meetingUrl = $"https://graph.microsoft.com/v1.0/me/onlineMeetings/{meetingId}";
        var meetingResponse = await client.GetAsync(meetingUrl);
        string meetingTitle = $"Teams Meeting {DateTime.UtcNow:yyyy-MM-dd}";
        string? teamsChannelId = null;
        List<string> participantEmails = new();

        if (meetingResponse.IsSuccessStatusCode)
        {
            var meetingDoc = JsonDocument.Parse(await meetingResponse.Content.ReadAsStringAsync());
            meetingTitle = meetingDoc.RootElement.TryGetProperty("subject", out var sub) ? sub.GetString() ?? meetingTitle : meetingTitle;
            if (meetingDoc.RootElement.TryGetProperty("chatInfo", out var chatInfo) && chatInfo.TryGetProperty("threadId", out var threadId))
                teamsChannelId = threadId.GetString();

            if (meetingDoc.RootElement.TryGetProperty("participants", out var participants) && participants.TryGetProperty("attendees", out var attendees))
            {
                foreach (var attendee in attendees.EnumerateArray())
                {
                    if (attendee.TryGetProperty("identity", out var identity) && identity.TryGetProperty("user", out var user) && user.TryGetProperty("mail", out var mail))
                        participantEmails.Add(mail.GetString() ?? "");
                }
            }
        }

        // Match project
        var matchResult = await _matcher.MatchAsync(tenantId, teamsChannelId, transcriptText, participantEmails);
        if (matchResult.ProjectId == null)
        {
            _log.LogInformation("Could not match meeting '{Title}' to a project", meetingTitle);
            return new WebhookProcessResult(false, null, 0, 0, 0, "Could not match meeting to a project");
        }

        var projectId = matchResult.ProjectId.Value;
        var project = await _db.Projects.Include(p => p.TeamAssignments).ThenInclude(r => r.User).FirstOrDefaultAsync(p => p.Id == projectId);
        if (project == null) return new WebhookProcessResult(false, null, 0, 0, 0, "Project not found");

        // Create meeting record
        var systemUser = await _db.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId);
        if (systemUser == null) return new WebhookProcessResult(false, null, 0, 0, 0, "No user found");

        var meeting = new ProjectMeeting
        {
            ProjectId = projectId, CreatedByUserId = systemUser.Id,
            Title = meetingTitle, MeetingDate = DateTime.UtcNow,
            TeamsOnlineMeetingId = meetingId, Participants = string.Join(", ", participantEmails),
            TranscriptRaw = transcriptText, TranscriptSource = "graph",
            TranscriptFetchedAt = DateTime.UtcNow, ExtractionStatus = "pending"
        };
        _db.ProjectMeetings.Add(meeting);
        await _db.SaveChangesAsync();

        // Extract with LLM if confidence threshold met (auto-create) or notify for review
        MeetingExtractionResult extracted;

        if (_ai.IsConfigured && matchResult.Confidence >= 0.75)
        {
            var teamNames = project.TeamAssignments.Select(r => r.User?.DisplayName ?? "").Where(n => n.Length > 0).ToList();
            var preview = transcriptText.Length > 12000 ? transcriptText[..12000] : transcriptText;

            extracted = await _ai.ExtractStructuredAsync<MeetingExtractionResult>(
                $"""
                Extract structured items from this meeting transcript. Project: {project.Name}. Team: {string.Join(", ", teamNames)}.
                Return JSON with tasks, decisions, risks, knowledge, summary, sentiment (positive|neutral|concerning), confidence (0-1).
                """,
                preview, 3000) ?? new MeetingExtractionResult { Summary = $"Meeting {meetingTitle} automatically processed.", Sentiment = "neutral", Confidence = 0.6f };
        }
        else
        {
            extracted = new MeetingExtractionResult { Summary = $"Meeting {meetingTitle} erfasst. Manuelle Extraktion empfohlen.", Sentiment = "neutral", Confidence = 0.5f };
        }

        // Create entities if auto-create threshold met
        var tasksCreated = 0; var decisionsCreated = 0; var risksCreated = 0;

        if (matchResult.Confidence >= 0.75 && _ai.IsConfigured)
        {
            foreach (var t in extracted.Tasks)
            {
                var assigneeId = project.TeamAssignments.FirstOrDefault(r => r.User?.DisplayName?.Contains(t.AssigneeName, StringComparison.OrdinalIgnoreCase) == true)?.UserId ?? systemUser.Id;
                _db.Tasks.Add(new ProjectTask { ProjectId = projectId, Title = t.Title, Description = t.Description, Status = "todo", Priority = t.Priority, AssigneeId = assigneeId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(t.DueDaysFromNow)), EstimatedHours = 4 });
                tasksCreated++;
            }
            foreach (var d in extracted.Decisions)
            {
                _db.ProjectDecisions.Add(new ProjectDecision { ProjectId = projectId, OwnerId = systemUser.Id, Title = d.Title, Context = d.Context, Decision = "Aus automatisiertem Meeting-Import erkannt.", DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(d.DueDaysFromNow)), Status = "open" });
                decisionsCreated++;
            }
            foreach (var r in extracted.Risks)
            {
                _db.Risks.Add(new Risk { ProjectId = projectId, OwnerId = systemUser.Id, Title = r.Title, Description = r.Description, Impact = r.Impact, Probability = r.Probability, Status = "open", Mitigation = "Automatisch erfasst — Massnahme abstimmen." });
                risksCreated++;
            }
        }

        _db.ProjectNotes.Add(new ProjectNote { ProjectId = projectId, AuthorId = systemUser.Id, Title = $"Auto-Meeting: {meetingTitle}", Content = extracted.Summary, Category = "meeting", MeetingDate = DateOnly.FromDateTime(DateTime.UtcNow) });

        meeting.ExtractionStatus = "extracted";
        meeting.UpdatedAt = DateTime.UtcNow;

        _db.ActivityLogs.Add(new ActivityLog { TenantId = tenantId, UserId = systemUser.Id, ProjectId = projectId, EntityId = meeting.Id, EntityType = "Meeting", Action = $"Automatisches Meeting-Import: {meetingTitle} ({tasksCreated} Tasks, {decisionsCreated} Entscheidungen)" });
        await _db.SaveChangesAsync();

        // SignalR push to all tenant users
        await _hub.Clients.Group(tenantId.ToString()).SendAsync(HubEvents.MeetingExtracted, new
        {
            projectId, meetingId = meeting.Id, meetingTitle, project.Name,
            tasksCreated, decisionsCreated, risksCreated,
            sentiment = extracted.Sentiment, confidence = matchResult.Confidence,
            requiresReview = matchResult.RequiresUserConfirmation
        });

        return new WebhookProcessResult(true, project.Name, tasksCreated, decisionsCreated, risksCreated, null);
    }

    private static string? ExtractMeetingIdFromResource(string resource)
    {
        // Resource format: "communications/onlineMeetings/{meetingId}/transcripts/{transcriptId}"
        var parts = resource.Split('/');
        var idx = Array.IndexOf(parts, "onlineMeetings");
        return idx >= 0 && idx + 1 < parts.Length ? parts[idx + 1] : null;
    }
}

// ─── Request/response types ───────────────────────────────────────────────────

public class GraphNotificationPayload
{
    public List<GraphNotificationItem> Value { get; set; } = [];
}

public class GraphNotificationItem
{
    public string? SubscriptionId { get; set; }
    public string? ChangeType { get; set; }
    public string? Resource { get; set; }
    public string? ClientState { get; set; }
    public string? TenantId { get; set; }
}

public record RegisterWebhookRequest(Guid TenantId);
public record GraphSubscriptionResult(string SubscriptionId, string ExpirationDateTime, string Status);
public record ManualMeetingProcessRequest(Guid TenantId, string TeamsOnlineMeetingId);
