using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using System.Security.Claims;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/[controller]"), Authorize]
public class AiController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public AiController(AppDbContext db) => _db = db;

    [HttpPost("chat")]
    public async Task<ActionResult<AiChatResponse>> Chat([FromBody] AiChatRequest req)
    {
        var projects = await _db.Projects
            .Where(p => p.TenantId == TenantId && (!req.ProjectId.HasValue || p.Id == req.ProjectId.Value))
            .Include(p => p.Tasks)
            .Include(p => p.Risks)
            .Include(p => p.KnowledgeItems)
            .ToListAsync();

        var q = req.Message.ToLowerInvariant();
        string reply;

        if (q.Contains("status") || q.Contains("ueberblick"))
        {
            var red = projects.Where(p => p.Status == "red").ToList();
            var yellow = projects.Where(p => p.Status == "yellow").ToList();
            reply = $"Portfolio: {projects.Count} Projekte | Gruen: {projects.Count(p => p.Status == "green")} | Gelb: {yellow.Count} ({string.Join(", ", yellow.Select(p => p.Name))}) | Rot: {red.Count} ({string.Join(", ", red.Select(p => p.Name))})";
        }
        else if (q.Contains("risik"))
        {
            var allRisks = projects.SelectMany(p => p.Risks).ToList();
            var critical = allRisks.Where(r => r.Impact * r.Probability >= 15).ToList();
            reply = $"Risiken: {allRisks.Count} gesamt, {critical.Count} kritisch. {string.Join(" | ", critical.Select(r => $"{r.Title} (Score {r.Impact * r.Probability})"))}";
        }
        else if (q.Contains("task"))
        {
            var allTasks = projects.SelectMany(p => p.Tasks).ToList();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            reply = $"Tasks: {allTasks.Count} gesamt | Ueberfaellig: {allTasks.Count(t => t.DueDate < today && t.Status != "done")} | In Progress: {allTasks.Count(t => t.Status == "in_progress")}";
        }
        else if (q.Contains("wissen") || q.Contains("knowledge") || q.Contains("kontext"))
        {
            var topItems = projects
                .SelectMany(p => p.KnowledgeItems.Select(item => new { p.Name, Item = item }))
                .OrderByDescending(x => x.Item.Importance)
                .ThenByDescending(x => x.Item.CreatedAt)
                .Take(5)
                .ToList();

            reply = topItems.Count == 0
                ? "Es liegen aktuell noch keine Knowledge-Eintraege fuer den gewaehlten Kontext vor."
                : "Wichtigster Projektkontext: " + string.Join(" | ", topItems.Select(x => $"{x.Name}: {x.Item.Title} [{x.Item.SourceType}]"));
        }
        else
        {
            reply = "Ich helfe bei Projektstatus, Risiken, Tasks und Knowledge-Kontext. Stellen Sie eine konkrete Frage!";
        }

        return Ok(new AiChatResponse(reply));
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult<List<AiSuggestionDto>>> GetSuggestions([FromQuery] Guid? projectId = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var projects = await _db.Projects
            .Where(p => p.TenantId == TenantId && (!projectId.HasValue || p.Id == projectId.Value))
            .Include(p => p.Tasks)
            .Include(p => p.Risks)
            .Include(p => p.Milestones)
            .Include(p => p.Decisions)
            .Include(p => p.GovernanceChecks)
            .Include(p => p.KnowledgeItems)
            .Include(p => p.AiSuggestionFeedback)
            .ToListAsync();

        var suggestions = new List<AiSuggestionDto>();

        foreach (var project in projects)
        {
            var overdueMilestones = project.Milestones.Where(m => m.DueDate < today && m.Status != "done").ToList();
            if (overdueMilestones.Count > 0)
            {
                suggestions.Add(BuildSuggestion(
                    project,
                    "milestone",
                    "Ueberfaelligen Meilenstein eskalieren",
                    "Mindestens ein Meilenstein ist ueberfaellig und noch nicht erledigt.",
                    "Status mit Owner pruefen, neues Zieldatum setzen und Management-Update vorbereiten.",
                    "high",
                    overdueMilestones.Select(m => m.Title).Take(3).ToList()
                ));
            }

            var openDecisions = project.Decisions.Where(d => d.Status == "open" || d.Status == "review").ToList();
            if (openDecisions.Count > 0)
            {
                suggestions.Add(BuildSuggestion(
                    project,
                    "decision",
                    "Offene Entscheidung aufloesen",
                    "Es gibt offene oder in Review befindliche Entscheidungen mit Steuerungsrelevanz.",
                    "Die naechste Entscheidungsvorlage im Steering oder Projektmeeting platzieren.",
                    "medium",
                    openDecisions.Select(d => d.Title).Take(3).ToList()
                ));
            }

            var openGovernance = project.GovernanceChecks.Where(g => g.Status != "done").ToList();
            if (openGovernance.Count > 0)
            {
                suggestions.Add(BuildSuggestion(
                    project,
                    "governance",
                    "Governance-Luecke schliessen",
                    "Es existieren offene Governance-Checks fuer das Projekt.",
                    "Pruefen, welche Pflichtartefakte oder Freigaben vor dem naechsten Gate fehlen.",
                    "high",
                    openGovernance.Select(g => g.Title).Take(3).ToList()
                ));
            }

            var criticalRisks = project.Risks.Where(r => r.Status == "open" && r.Impact * r.Probability >= 12).ToList();
            if (criticalRisks.Count > 0)
            {
                suggestions.Add(BuildSuggestion(
                    project,
                    "risk",
                    "Kritisches Risiko priorisieren",
                    "Mindestens ein offenes Risiko liegt im kritischen Bereich.",
                    "Massnahme, Owner und Eskalationspfad fuer das hoechste Risiko konkretisieren.",
                    "high",
                    criticalRisks.Select(r => r.Title).Take(3).ToList()
                ));
            }

            var overdueTasks = project.Tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value < today && t.Status != "done").ToList();
            if (overdueTasks.Count > 0)
            {
                suggestions.Add(BuildSuggestion(
                    project,
                    "task",
                    "Ueberfaellige Aufgaben neu planen",
                    "Es gibt ueberfaellige Tasks, die die Planung gefaehrden koennen.",
                    "Betroffene Tasks im Wochenstatus benennen und mit den Verantwortlichen neu terminieren.",
                    "medium",
                    overdueTasks.Select(t => t.Title).Take(3).ToList()
                ));
            }

            var knowledgeSignals = project.KnowledgeItems
                .Where(item => item.Importance >= 4)
                .OrderByDescending(item => item.Importance)
                .ThenByDescending(item => item.CreatedAt)
                .Take(3)
                .ToList();

            if (knowledgeSignals.Count > 0)
            {
                suggestions.Add(BuildSuggestion(
                    project,
                    "knowledge",
                    "Wissensbasis in konkrete Schritte uebersetzen",
                    "Es liegen priorisierte Knowledge-Eintraege mit Steuerungsrelevanz vor.",
                    "Die wichtigsten Erkenntnisse in Entscheidungen, Tasks oder Governance-Checks ueberfuehren.",
                    knowledgeSignals.Any(item => item.SourceType == "import" || item.SourceType == "meeting") ? "high" : "medium",
                    knowledgeSignals.Select(item => item.Title).ToList()
                ));
            }
        }

        return Ok(suggestions
            .OrderByDescending(s => s.Priority == "high")
            .ThenBy(s => s.ProjectName)
            .ThenBy(s => s.Title)
            .ToList());
    }

    [HttpGet("weekly-status/{projectId}")]
    public async Task<ActionResult<WeeklyStatusDto>> GetWeeklyStatus(Guid projectId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var project = await _db.Projects
            .Where(p => p.Id == projectId && p.TenantId == TenantId)
            .Include(p => p.Tasks)
            .Include(p => p.Risks)
            .Include(p => p.Milestones)
            .Include(p => p.Decisions)
            .Include(p => p.GovernanceChecks)
            .Include(p => p.KnowledgeItems)
            .FirstOrDefaultAsync();

        if (project == null) return NotFound();

        var overdueTasks = project.Tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < today && t.Status != "done");
        var criticalRisks = project.Risks.Count(r => r.Status == "open" && r.Impact * r.Probability >= 12);
        var overdueMilestones = project.Milestones.Count(m => m.DueDate < today && m.Status != "done");
        var openDecisions = project.Decisions.Count(d => d.Status == "open" || d.Status == "review");
        var openGovernance = project.GovernanceChecks.Count(g => g.Status != "done");
        var keyKnowledge = project.KnowledgeItems
            .OrderByDescending(item => item.Importance)
            .ThenByDescending(item => item.CreatedAt)
            .Take(2)
            .ToList();

        var highlights = new List<string>
        {
            $"{project.ProgressPercent}% Fortschritt bei Status {project.Status}.",
            $"{project.Tasks.Count(t => t.Status != "done")} offene Tasks, davon {overdueTasks} ueberfaellig.",
            $"{criticalRisks} kritische Risiken, {openDecisions} offene Entscheidungen."
        };
        highlights.AddRange(keyKnowledge.Select(item => $"Knowledge: {item.Title} ({item.SourceType})."));

        var nextActions = new List<string>();
        if (overdueMilestones > 0) nextActions.Add("Ueberfaellige Meilensteine im Wochenstatus eskalieren.");
        if (openGovernance > 0) nextActions.Add("Offene Governance-Checks vor dem naechsten Gate schliessen.");
        if (criticalRisks > 0) nextActions.Add("Top-Risiken mit Massnahme und Owner im Steuerkreis platzieren.");
        if (keyKnowledge.Count > 0) nextActions.Add($"Wichtigste Erkenntnis aus '{keyKnowledge[0].Title}' in konkrete Aufgaben oder Entscheidungen ueberfuehren.");
        if (nextActions.Count == 0) nextActions.Add("Projekt auf Kurs halten und den naechsten Meilenstein absichern.");

        var summary = $"{project.Name} ist aktuell {project.Status} mit {project.ProgressPercent}% Fortschritt. " +
                      $"Budgetverbrauch liegt bei {project.BudgetSpent:0} von {project.BudgetTotal:0}. " +
                      $"Im Fokus stehen {project.NextMilestone}.";

        return Ok(new WeeklyStatusDto(
            project.Id,
            project.Name,
            summary,
            $"Offene Tasks: {project.Tasks.Count(t => t.Status != "done")}, ueberfaellig: {overdueTasks}, naechster Meilenstein: {project.NextMilestone}.",
            $"Offene Risiken: {project.Risks.Count(r => r.Status == "open")}, kritisch: {criticalRisks}.",
            $"Governance Checks offen: {openGovernance}, Entscheidungen offen: {openDecisions}, ueberfaellige Meilensteine: {overdueMilestones}.",
            highlights,
            nextActions
        ));
    }

    [HttpGet("feedback")]
    public async Task<ActionResult<List<AiSuggestionFeedbackDto>>> GetFeedback([FromQuery] Guid? projectId = null)
    {
        var feedback = await _db.AiSuggestionFeedback
            .Where(item => item.Project!.TenantId == TenantId && (!projectId.HasValue || item.ProjectId == projectId.Value))
            .Include(item => item.User)
            .Include(item => item.Project)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        return Ok(feedback.Select(MapFeedback));
    }

    [HttpPost("feedback")]
    public async Task<ActionResult<AiSuggestionFeedbackDto>> AddFeedback([FromBody] CreateAiSuggestionFeedbackRequest req)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var existing = await _db.AiSuggestionFeedback
            .FirstOrDefaultAsync(item => item.ProjectId == req.ProjectId && item.SuggestionType == req.Type && item.SuggestionTitle == req.Title);

        if (existing != null)
        {
            existing.Status = req.Status;
            existing.Notes = req.Notes;
            existing.UserId = UserId;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            existing.User = await _db.Users.FindAsync(UserId);
            return Ok(MapFeedback(existing));
        }

        var feedback = new AiSuggestionFeedback
        {
            ProjectId = req.ProjectId,
            UserId = UserId,
            SuggestionType = req.Type,
            SuggestionTitle = req.Title,
            Status = req.Status,
            Notes = req.Notes
        };

        _db.AiSuggestionFeedback.Add(feedback);
        _db.ActivityLogs.Add(new ActivityLog
        {
            TenantId = TenantId,
            UserId = UserId,
            ProjectId = req.ProjectId,
            EntityId = feedback.Id,
            EntityType = "AiSuggestionFeedback",
            Action = $"hat AI-Vorschlag bewertet: {req.Title} ({req.Status})"
        });
        await _db.SaveChangesAsync();

        feedback.User = await _db.Users.FindAsync(UserId);
        return Ok(MapFeedback(feedback));
    }

    [HttpPost("apply-suggestion")]
    public async Task<ActionResult<ApplyAiSuggestionResponse>> ApplySuggestion([FromBody] ApplyAiSuggestionRequest req)
    {
        var project = await _db.Projects
            .Include(p => p.AiSuggestionFeedback)
            .FirstOrDefaultAsync(p => p.Id == req.ProjectId && p.TenantId == TenantId);

        if (project == null) return NotFound();

        var normalizedTarget = req.TargetType.Trim().ToLowerInvariant();
        Guid entityId;
        string entityTitle;

        switch (normalizedTarget)
        {
            case "task":
                var task = new ProjectTask
                {
                    ProjectId = req.ProjectId,
                    Title = req.Title,
                    Description = string.IsNullOrWhiteSpace(req.Notes) ? req.Recommendation : $"{req.Recommendation}\n\nFreigabe-Notiz: {req.Notes}",
                    Status = "todo",
                    Priority = req.Type is "risk" or "governance" ? "high" : "medium",
                    AssigneeId = UserId,
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                    EstimatedHours = 4
                };
                _db.Tasks.Add(task);
                entityId = task.Id;
                entityTitle = task.Title;
                break;
            case "risk":
                var risk = new Risk
                {
                    ProjectId = req.ProjectId,
                    OwnerId = UserId,
                    Title = req.Title,
                    Description = string.IsNullOrWhiteSpace(req.Notes) ? req.Recommendation : $"{req.Recommendation}\n\nFreigabe-Notiz: {req.Notes}",
                    Impact = 4,
                    Probability = 3,
                    Status = "open",
                    Mitigation = "Aus AI-Freigabe erzeugt. Massnahme im Projekt abstimmen."
                };
                _db.Risks.Add(risk);
                entityId = risk.Id;
                entityTitle = risk.Title;
                break;
            case "decision":
                var decision = new ProjectDecision
                {
                    ProjectId = req.ProjectId,
                    OwnerId = UserId,
                    Title = req.Title,
                    Context = req.Recommendation,
                    Decision = string.IsNullOrWhiteSpace(req.Notes) ? "Zur Entscheidung vorbereitet." : req.Notes,
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                    Status = "open"
                };
                _db.ProjectDecisions.Add(decision);
                entityId = decision.Id;
                entityTitle = decision.Title;
                break;
            default:
                return BadRequest(new { message = "TargetType muss task, risk oder decision sein." });
        }

        var existingFeedback = await _db.AiSuggestionFeedback
            .FirstOrDefaultAsync(item => item.ProjectId == req.ProjectId && item.SuggestionType == req.Type && item.SuggestionTitle == req.Title);

        if (existingFeedback == null)
        {
            existingFeedback = new AiSuggestionFeedback
            {
                ProjectId = req.ProjectId,
                UserId = UserId,
                SuggestionType = req.Type,
                SuggestionTitle = req.Title,
                Status = "accepted",
                Notes = req.Notes ?? ""
            };
            _db.AiSuggestionFeedback.Add(existingFeedback);
        }
        else
        {
            existingFeedback.UserId = UserId;
            existingFeedback.Status = "accepted";
            existingFeedback.Notes = req.Notes ?? existingFeedback.Notes;
            existingFeedback.UpdatedAt = DateTime.UtcNow;
        }

        _db.ActivityLogs.Add(new ActivityLog
        {
            TenantId = TenantId,
            UserId = UserId,
            ProjectId = req.ProjectId,
            EntityId = entityId,
            EntityType = "AiSuggestionApply",
            Action = $"hat AI-Vorschlag uebernommen als {normalizedTarget}: {req.Title}"
        });

        await _db.SaveChangesAsync();

        return Ok(new ApplyAiSuggestionResponse(req.ProjectId, normalizedTarget, entityId, entityTitle, "accepted"));
    }

    private static AiSuggestionDto BuildSuggestion(Project project, string type, string title, string reason, string recommendation, string priority, List<string> sources)
        => new(
            project.Id,
            project.Name,
            type,
            title,
            reason,
            recommendation,
            priority,
            sources,
            ResolveFeedbackStatus(project, type, title)
        );

    private static string ResolveFeedbackStatus(Project project, string type, string title)
        => project.AiSuggestionFeedback
            .Where(item => item.SuggestionType == type && item.SuggestionTitle == title)
            .OrderByDescending(item => item.UpdatedAt)
            .Select(item => item.Status)
            .FirstOrDefault() ?? "open";

    private static AiSuggestionFeedbackDto MapFeedback(AiSuggestionFeedback item)
        => new(item.Id, item.ProjectId, item.SuggestionType, item.SuggestionTitle, item.Status, item.Notes, item.User?.DisplayName ?? "", item.CreatedAt);
}
