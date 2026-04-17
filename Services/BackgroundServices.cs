using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Hubs;
using PmTool.Api.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PmTool.Api.Services;

// ─── Webhook Renewal Service ──────────────────────────────────────────────────

/// <summary>
/// Runs every 6 hours. Renews Microsoft Graph webhook subscriptions before they expire (24h window).
/// Graph subscriptions expire after max 3 days for online meetings.
/// </summary>
public class WebhookRenewalService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WebhookRenewalService> _log;
    private readonly IHttpClientFactory _http;

    public WebhookRenewalService(IServiceProvider services, ILogger<WebhookRenewalService> log, IHttpClientFactory http)
    {
        _services = services; _log = log; _http = http;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("WebhookRenewalService started");

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(6), ct);
            await RenewExpiringSubscriptionsAsync(ct);
        }
    }

    private async Task RenewExpiringSubscriptionsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Find subscriptions expiring in next 24 hours
        var expirySoon = DateTime.UtcNow.AddHours(24);
        var expiringSubscriptions = await db.GraphWebhookSubscriptions
            .Where(s => s.Status == "active" && s.ExpirationDateTime < expirySoon)
            .ToListAsync(ct);

        foreach (var sub in expiringSubscriptions)
        {
            try
            {
                var tokenRecord = await db.GraphTokens
                    .Where(t => t.TenantId == sub.TenantId)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync(ct);

                if (tokenRecord == null || tokenRecord.ExpiresAt < DateTime.UtcNow)
                {
                    sub.Status = "expired";
                    sub.UpdatedAt = DateTime.UtcNow;
                    continue;
                }

                var client = _http.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenRecord.AccessToken);

                var newExpiry = DateTime.UtcNow.AddDays(2).ToString("o");
                var patch = JsonSerializer.Serialize(new { expirationDateTime = newExpiry });
                var content = new StringContent(patch, Encoding.UTF8, "application/json");

                var response = await client.PatchAsync(
                    $"https://graph.microsoft.com/v1.0/subscriptions/{sub.SubscriptionId}", content, ct);

                if (response.IsSuccessStatusCode)
                {
                    sub.ExpirationDateTime = DateTime.Parse(newExpiry);
                    sub.LastRenewedAt = DateTime.UtcNow;
                    sub.UpdatedAt = DateTime.UtcNow;
                    _log.LogInformation("Renewed Graph subscription {Id}", sub.SubscriptionId);
                }
                else
                {
                    sub.Status = "failed";
                    sub.UpdatedAt = DateTime.UtcNow;
                    _log.LogWarning("Failed to renew subscription {Id}: {Status}", sub.SubscriptionId, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error renewing subscription {Id}", sub.SubscriptionId);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}

// ─── Embedding Backfill Service ───────────────────────────────────────────────

/// <summary>
/// Runs on startup (with delay) and then every 24 hours.
/// Embeds all knowledge items and project summaries that don't yet have vectors.
/// Prerequisite: OpenAI:ApiKey must be configured.
/// </summary>
public class EmbeddingBackfillService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<EmbeddingBackfillService> _log;

    public EmbeddingBackfillService(IServiceProvider services, ILogger<EmbeddingBackfillService> log)
    {
        _services = services; _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Wait 30s for app to fully start
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
        _log.LogInformation("EmbeddingBackfillService started");

        while (!ct.IsCancellationRequested)
        {
            await BackfillAsync(ct);
            await Task.Delay(TimeSpan.FromHours(24), ct);
        }
    }

    private async Task BackfillAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var embeddings = scope.ServiceProvider.GetRequiredService<EmbeddingService>();
        var matcher = scope.ServiceProvider.GetRequiredService<ProjectMatcherService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<PmHub>>();

        if (!embeddings.IsConfigured)
        {
            _log.LogInformation("EmbeddingBackfillService skipped — OpenAI:ApiKey not configured");
            return;
        }

        var tenants = await db.Tenants.ToListAsync(ct);
        foreach (var tenant in tenants)
        {
            _log.LogInformation("Backfilling embeddings for tenant {Name}", tenant.Name);
            await embeddings.BackfillKnowledgeItemsAsync(tenant.Id);
            await matcher.BackfillProjectEmbeddingsAsync(tenant.Id);
        }

        _log.LogInformation("Embedding backfill complete");
        await hub.Clients.All.SendAsync(HubEvents.EmbeddingBackfillComplete, new { timestamp = DateTime.UtcNow }, ct);
    }
}

// ─── Monday Briefing Service ──────────────────────────────────────────────────

/// <summary>
/// Runs every hour, checks if it's Monday morning, generates and sends personalized briefings.
/// Enabled via MondayBriefing:Enabled = true in appsettings.
/// Requires: Anthropic:ApiKey + Microsoft Graph mail.send scope.
/// </summary>
public class MondayBriefingService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MondayBriefingService> _log;
    private DateTime _lastBriefingSent = DateTime.MinValue;

    public MondayBriefingService(IServiceProvider services, ILogger<MondayBriefingService> log)
    {
        _services = services; _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("MondayBriefingService started");

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(1), ct);
            await CheckAndSendBriefingsAsync(ct);
        }
    }

    private async Task CheckAndSendBriefingsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (!bool.TryParse(config["MondayBriefing:Enabled"], out var enabled) || !enabled) return;

        var now = DateTime.UtcNow;
        if (now.DayOfWeek != DayOfWeek.Monday) return;

        var targetHour = int.TryParse(config["MondayBriefing:CronHour"], out var h) ? h : 8;
        var targetMinute = int.TryParse(config["MondayBriefing:CronMinute"], out var m) ? m : 0;

        if (now.Hour != targetHour || now.Minute > targetMinute + 5) return;
        if (_lastBriefingSent.Date == now.Date) return; // Only once per day

        _lastBriefingSent = now;
        await GenerateAndSendBriefingsAsync(scope, ct);
    }

    private async Task GenerateAndSendBriefingsAsync(IServiceScope scope, CancellationToken ct)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ai = scope.ServiceProvider.GetRequiredService<AnthropicService>();
        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<PmHub>>();

        if (!ai.IsConfigured) { _log.LogInformation("Monday briefing skipped — AI not configured"); return; }

        var tenants = await db.Tenants.ToListAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var tenant in tenants)
        {
            var projects = await db.Projects
                .Where(p => p.TenantId == tenant.Id)
                .Include(p => p.Tasks)
                .Include(p => p.Risks)
                .Include(p => p.Decisions)
                .Include(p => p.TeamAssignments).ThenInclude(r => r.User)
                .ToListAsync(ct);

            var users = await db.Users.Where(u => u.TenantId == tenant.Id).ToListAsync(ct);

            foreach (var user in users)
            {
                var userProjects = projects.Where(p => p.TeamAssignments.Any(r => r.UserId == user.Id)).ToList();
                if (userProjects.Count == 0) continue;

                var isProjectLead = user.Role is "Projektleiter" or "Admin" or "Management" or "PMO";

                var context = isProjectLead
                    ? $"You are a project manager named {user.DisplayName}. Projects you lead: {string.Join(", ", userProjects.Select(p => $"{p.Name}({p.Status})"))}"
                    : $"You are a team member named {user.DisplayName}. Your projects: {string.Join(", ", userProjects.Select(p => p.Name))}";

                var userTasks = projects.SelectMany(p => p.Tasks)
                    .Where(t => t.AssigneeId == user.Id && t.Status != "done")
                    .OrderBy(t => t.DueDate).Take(5).ToList();

                var briefingData = $"""
                    {context}
                    Role: {user.Role}
                    Today: {today}
                    Your open tasks ({userTasks.Count}): {string.Join(", ", userTasks.Select(t => $"{t.Title} (due: {t.DueDate})"))}
                    """;

                var briefing = await ai.CompleteAsync(
                    $"Write a personalized Monday morning briefing for {user.DisplayName} ({user.Role}). Be concise (max 150 words). Focus on their most important 3 items. Same language as names.",
                    briefingData, 512);

                // Push via SignalR (in-app notification)
                await hub.Clients.Group(tenant.Id.ToString()).SendAsync(
                    HubEvents.MondayBriefingReady,
                    new { userId = user.Id, userName = user.DisplayName, briefing, generatedAt = DateTime.UtcNow },
                    ct);

                _log.LogInformation("Monday briefing sent to {User} ({Role})", user.DisplayName, user.Role);
            }
        }
    }
}

// ─── Project Retro Summary Service ───────────────────────────────────────────

/// <summary>
/// Called when a project status changes to "done" or "closed".
/// Generates an AI retrospective summary and stores it as institutional memory.
/// </summary>
public class ProjectRetroService
{
    private readonly AppDbContext _db;
    private readonly AnthropicService _ai;
    private readonly EmbeddingService _embed;
    private readonly ILogger<ProjectRetroService> _log;

    public ProjectRetroService(AppDbContext db, AnthropicService ai, EmbeddingService embed, ILogger<ProjectRetroService> log)
    {
        _db = db; _ai = ai; _embed = embed; _log = log;
    }

    public async Task GenerateRetroAsync(Guid tenantId, Guid projectId)
    {
        if (!_ai.IsConfigured) return;

        var project = await _db.Projects
            .Where(p => p.Id == projectId && p.TenantId == tenantId)
            .Include(p => p.Tasks)
            .Include(p => p.Risks)
            .Include(p => p.KnowledgeItems)
            .Include(p => p.Decisions)
            .FirstOrDefaultAsync();

        if (project == null) return;

        var existing = await _db.ProjectRetroSummaries.FirstOrDefaultAsync(r => r.ProjectId == projectId);
        if (existing != null) return; // Already generated

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var durationDays = (today.ToDateTime(TimeOnly.MinValue) - project.StartDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
        var budgetVariance = project.BudgetTotal > 0 ? ((project.BudgetSpent - project.BudgetTotal) / project.BudgetTotal) * 100 : 0;
        var wasDelayed = today > project.EndDate;

        var ctx = $"""
            Project: {project.Name} ({project.Category})
            Duration: {durationDays:0} days | Budget variance: {budgetVariance:F1}% | Delayed: {wasDelayed}
            Tasks completed: {project.Tasks.Count(t => t.Status == "done")}/{project.Tasks.Count}
            Risks managed: {project.Risks.Count} | Decisions made: {project.Decisions.Count}
            Top knowledge items: {string.Join("; ", project.KnowledgeItems.OrderByDescending(k => k.Importance).Take(5).Select(k => k.Title))}
            """;

        var retro = await _ai.ExtractStructuredAsync<RetroResult>(
            """
            Write a project retrospective summary for institutional memory.
            Return JSON:
            {
              "summary": "overall project summary (3-4 sentences)",
              "lessonsLearned": "key lessons learned",
              "successFactors": "what worked well",
              "riskPatterns": "recurring risks or patterns to watch"
            }
            Be specific and actionable so future projects can learn from this.
            """, ctx, 1024);

        if (retro == null) return;

        var retroEntity = new ProjectRetroSummary
        {
            TenantId = tenantId, ProjectId = projectId,
            Summary = retro.Summary, LessonsLearned = retro.LessonsLearned,
            SuccessFactors = retro.SuccessFactors, RiskPatterns = retro.RiskPatterns,
            ProjectCategory = project.Category, ProjectStage = project.Stage,
            DurationDays = (int)durationDays, BudgetVariancePercent = budgetVariance, WasDelayed = wasDelayed
        };

        _db.ProjectRetroSummaries.Add(retroEntity);
        await _db.SaveChangesAsync();

        // Store as institutional memory embedding
        var memoryText = $"{project.Name} ({project.Category}): {retro.Summary} Lessons: {retro.LessonsLearned} Risks: {retro.RiskPatterns}";
        await _embed.StoreEmbeddingAsync(tenantId, projectId, "retro", retroEntity.Id, memoryText, 5, isInstitutionalMemory: true);

        _log.LogInformation("Retro summary generated for project {Name}", project.Name);
    }
}

internal class RetroResult
{
    public string Summary { get; set; } = "";
    public string LessonsLearned { get; set; } = "";
    public string SuccessFactors { get; set; } = "";
    public string RiskPatterns { get; set; } = "";
}
