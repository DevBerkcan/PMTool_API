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

    [HttpPost("project-question")]
    public async Task<ActionResult<ProjectAiAnswerDto>> AskProjectQuestion([FromBody] ProjectAiQuestionRequest req)
    {
        var project = await _db.Projects
            .Where(p => p.Id == req.ProjectId && p.TenantId == TenantId)
            .Include(p => p.Tasks).ThenInclude(task => task.Assignee)
            .Include(p => p.Risks).ThenInclude(risk => risk.Owner)
            .Include(p => p.Decisions).ThenInclude(decision => decision.Owner)
            .Include(p => p.Milestones).ThenInclude(milestone => milestone.Owner)
            .Include(p => p.GovernanceChecks).ThenInclude(check => check.Owner)
            .Include(p => p.KnowledgeItems).ThenInclude(item => item.Author)
            .Include(p => p.Notes).ThenInclude(note => note.Author)
            .FirstOrDefaultAsync();

        if (project == null) return NotFound();

        var tokens = Tokenize(req.Question);
        var sources = new List<AiAnswerSourceDto>();

        sources.AddRange(project.KnowledgeItems
            .SelectMany(item => BuildKnowledgeAnswerSources(item, tokens))
        );

        sources.AddRange(project.Tasks
            .Select(task => new AiAnswerSourceDto(
                "task",
                task.Title,
                $"{task.Status} | {task.Assignee?.DisplayName ?? "Nicht zugewiesen"} | {BuildSnippet(task.Description)}",
                ScoreTextMatch(tokens, task.Title, task.Description, task.Status, task.Priority)))
        );

        sources.AddRange(project.Risks
            .Select(risk => new AiAnswerSourceDto(
                "risk",
                risk.Title,
                $"Score {risk.Impact * risk.Probability} | {risk.Status} | {BuildSnippet(risk.Description)}",
                ScoreTextMatch(tokens, risk.Title, risk.Description, risk.Mitigation, risk.Status) + risk.Impact * risk.Probability))
        );

        sources.AddRange(project.Decisions
            .Select(decision => new AiAnswerSourceDto(
                "decision",
                decision.Title,
                $"{decision.Status} | {BuildSnippet(decision.Context)} | Entscheidung: {BuildSnippet(decision.Decision)}",
                ScoreTextMatch(tokens, decision.Title, decision.Context, decision.Decision, decision.Status)))
        );

        sources.AddRange(project.Milestones
            .Select(milestone => new AiAnswerSourceDto(
                "milestone",
                milestone.Title,
                $"{milestone.Status} | Faellig {milestone.DueDate:yyyy-MM-dd} | {BuildSnippet(milestone.Description)}",
                ScoreTextMatch(tokens, milestone.Title, milestone.Description, milestone.Status)))
        );

        sources.AddRange(project.GovernanceChecks
            .Select(check => new AiAnswerSourceDto(
                "governance",
                check.Title,
                $"{check.Status} | {check.Area} | {BuildSnippet(check.Notes)}",
                ScoreTextMatch(tokens, check.Title, check.Notes, check.Area, check.Status)))
        );

        sources.AddRange(project.Notes
            .Select(note => new AiAnswerSourceDto(
                "note",
                note.Title,
                BuildSnippet(note.Content),
                ScoreTextMatch(tokens, note.Title, note.Content)))
        );

        var topSources = sources
            .Where(source => source.RelevanceScore > 0 || tokens.Count == 0)
            .OrderByDescending(source => source.RelevanceScore)
            .ThenBy(source => source.Type)
            .Take(6)
            .ToList();

        if (topSources.Count == 0)
        {
            topSources = sources
                .OrderByDescending(source => source.RelevanceScore)
                .Take(4)
                .ToList();
        }

        var answer = BuildProjectAnswer(project, req.Question, topSources);
        var suggestedActions = BuildSuggestedActions(project, topSources);
        var confidence = topSources.Count >= 4
            ? "high"
            : topSources.Count >= 2
                ? "medium"
                : "low";

        return Ok(new ProjectAiAnswerDto(
            project.Id,
            project.Name,
            req.Question,
            answer,
            confidence,
            topSources,
            suggestedActions
        ));
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

    [HttpGet("portfolio-briefing")]
    public async Task<ActionResult<PortfolioBriefingDto>> GetPortfolioBriefing()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var projects = await _db.Projects
            .Where(project => project.TenantId == TenantId)
            .Include(project => project.Tasks)
            .Include(project => project.Risks)
            .Include(project => project.Decisions)
            .Include(project => project.GovernanceChecks)
            .OrderBy(project => project.Name)
            .ToListAsync();

        var projectBriefings = projects.Select(project =>
        {
            var openTasks = project.Tasks.Count(task => task.Status != "done");
            var criticalRisks = project.Risks.Count(risk => risk.Status == "open" && risk.Impact * risk.Probability >= 12);
            var openDecisions = project.Decisions.Count(decision => decision.Status == "open" || decision.Status == "review");
            var openGovernanceChecks = project.GovernanceChecks.Count(check => check.Status != "done");

            var headline = criticalRisks > 0
                ? $"{criticalRisks} kritische Risiken brauchen Eskalation."
                : openDecisions > 0
                    ? $"{openDecisions} Entscheidungen sind noch offen."
                    : $"{openTasks} offene Tasks, Governance {openGovernanceChecks}.";

            return new PortfolioBriefingProjectDto(
                project.Id,
                project.Name,
                project.Status,
                project.ProgressPercent,
                openTasks,
                criticalRisks,
                openDecisions,
                openGovernanceChecks,
                headline
            );
        }).ToList();

        var highlights = new List<string>
        {
            $"{projects.Count} Projekte im Portfolio, davon {projects.Count(project => project.Status == "green")} gruene, {projects.Count(project => project.Status == "yellow")} gelbe und {projects.Count(project => project.Status == "red")} rote.",
            $"{projectBriefings.Sum(project => project.CriticalRisks)} kritische Risiken und {projectBriefings.Sum(project => project.OpenDecisions)} offene Entscheidungen im Gesamtportfolio.",
            $"{projectBriefings.Sum(project => project.OpenGovernanceChecks)} offene Governance-Checks ueber alle Projekte."
        };

        var escalations = projectBriefings
            .Where(project => project.CriticalRisks > 0 || project.Status == "red" || project.OpenGovernanceChecks >= 3)
            .OrderByDescending(project => project.CriticalRisks)
            .ThenByDescending(project => project.OpenGovernanceChecks)
            .Select(project => $"{project.ProjectName}: {project.Headline}")
            .Take(5)
            .ToList();

        if (escalations.Count == 0)
        {
            escalations.Add("Aktuell keine akute Portfolio-Eskalation erkannt. Fokus auf Delivery-Stabilisierung und offene Entscheidungen.");
        }

        var summary = $"Portfolio-Briefing: {projects.Count} Projekte, {projectBriefings.Sum(project => project.OpenTasks)} offene Tasks, {projectBriefings.Sum(project => project.CriticalRisks)} kritische Risiken und {projectBriefings.Sum(project => project.OpenGovernanceChecks)} offene Governance-Checks.";

        return Ok(new PortfolioBriefingDto(summary, highlights, escalations, projectBriefings));
    }

    [HttpGet("risk-signals")]
    public async Task<ActionResult<List<RiskSignalDto>>> GetRiskSignals([FromQuery] Guid? projectId = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var projects = await _db.Projects
            .Where(project => project.TenantId == TenantId && (!projectId.HasValue || project.Id == projectId.Value))
            .Include(project => project.Tasks)
            .Include(project => project.Risks)
            .Include(project => project.Milestones)
            .Include(project => project.KnowledgeItems)
            .ToListAsync();

        var signals = new List<RiskSignalDto>();

        foreach (var project in projects)
        {
            var overdueTasks = project.Tasks.Where(task => task.DueDate.HasValue && task.DueDate.Value < today && task.Status != "done").ToList();
            if (overdueTasks.Count >= 2)
            {
                signals.Add(new RiskSignalDto(
                    project.Id,
                    project.Name,
                    overdueTasks.Count >= 4 ? "high" : "medium",
                    "Task-Verzug als Fruehwarnsignal",
                    $"{overdueTasks.Count} Tasks sind ueberfaellig und gefaehrden den Delivery-Plan.",
                    overdueTasks.Select(task => task.Title).Take(4).ToList(),
                    overdueTasks.Count * 4
                ));
            }

            var criticalRisks = project.Risks.Where(risk => risk.Status == "open" && risk.Impact * risk.Probability >= 12).ToList();
            if (criticalRisks.Count > 0)
            {
                signals.Add(new RiskSignalDto(
                    project.Id,
                    project.Name,
                    "high",
                    "Bereits erkannte kritische Risiken",
                    $"{criticalRisks.Count} offene Risiken liegen im kritischen Bereich.",
                    criticalRisks.Select(risk => risk.Title).Take(4).ToList(),
                    criticalRisks.Sum(risk => risk.Impact * risk.Probability)
                ));
            }

            var overdueMilestones = project.Milestones.Where(milestone => milestone.DueDate < today && milestone.Status != "done").ToList();
            if (overdueMilestones.Count > 0)
            {
                signals.Add(new RiskSignalDto(
                    project.Id,
                    project.Name,
                    overdueMilestones.Count >= 2 ? "high" : "medium",
                    "Meilenstein-Risiko",
                    $"{overdueMilestones.Count} Meilensteine sind ueberfaellig und deuten auf Planabweichungen hin.",
                    overdueMilestones.Select(milestone => milestone.Title).Take(4).ToList(),
                    overdueMilestones.Count * 5
                ));
            }

            var riskyKnowledge = project.KnowledgeItems
                .Where(item =>
                    item.Content.Contains("risiko", StringComparison.OrdinalIgnoreCase)
                    || item.Content.Contains("blocker", StringComparison.OrdinalIgnoreCase)
                    || item.Content.Contains("verzug", StringComparison.OrdinalIgnoreCase)
                    || item.Content.Contains("fehlend", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.Importance)
                .Take(3)
                .ToList();

            if (riskyKnowledge.Count > 0)
            {
                signals.Add(new RiskSignalDto(
                    project.Id,
                    project.Name,
                    riskyKnowledge.Any(item => item.Importance >= 5) ? "high" : "medium",
                    "Wissensbasierte Risiko-Signale",
                    "Knowledge-Eintraege enthalten Hinweise auf Blocker, Verzug oder fehlende Voraussetzungen.",
                    riskyKnowledge.Select(item => item.Title).ToList(),
                    riskyKnowledge.Sum(item => item.Importance * 2)
                ));
            }
        }

        return Ok(signals
            .OrderByDescending(signal => signal.Score)
            .ThenBy(signal => signal.ProjectName)
            .ToList());
    }

    [HttpGet("learning-summary")]
    public async Task<ActionResult<AiLearningSummaryDto>> GetLearningSummary()
    {
        var feedback = await _db.AiSuggestionFeedback
            .Where(item => item.Project!.TenantId == TenantId)
            .Include(item => item.Project)
            .ToListAsync();

        var byType = feedback
            .GroupBy(item => item.SuggestionType)
            .Select(group => new AiLearningByTypeDto(
                group.Key,
                group.Count(item => item.Status == "accepted"),
                group.Count(item => item.Status == "rejected"),
                group.Count(item => item.Status == "edited")))
            .OrderByDescending(group => group.Accepted)
            .ThenBy(group => group.SuggestionType)
            .ToList();

        var byProject = feedback
            .GroupBy(item => new { item.ProjectId, ProjectName = item.Project?.Name ?? "" })
            .Select(group => new AiLearningByProjectDto(
                group.Key.ProjectId,
                group.Key.ProjectName,
                group.Count(item => item.Status == "accepted"),
                group.Count(item => item.Status == "rejected"),
                group.Count(item => item.Status == "edited")))
            .OrderByDescending(group => group.Accepted + group.Edited)
            .ThenBy(group => group.ProjectName)
            .ToList();

        var accepted = feedback.Count(item => item.Status == "accepted");
        var rejected = feedback.Count(item => item.Status == "rejected");
        var edited = feedback.Count(item => item.Status == "edited");

        var insights = new List<string>();
        var strongestType = byType.OrderByDescending(item => item.Accepted).FirstOrDefault();
        if (strongestType != null)
        {
            insights.Add($"Am haeufigsten werden derzeit Vorschlaege vom Typ '{strongestType.SuggestionType}' angenommen.");
        }

        if (rejected > accepted)
        {
            insights.Add("Mehr Vorschlaege werden verworfen als angenommen. Empfehlung: Schwellwerte und Priorisierung schaerfen.");
        }
        else
        {
            insights.Add("Die Akzeptanz ist aktuell hoeher als die Ablehnung. Das ist ein positives Signal fuer die Suggestion-Qualitaet.");
        }

        var strongestProject = byProject.FirstOrDefault();
        if (strongestProject != null)
        {
            insights.Add($"{strongestProject.ProjectName} liefert aktuell die meisten Lernsignale fuer die KI-Auswertung.");
        }

        return Ok(new AiLearningSummaryDto(
            feedback.Count,
            accepted,
            rejected,
            edited,
            byType,
            byProject,
            insights
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

    private static List<string> Tokenize(string? question) => (question ?? "")
        .ToLowerInvariant()
        .Split(new[] { ' ', ',', '.', ';', ':', '\n', '\r', '\t', '-', '_', '/', '\\', '(', ')' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(token => token.Length >= 3)
        .Distinct()
        .ToList();

    private static int ScoreTextMatch(List<string> tokens, params string[] parts)
    {
        if (tokens.Count == 0)
        {
            return 1;
        }

        var score = 0;
        foreach (var part in parts.Where(part => !string.IsNullOrWhiteSpace(part)))
        {
            foreach (var token in tokens)
            {
                if (part.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    score += 5;
                }
            }
        }

        return score;
    }

    private static List<AiAnswerSourceDto> BuildKnowledgeAnswerSources(ProjectKnowledgeItem item, List<string> tokens)
    {
        var chunks = SplitIntoChunks(item.Content);
        return chunks.Select((chunk, index) => new AiAnswerSourceDto(
            "knowledge",
            index == 0 ? item.Title : $"{item.Title} (Abschnitt {index + 1})",
            $"{item.SourceType}/{item.Category}: {BuildSnippet(chunk)}",
            ScoreTextMatch(tokens, item.Title, chunk, item.TagsCsv, item.SourceType, item.SourceLabel, item.Category) + item.Importance * 5 + (item.Version > 1 ? 1 : 0)
        )).ToList();
    }

    private static string BuildSnippet(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var normalized = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return normalized.Length <= 120 ? normalized : normalized[..120] + "...";
    }

    private static List<string> SplitIntoChunks(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var normalized = content.Replace("\r\n", "\n");
        var paragraphs = normalized
            .Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(paragraph => paragraph.Replace('\n', ' ').Trim())
            .Where(paragraph => paragraph.Length > 0)
            .ToList();

        if (paragraphs.Count > 0)
        {
            return paragraphs
                .SelectMany(paragraph => paragraph.Length <= 220
                    ? new[] { paragraph }
                    : paragraph.Chunk(200).Select(chunk => new string(chunk).Trim()).Where(chunk => chunk.Length > 0))
                .ToList();
        }

        return normalized.Length <= 220
            ? [normalized.Trim()]
            : normalized.Chunk(200).Select(chunk => new string(chunk).Trim()).Where(chunk => chunk.Length > 0).ToList();
    }

    private static string BuildProjectAnswer(Project project, string question, List<AiAnswerSourceDto> sources)
    {
        var lowered = question.ToLowerInvariant();
        if (lowered.Contains("risik"))
        {
            var risks = sources.Where(source => source.Type == "risk").Take(3).ToList();
            return risks.Count == 0
                ? $"Fuer {project.Name} gibt es aus dem aktuellen Wissensstand keine stark passenden Risikoquellen."
                : $"Fuer {project.Name} stehen aktuell vor allem diese Risiken im Vordergrund: {string.Join(" | ", risks.Select(risk => $"{risk.Title}: {risk.Detail}"))}";
        }

        if (lowered.Contains("wissen") || lowered.Contains("kontext") || lowered.Contains("stand"))
        {
            return $"Der staerkste Projektkontext fuer {project.Name} ergibt sich aus: {string.Join(" | ", sources.Take(4).Select(source => $"{source.Type}: {source.Title}"))}.";
        }

        if (lowered.Contains("naechst") || lowered.Contains("aktion") || lowered.Contains("tun"))
        {
            var actions = BuildSuggestedActions(project, sources);
            return $"Die naechsten sinnvollen Schritte fuer {project.Name} sind: {string.Join(" | ", actions.Take(3))}";
        }

        return $"Fuer {project.Name} sind die relevantesten Quellen zu Ihrer Frage: {string.Join(" | ", sources.Take(4).Select(source => $"{source.Type}: {source.Title}"))}. Daraus ergibt sich ein aktueller Fokus auf Fortschritt, Risiken und offene Entscheidungen.";
    }

    private static List<string> BuildSuggestedActions(Project project, List<AiAnswerSourceDto> sources)
    {
        var actions = new List<string>();
        if (sources.Any(source => source.Type == "risk"))
        {
            actions.Add("Kritische Risiken mit Owner und Massnahme im naechsten Statusmeeting durchgehen.");
        }

        if (sources.Any(source => source.Type == "decision"))
        {
            actions.Add("Offene Entscheidungen in das naechste Steering oder Projektmeeting aufnehmen.");
        }

        if (sources.Any(source => source.Type == "milestone"))
        {
            actions.Add("Meilensteinplanung mit realistischem Termin und Verantwortlichen absichern.");
        }

        if (sources.Any(source => source.Type == "knowledge"))
        {
            actions.Add("Wichtige Knowledge-Eintraege in konkrete Tasks, Risiken oder Governance-Checks ueberfuehren.");
        }

        if (actions.Count == 0)
        {
            actions.Add($"Projekt {project.Name} weiter mit Fokus auf {project.NextMilestone} steuern.");
        }

        return actions.Distinct().ToList();
    }
}
