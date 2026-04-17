using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Hubs;
using PmTool.Api.Models;
using PmTool.Api.Services;
using System.Security.Claims;
using System.Text.Json;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/[controller]"), Authorize]
public class AiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AnthropicService _ai;
    private readonly EmbeddingService _embed;
    private readonly IHubContext<PmHub> _hub;
    private readonly CommandService _commands;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string UserName => User.FindFirstValue(ClaimTypes.Name) ?? "PM";

    public AiController(AppDbContext db, AnthropicService ai, EmbeddingService embed, IHubContext<PmHub> hub, CommandService commands)
    {
        _db = db;
        _ai = ai;
        _embed = embed;
        _hub = hub;
        _commands = commands;
    }

    // ─── Command system ───────────────────────────────────────────────────────

    [HttpPost("command")]
    public async Task<ActionResult<CommandResult>> ExecuteCommand([FromBody] CommandRequest req)
    {
        var result = await _commands.ExecuteAsync(TenantId, UserId, UserName, req.Input, req.ProjectId);
        return Ok(result);
    }

    // ─── RAG-powered chat ─────────────────────────────────────────────────────

    [HttpPost("chat")]
    public async Task<ActionResult<AiChatResponse>> Chat([FromBody] AiChatRequest req)
    {
        // Load conversation history (last 10 turns)
        var conversationId = req.ConversationId ?? Guid.NewGuid().ToString("N");
        var history = (await _db.AiConversations
            .Where(c => c.TenantId == TenantId && c.ConversationId == conversationId)
            .OrderBy(c => c.CreatedAt)
            .TakeLast(10)
            .ToListAsync())
            .Select(c => (c.Role, c.Content))
            .ToList();

        // Semantic search for relevant context
        var semanticContext = "";
        var sources = new List<AiAnswerSourceDto>();

        if (_embed.IsConfigured)
        {
            var matches = await _embed.SearchAsync(TenantId, req.Message, req.ProjectId, topK: 6, includeInstitutionalMemory: true);
            if (matches.Count > 0)
            {
                semanticContext = "\n\n## Relevant project context (RAG):\n" +
                    string.Join("\n", matches.Select((m, i) => $"[{i + 1}] [{m.EntityType.ToUpper()}] {m.Text[..Math.Min(300, m.Text.Length)]}"));

                sources = matches.Select(m => new AiAnswerSourceDto(
                    m.EntityType, $"{m.EntityType} — {m.EntityId}", m.Text[..Math.Min(120, m.Text.Length)], (int)(m.Score * 100)
                )).ToList();
            }
        }

        // Load structured project data for grounding
        var projectContext = await BuildProjectContextAsync(req.ProjectId);

        var systemPrompt = $"""
            You are an intelligent project management assistant for the RealCore PM platform.
            You help project managers with status overviews, risk analysis, task planning, decisions, and knowledge retrieval.

            Current user: {UserName}
            Today: {DateTime.UtcNow:yyyy-MM-dd}

            ## Project Portfolio Context:
            {projectContext}
            {semanticContext}

            ## Instructions:
            - Answer in the same language the user writes in (German or English).
            - Be concise and action-oriented. Lead with the answer.
            - When citing knowledge items, mention the source type (e.g., "laut Meeting-Protokoll vom...").
            - If you detect a critical risk or overdue item, proactively flag it.
            - Format responses using markdown when helpful (bullet points, bold for key terms).
            - If the user asks to create something (task, risk, decision), respond with a JSON block in a ```action``` fence.
            """;

        var turn = new List<(string Role, string Content)>(history) { ("user", req.Message) };
        var reply = await _ai.ChatAsync(systemPrompt, turn);

        // Persist conversation
        _db.AiConversations.Add(new AiConversation { TenantId = TenantId, UserId = UserId, ProjectId = req.ProjectId, Role = "user", Content = req.Message, ConversationId = conversationId });
        _db.AiConversations.Add(new AiConversation { TenantId = TenantId, UserId = UserId, ProjectId = req.ProjectId, Role = "assistant", Content = reply, ConversationId = conversationId });
        await _db.SaveChangesAsync();

        return Ok(new AiChatResponse(reply, conversationId, sources));
    }

    // ─── RAG project question ─────────────────────────────────────────────────

    [HttpPost("project-question")]
    public async Task<ActionResult<ProjectAiAnswerDto>> AskProjectQuestion([FromBody] ProjectAiQuestionRequest req)
    {
        var project = await _db.Projects
            .Where(p => p.Id == req.ProjectId && p.TenantId == TenantId)
            .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
            .Include(p => p.Risks).ThenInclude(r => r.Owner)
            .Include(p => p.Decisions).ThenInclude(d => d.Owner)
            .Include(p => p.Milestones).ThenInclude(m => m.Owner)
            .Include(p => p.GovernanceChecks)
            .Include(p => p.KnowledgeItems).ThenInclude(k => k.Author)
            .Include(p => p.Notes)
            .FirstOrDefaultAsync();

        if (project == null) return NotFound();

        // Semantic search in project scope
        var sources = new List<AiAnswerSourceDto>();
        string ragContext = "";

        if (_embed.IsConfigured)
        {
            var matches = await _embed.SearchAsync(TenantId, req.Question, req.ProjectId, topK: 8);
            if (matches.Count > 0)
            {
                ragContext = string.Join("\n", matches.Select((m, i) =>
                    $"[{i + 1}] [{m.EntityType.ToUpper()}] {m.Text[..Math.Min(400, m.Text.Length)]}"));

                sources = matches.Select(m => new AiAnswerSourceDto(
                    m.EntityType, m.EntityType, m.Text[..Math.Min(120, m.Text.Length)], (int)(m.Score * 100)
                )).ToList();
            }
        }

        // Build structured summary
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overdueTasks = project.Tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value < today && t.Status != "done").ToList();
        var criticalRisks = project.Risks.Where(r => r.Status == "open" && r.Impact * r.Probability >= 12).ToList();

        var structuredContext = $"""
            Project: {project.Name} | Status: {project.Status} | Progress: {project.ProgressPercent}%
            Budget: {project.BudgetSpent:0}/{project.BudgetTotal:0} EUR
            Tasks: {project.Tasks.Count} total, {overdueTasks.Count} overdue
            Critical risks: {criticalRisks.Count} ({string.Join(", ", criticalRisks.Select(r => r.Title))})
            Open decisions: {project.Decisions.Count(d => d.Status == "open")}
            Next milestone: {project.NextMilestone}
            Team: {string.Join(", ", project.Tasks.Where(t => t.Assignee != null).Select(t => t.Assignee!.DisplayName).Distinct())}
            """;

        var systemPrompt = $"""
            You are an expert project management AI assistant analyzing the project "{project.Name}".
            Today: {DateTime.UtcNow:yyyy-MM-dd}

            ## Project State:
            {structuredContext}

            ## Relevant Knowledge (semantic search results):
            {(string.IsNullOrEmpty(ragContext) ? "No embeddings available — using structured data only." : ragContext)}

            Answer the user's question precisely and concisely. Cite specific items (task names, risk titles, dates).
            Provide 1-3 concrete suggested next actions. Answer in the same language as the question.
            """;

        var answer = await _ai.CompleteAsync(systemPrompt, req.Question);
        var suggestedActions = BuildSuggestedActions(project, sources);
        var confidence = _embed.IsConfigured && sources.Count >= 3 ? "high" : sources.Count >= 1 ? "medium" : "low";

        return Ok(new ProjectAiAnswerDto(project.Id, project.Name, req.Question, answer, confidence, sources, suggestedActions));
    }

    // ─── AI Suggestions (LLM-enhanced) ───────────────────────────────────────

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
                suggestions.Add(BuildSuggestion(project, "milestone", "Ueberfaelligen Meilenstein eskalieren",
                    $"{overdueMilestones.Count} Meilenstein(e) sind ueberfaellig: {string.Join(", ", overdueMilestones.Select(m => m.Title))}",
                    "Status mit Owner pruefen, neues Zieldatum setzen und Management-Update vorbereiten.",
                    "high", overdueMilestones.Select(m => m.Title).Take(3).ToList()));

            var openDecisions = project.Decisions.Where(d => d.Status is "open" or "review").ToList();
            if (openDecisions.Count > 0)
                suggestions.Add(BuildSuggestion(project, "decision", "Offene Entscheidung aufloesen",
                    $"{openDecisions.Count} Entscheidung(en) ausstehend: {string.Join(", ", openDecisions.Select(d => d.Title))}",
                    "Die naechste Entscheidungsvorlage im Steering oder Projektmeeting platzieren.",
                    "medium", openDecisions.Select(d => d.Title).Take(3).ToList()));

            var openGovernance = project.GovernanceChecks.Where(g => g.Status != "done").ToList();
            if (openGovernance.Count > 0)
                suggestions.Add(BuildSuggestion(project, "governance", "Governance-Luecke schliessen",
                    $"{openGovernance.Count} offene Governance-Checks fuer {project.Name}.",
                    "Pruefen, welche Pflichtartefakte oder Freigaben vor dem naechsten Gate fehlen.",
                    "high", openGovernance.Select(g => g.Title).Take(3).ToList()));

            var criticalRisks = project.Risks.Where(r => r.Status == "open" && r.Impact * r.Probability >= 12).ToList();
            if (criticalRisks.Count > 0)
                suggestions.Add(BuildSuggestion(project, "risk", "Kritisches Risiko priorisieren",
                    $"{criticalRisks.Count} kritische(s) Risiko(en): {string.Join(", ", criticalRisks.Select(r => $"{r.Title} (Score {r.Impact * r.Probability})"))}",
                    "Massnahme, Owner und Eskalationspfad fuer das hoechste Risiko konkretisieren.",
                    "high", criticalRisks.Select(r => r.Title).Take(3).ToList()));

            var overdueTasks = project.Tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value < today && t.Status != "done").ToList();
            if (overdueTasks.Count > 0)
                suggestions.Add(BuildSuggestion(project, "task", "Ueberfaellige Aufgaben neu planen",
                    $"{overdueTasks.Count} Tasks ueberfaellig: {string.Join(", ", overdueTasks.Select(t => t.Title).Take(3))}",
                    "Betroffene Tasks im Wochenstatus benennen und mit den Verantwortlichen neu terminieren.",
                    "medium", overdueTasks.Select(t => t.Title).Take(3).ToList()));

            var knowledgeSignals = project.KnowledgeItems.Where(i => i.Importance >= 4).OrderByDescending(i => i.Importance).Take(3).ToList();
            if (knowledgeSignals.Count > 0)
                suggestions.Add(BuildSuggestion(project, "knowledge", "Wissensbasis in konkrete Schritte uebersetzen",
                    $"Priorisierte Knowledge-Eintraege mit Steuerungsrelevanz vorhanden.",
                    "Die wichtigsten Erkenntnisse in Entscheidungen, Tasks oder Governance-Checks ueberfuehren.",
                    knowledgeSignals.Any(i => i.SourceType is "import" or "meeting") ? "high" : "medium",
                    knowledgeSignals.Select(i => i.Title).ToList()));
        }

        return Ok(suggestions.OrderByDescending(s => s.Priority == "high").ThenBy(s => s.ProjectName).ThenBy(s => s.Title).ToList());
    }

    // ─── LLM-powered weekly status ────────────────────────────────────────────

    [HttpGet("weekly-status/{projectId}")]
    public async Task<ActionResult<WeeklyStatusDto>> GetWeeklyStatus(Guid projectId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var project = await _db.Projects
            .Where(p => p.Id == projectId && p.TenantId == TenantId)
            .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
            .Include(p => p.Risks)
            .Include(p => p.Milestones)
            .Include(p => p.Decisions)
            .Include(p => p.GovernanceChecks)
            .Include(p => p.KnowledgeItems)
            .FirstOrDefaultAsync();

        if (project == null) return NotFound();

        var overdueTasks = project.Tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value < today && t.Status != "done").ToList();
        var criticalRisks = project.Risks.Where(r => r.Status == "open" && r.Impact * r.Probability >= 12).ToList();
        var overdueMilestones = project.Milestones.Where(m => m.DueDate < today && m.Status != "done").ToList();
        var openDecisions = project.Decisions.Where(d => d.Status is "open" or "review").ToList();
        var openGovernance = project.GovernanceChecks.Where(g => g.Status != "done").ToList();

        if (_ai.IsConfigured)
        {
            var dataContext = $"""
                Project: {project.Name}
                Status: {project.Status} (green=on track, yellow=at risk, red=critical)
                Progress: {project.ProgressPercent}%
                Budget: {project.BudgetSpent:0} / {project.BudgetTotal:0} EUR spent
                Next milestone: {project.NextMilestone}
                Overdue tasks ({overdueTasks.Count}): {string.Join(", ", overdueTasks.Select(t => $"{t.Title} (owner: {t.Assignee?.DisplayName ?? "unassigned"})"))}
                Critical risks ({criticalRisks.Count}): {string.Join(", ", criticalRisks.Select(r => $"{r.Title} score={r.Impact * r.Probability}"))}
                Overdue milestones ({overdueMilestones.Count}): {string.Join(", ", overdueMilestones.Select(m => m.Title))}
                Open decisions ({openDecisions.Count}): {string.Join(", ", openDecisions.Select(d => d.Title))}
                Open governance checks ({openGovernance.Count}): {string.Join(", ", openGovernance.Select(g => g.Title))}
                Key knowledge items: {string.Join(", ", project.KnowledgeItems.OrderByDescending(k => k.Importance).Take(3).Select(k => k.Title))}
                """;

            var systemPrompt = """
                You are a senior project manager writing a concise weekly status report.
                Write in a professional, action-oriented tone. Use German if the project context is in German.
                Structure: 1) Executive Summary (2 sentences), 2) Delivery Focus, 3) Risk Focus, 4) Governance Focus, 5) Top 3 Next Actions.
                Keep the total under 250 words. Be specific — name actual tasks, risks, milestones.
                Return ONLY the report text, no extra commentary.
                """;

            var report = await _ai.CompleteAsync(systemPrompt, $"Write a weekly status report for:\n{dataContext}");

            var highlights = new List<string>
            {
                $"{project.ProgressPercent}% Fortschritt | Status: {project.Status}",
                $"{overdueTasks.Count} ueberfaellige Tasks | {criticalRisks.Count} kritische Risiken",
                $"Naechster Meilenstein: {project.NextMilestone}"
            };

            var nextActions = new List<string>();
            if (overdueMilestones.Count > 0) nextActions.Add($"Meilensteine eskalieren: {string.Join(", ", overdueMilestones.Select(m => m.Title))}");
            if (criticalRisks.Count > 0) nextActions.Add($"Top-Risiken addressieren: {criticalRisks[0].Title}");
            if (openDecisions.Count > 0) nextActions.Add($"Entscheidungen treffen: {string.Join(", ", openDecisions.Take(2).Select(d => d.Title))}");
            if (nextActions.Count == 0) nextActions.Add("Projekt auf Kurs halten und naechsten Meilenstein absichern.");

            return Ok(new WeeklyStatusDto(project.Id, project.Name, report, "", "", "", highlights, nextActions));
        }

        // Fallback (no AI key)
        var summary = $"{project.Name}: {project.Status}, {project.ProgressPercent}% Fortschritt. Budget: {project.BudgetSpent:0}/{project.BudgetTotal:0}. Naechster Meilenstein: {project.NextMilestone}.";
        var fb_highlights = new List<string> { $"{project.ProgressPercent}% Fortschritt", $"{overdueTasks.Count} ueberfaellige Tasks", $"{criticalRisks.Count} kritische Risiken" };
        var fb_actions = new List<string> { "Status-Meeting vorbereiten", "Offene Risiken bewerten" };

        return Ok(new WeeklyStatusDto(project.Id, project.Name, summary,
            $"Tasks: {project.Tasks.Count(t => t.Status != "done")} offen, {overdueTasks.Count} ueberfaellig",
            $"Risiken: {project.Risks.Count(r => r.Status == "open")} offen, {criticalRisks.Count} kritisch",
            $"Governance: {openGovernance.Count} offen, Entscheidungen: {openDecisions.Count}",
            fb_highlights, fb_actions));
    }

    // ─── Portfolio briefing ───────────────────────────────────────────────────

    [HttpGet("portfolio-briefing")]
    public async Task<ActionResult<PortfolioBriefingDto>> GetPortfolioBriefing()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var projects = await _db.Projects
            .Where(p => p.TenantId == TenantId)
            .Include(p => p.Tasks)
            .Include(p => p.Risks)
            .Include(p => p.Decisions)
            .Include(p => p.GovernanceChecks)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var projectBriefings = projects.Select(p =>
        {
            var openTasks = p.Tasks.Count(t => t.Status != "done");
            var criticalRisks = p.Risks.Count(r => r.Status == "open" && r.Impact * r.Probability >= 12);
            var openDecisions = p.Decisions.Count(d => d.Status is "open" or "review");
            var openGovernance = p.GovernanceChecks.Count(g => g.Status != "done");
            var headline = criticalRisks > 0 ? $"{criticalRisks} kritische Risiken brauchen Eskalation."
                : openDecisions > 0 ? $"{openDecisions} Entscheidungen ausstehend."
                : $"{openTasks} offene Tasks, Governance {openGovernance} offen.";
            return new PortfolioBriefingProjectDto(p.Id, p.Name, p.Status, p.ProgressPercent, openTasks, criticalRisks, openDecisions, openGovernance, headline);
        }).ToList();

        var highlights = new List<string>
        {
            $"{projects.Count} Projekte: {projects.Count(p => p.Status == "green")} gruen, {projects.Count(p => p.Status == "yellow")} gelb, {projects.Count(p => p.Status == "red")} rot.",
            $"{projectBriefings.Sum(p => p.CriticalRisks)} kritische Risiken | {projectBriefings.Sum(p => p.OpenDecisions)} offene Entscheidungen.",
            $"{projectBriefings.Sum(p => p.OpenGovernanceChecks)} offene Governance-Checks."
        };

        var escalations = projectBriefings
            .Where(p => p.CriticalRisks > 0 || p.Status == "red" || p.OpenGovernanceChecks >= 3)
            .OrderByDescending(p => p.CriticalRisks).ThenByDescending(p => p.OpenGovernanceChecks)
            .Select(p => $"{p.ProjectName}: {p.Headline}").Take(5).ToList();

        if (escalations.Count == 0)
            escalations.Add("Keine akute Portfolio-Eskalation. Fokus auf Delivery-Stabilisierung.");

        string summary;
        if (_ai.IsConfigured)
        {
            var ctx = string.Join("\n", projectBriefings.Select(p =>
                $"- {p.ProjectName} ({p.Status}): {p.OpenTasks} tasks, {p.CriticalRisks} critical risks, {p.OpenDecisions} decisions, {p.OpenGovernanceChecks} governance checks"));

            summary = await _ai.CompleteAsync(
                "You are a senior PMO writing an executive portfolio briefing. Be concise (3-4 sentences max). Focus on top 2-3 risks and required executive decisions. Use the same language as the project names.",
                $"Write a portfolio briefing for these projects:\n{ctx}");
        }
        else
        {
            summary = $"Portfolio: {projects.Count} Projekte, {projectBriefings.Sum(p => p.OpenTasks)} offene Tasks, {projectBriefings.Sum(p => p.CriticalRisks)} kritische Risiken.";
        }

        return Ok(new PortfolioBriefingDto(summary, highlights, escalations, projectBriefings));
    }

    // ─── Risk signals ─────────────────────────────────────────────────────────

    [HttpGet("risk-signals")]
    public async Task<ActionResult<List<RiskSignalDto>>> GetRiskSignals([FromQuery] Guid? projectId = null)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var projects = await _db.Projects
            .Where(p => p.TenantId == TenantId && (!projectId.HasValue || p.Id == projectId.Value))
            .Include(p => p.Tasks)
            .Include(p => p.Risks)
            .Include(p => p.Milestones)
            .Include(p => p.KnowledgeItems)
            .ToListAsync();

        var signals = new List<RiskSignalDto>();

        foreach (var project in projects)
        {
            var overdueTasks = project.Tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value < today && t.Status != "done").ToList();
            if (overdueTasks.Count >= 2)
                signals.Add(new RiskSignalDto(project.Id, project.Name,
                    overdueTasks.Count >= 4 ? "high" : "medium",
                    "Task-Verzug als Fruehwarnsignal",
                    $"{overdueTasks.Count} Tasks sind ueberfaellig und gefaehrden den Delivery-Plan.",
                    overdueTasks.Select(t => t.Title).Take(4).ToList(),
                    overdueTasks.Count * 4));

            var criticalRisks = project.Risks.Where(r => r.Status == "open" && r.Impact * r.Probability >= 12).ToList();
            if (criticalRisks.Count > 0)
                signals.Add(new RiskSignalDto(project.Id, project.Name, "high",
                    "Erkannte kritische Risiken",
                    $"{criticalRisks.Count} offene Risiken im kritischen Bereich. Hoechster Score: {criticalRisks.Max(r => r.Impact * r.Probability)}.",
                    criticalRisks.Select(r => r.Title).Take(4).ToList(),
                    criticalRisks.Sum(r => r.Impact * r.Probability)));

            var overdueMilestones = project.Milestones.Where(m => m.DueDate < today && m.Status != "done").ToList();
            if (overdueMilestones.Count > 0)
                signals.Add(new RiskSignalDto(project.Id, project.Name,
                    overdueMilestones.Count >= 2 ? "high" : "medium",
                    "Meilenstein-Risiko",
                    $"{overdueMilestones.Count} Meilenstein(e) ueberfaellig — Planabweichung erkannt.",
                    overdueMilestones.Select(m => m.Title).Take(4).ToList(),
                    overdueMilestones.Count * 5));

            // Semantic risk signals from knowledge items
            var riskyKnowledge = project.KnowledgeItems
                .Where(i => i.Content.Contains("risiko", StringComparison.OrdinalIgnoreCase)
                    || i.Content.Contains("blocker", StringComparison.OrdinalIgnoreCase)
                    || i.Content.Contains("verzug", StringComparison.OrdinalIgnoreCase)
                    || i.Content.Contains("fehlend", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i => i.Importance).Take(3).ToList();

            if (riskyKnowledge.Count > 0)
                signals.Add(new RiskSignalDto(project.Id, project.Name,
                    riskyKnowledge.Any(i => i.Importance >= 5) ? "high" : "medium",
                    "Wissensbasierte Risiko-Signale",
                    "Knowledge-Eintraege enthalten Hinweise auf Blocker, Verzug oder fehlende Voraussetzungen.",
                    riskyKnowledge.Select(i => i.Title).ToList(),
                    riskyKnowledge.Sum(i => i.Importance * 2)));
        }

        return Ok(signals.OrderByDescending(s => s.Score).ThenBy(s => s.ProjectName).ToList());
    }

    // ─── Natural language task creation ──────────────────────────────────────

    [HttpPost("create-from-text")]
    public async Task<ActionResult<NlTaskCreateResponse>> CreateFromNaturalLanguage([FromBody] NlTaskCreateRequest req)
    {
        var project = await _db.Projects
            .Where(p => p.Id == req.ProjectId && p.TenantId == TenantId)
            .Include(p => p.TeamAssignments).ThenInclude(r => r.User)
            .FirstOrDefaultAsync();

        if (project == null) return NotFound();

        var teamNames = project.TeamAssignments.Select(r => r.User?.DisplayName ?? "").Where(n => n.Length > 0).ToList();

        var systemPrompt = $$"""
            You are a task extraction assistant. Extract a structured task from natural language input.
            Project: {{project.Name}}
            Team members: {{string.Join(", ", teamNames)}}
            Today: {{DateTime.UtcNow:yyyy-MM-dd}} (use this to calculate due dates from "next Friday", "in 3 days", etc.)

            Return ONLY a JSON object with these fields:
            {
              "title": "short task title (max 80 chars)",
              "description": "detailed description",
              "assigneeName": "name from team list above, or empty string",
              "priority": "high|medium|low",
              "dueDaysFromNow": <integer days from today>,
              "confidence": <0.0-1.0>
            }
            """;

        var result = await _ai.ExtractStructuredAsync<NlTaskResult>(systemPrompt, req.Text);

        if (result == null || result.Confidence < 0.5f)
            return BadRequest(new { message = "Konnte keinen Task aus dem Text extrahieren. Bitte praezisieren." });

        // Find assignee
        Guid? assigneeId = null;
        if (!string.IsNullOrWhiteSpace(result.AssigneeName))
        {
            var assignee = project.TeamAssignments
                .FirstOrDefault(r => r.User?.DisplayName?.Contains(result.AssigneeName, StringComparison.OrdinalIgnoreCase) == true);
            assigneeId = assignee?.UserId;
        }

        if (req.DryRun)
        {
            return Ok(new NlTaskCreateResponse(
                null, result.Title, result.Description,
                result.AssigneeName, result.Priority,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(result.DueDaysFromNow)),
                result.Confidence, true));
        }

        var task = new ProjectTask
        {
            ProjectId = req.ProjectId,
            Title = result.Title,
            Description = result.Description,
            Status = "todo",
            Priority = result.Priority,
            AssigneeId = assigneeId ?? UserId,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(result.DueDaysFromNow)),
            EstimatedHours = 4
        };

        _db.Tasks.Add(task);
        _db.ActivityLogs.Add(new ActivityLog
        {
            TenantId = TenantId, UserId = UserId, ProjectId = req.ProjectId,
            EntityId = task.Id, EntityType = "Task",
            Action = $"hat Task per KI erstellt: {task.Title}"
        });
        await _db.SaveChangesAsync();

        // Embed the new task
        _ = _embed.StoreEmbeddingAsync(TenantId, req.ProjectId, "task", task.Id, $"{task.Title}\n{task.Description}");

        // SignalR push
        await _hub.Clients.Group(TenantId.ToString()).SendAsync("TaskCreated", new { projectId = req.ProjectId, taskId = task.Id, title = task.Title, source = "ai" });

        return Ok(new NlTaskCreateResponse(task.Id, task.Title, task.Description,
            result.AssigneeName, task.Priority, task.DueDate!.Value, result.Confidence, false));
    }

    // ─── Deadline prediction ──────────────────────────────────────────────────

    [HttpGet("deadline-prediction/{projectId}")]
    public async Task<ActionResult<DeadlinePrediction>> PredictDeadline(Guid projectId)
    {
        var project = await _db.Projects
            .Where(p => p.Id == projectId && p.TenantId == TenantId)
            .Include(p => p.Tasks)
            .Include(p => p.Milestones)
            .FirstOrDefaultAsync();

        if (project == null) return NotFound();

        // Gather velocity data
        var timeEntries = await _db.TimeEntries
            .Where(t => t.ProjectId == projectId && t.TenantId == TenantId)
            .OrderByDescending(t => t.Year).ThenByDescending(t => t.Month)
            .Take(90)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalTasks = project.Tasks.Count;
        var doneTasks = project.Tasks.Count(t => t.Status == "done");
        var overdueTasks = project.Tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < today && t.Status != "done");
        var overdueMilestones = project.Milestones.Count(m => m.DueDate < today && m.Status != "done");
        var totalHoursLogged = timeEntries.Sum(t => (double)t.GeleistetHours);
        var daysSinceStart = (today.ToDateTime(TimeOnly.MinValue) - project.StartDate.ToDateTime(TimeOnly.MinValue)).TotalDays;

        if (!_ai.IsConfigured)
        {
            // Simple rule-based fallback
            var progressRatio = totalTasks > 0 ? (double)doneTasks / totalTasks : project.ProgressPercent / 100.0;
            var daysTotal = (project.EndDate.ToDateTime(TimeOnly.MinValue) - project.StartDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
            var predictedDays = progressRatio > 0 ? daysSinceStart / progressRatio : daysTotal;
            var predicted = project.StartDate.AddDays((int)predictedDays);
            var delay = (predicted.ToDateTime(TimeOnly.MinValue) - project.EndDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
            return Ok(new DeadlinePrediction
            {
                PredictedDeliveryDate = predicted,
                ConfidencePercent = 55,
                Reasoning = $"Basierend auf {doneTasks}/{totalTasks} erledigten Tasks und {daysSinceStart:0} Tagen seit Projektstart.",
                Trend = delay > 7 ? "delayed" : delay > 0 ? "at_risk" : "on_track",
                DelayDays = (int)Math.Max(0, delay)
            });
        }

        var dataCtx = $"""
            Project: {project.Name}
            Planned start: {project.StartDate}, Planned end: {project.EndDate}
            Current date: {today}
            Days since start: {daysSinceStart:0}
            Progress: {project.ProgressPercent}% (manual) | Tasks done: {doneTasks}/{totalTasks}
            Overdue tasks: {overdueTasks} | Overdue milestones: {overdueMilestones}
            Total hours logged: {totalHoursLogged:0}h across last 90 days of entries
            Budget spent: {project.BudgetSpent:0}/{project.BudgetTotal:0} EUR
            """;

        var prediction = await _ai.ExtractStructuredAsync<DeadlinePrediction>(
            """
            You are a project delivery prediction expert. Analyze the data and predict the delivery date.
            Return ONLY a JSON object:
            {
              "predictedDeliveryDate": "YYYY-MM-DD",
              "confidencePercent": <integer 0-100>,
              "reasoning": "brief explanation in same language as project",
              "trend": "on_track|at_risk|delayed",
              "delayDays": <integer, 0 if on track>
            }
            """,
            dataCtx, 512);

        if (prediction == null)
            return Ok(new DeadlinePrediction
            {
                PredictedDeliveryDate = project.EndDate,
                ConfidencePercent = 40,
                Reasoning = "Vorhersage konnte nicht generiert werden.",
                Trend = "on_track",
                DelayDays = 0
            });

        return Ok(prediction);
    }

    // ─── AI Meeting Agenda Generator ──────────────────────────────────────────

    [HttpGet("meeting-agenda/{projectId}")]
    public async Task<ActionResult<MeetingAgendaDto>> GenerateMeetingAgenda(Guid projectId)
    {
        var project = await _db.Projects
            .Where(p => p.Id == projectId && p.TenantId == TenantId)
            .Include(p => p.Tasks)
            .Include(p => p.Risks)
            .Include(p => p.Decisions)
            .Include(p => p.GovernanceChecks)
            .Include(p => p.Milestones)
            .FirstOrDefaultAsync();

        if (project == null) return NotFound();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overdueTasks = project.Tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value < today && t.Status != "done").ToList();
        var criticalRisks = project.Risks.Where(r => r.Status == "open" && r.Impact * r.Probability >= 12).ToList();
        var openDecisions = project.Decisions.Where(d => d.Status is "open" or "review").ToList();
        var upcomingMilestones = project.Milestones.Where(m => m.Status != "done" && m.DueDate >= today && m.DueDate <= today.AddDays(21)).ToList();

        if (!_ai.IsConfigured)
        {
            var items = new List<AgendaItem>
            {
                new("Statusupdate", $"Fortschritt {project.ProgressPercent}%, Budget und Naechster Meilenstein: {project.NextMilestone}", 5),
                new("Offene Tasks", $"{overdueTasks.Count} ueberfaellige Tasks besprechen", 10),
                new("Risiken", $"{criticalRisks.Count} kritische Risiken", 10),
                new("Entscheidungen", $"{openDecisions.Count} offene Entscheidungen", 10),
                new("Naechste Schritte", "Massnahmen und Verantwortlichkeiten festlegen", 5)
            };
            return Ok(new MeetingAgendaDto(project.Id, project.Name, items, 40, DateTime.UtcNow));
        }

        var ctx = $"""
            Project: {project.Name} | Status: {project.Status} | Progress: {project.ProgressPercent}%
            Overdue tasks: {string.Join(", ", overdueTasks.Select(t => t.Title))}
            Critical risks: {string.Join(", ", criticalRisks.Select(r => $"{r.Title} (score {r.Impact * r.Probability})"))}
            Open decisions: {string.Join(", ", openDecisions.Select(d => d.Title))}
            Upcoming milestones (next 3 weeks): {string.Join(", ", upcomingMilestones.Select(m => $"{m.Title} due {m.DueDate}"))}
            """;

        var agenda = await _ai.ExtractStructuredAsync<MeetingAgendaResult>(
            """
            You are a project meeting facilitator. Create a concise meeting agenda based on the project state.
            Return ONLY JSON:
            {
              "items": [
                {"title": "...", "description": "...", "minutesAllocated": <int>}
              ],
              "totalMinutes": <int>
            }
            Keep to max 6 items, total max 60 minutes.
            """, ctx, 1024);

        var agendaItems = agenda?.Items?.Select(i => new AgendaItem(i.Title, i.Description, i.MinutesAllocated)).ToList()
            ?? new List<AgendaItem> { new("Review", "Projektstand besprechen", 30) };

        return Ok(new MeetingAgendaDto(project.Id, project.Name, agendaItems, agenda?.TotalMinutes ?? 30, DateTime.UtcNow));
    }

    // ─── Resource optimization ────────────────────────────────────────────────

    [HttpGet("resource-optimization")]
    public async Task<ActionResult<ResourceOptimizationDto>> GetResourceOptimization()
    {
        var projects = await _db.Projects
            .Where(p => p.TenantId == TenantId)
            .Include(p => p.TeamAssignments).ThenInclude(r => r.User)
            .Include(p => p.Tasks)
            .ToListAsync();

        var users = await _db.Users.Where(u => u.TenantId == TenantId).ToListAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var userAllocations = projects
            .SelectMany(p => p.TeamAssignments.Select(r => new
            {
                UserId = r.UserId,
                UserName = r.User?.DisplayName ?? "",
                ProjectId = p.Id,
                ProjectName = p.Name,
                AllocatedHours = r.AllocatedHours,
                ProjectStatus = p.Status,
                OpenTaskCount = p.Tasks.Count(t => t.AssigneeId == r.UserId && t.Status != "done"),
                OverdueTaskCount = p.Tasks.Count(t => t.AssigneeId == r.UserId && t.DueDate.HasValue && t.DueDate.Value < today && t.Status != "done")
            }))
            .GroupBy(a => new { a.UserId, a.UserName })
            .Select(g => new UserAllocationSummary(
                g.Key.UserId, g.Key.UserName,
                g.Sum(a => a.AllocatedHours),
                g.Select(a => new ProjectLoad(a.ProjectId, a.ProjectName, a.AllocatedHours, a.OpenTaskCount, a.OverdueTaskCount, a.ProjectStatus)).ToList()
            ))
            .ToList();

        var overloaded = userAllocations.Where(u => u.TotalAllocatedHours > 40).ToList();
        var underloaded = userAllocations.Where(u => u.TotalAllocatedHours < 16).ToList();

        if (!_ai.IsConfigured)
        {
            var suggestions = new List<string>();
            foreach (var ol in overloaded)
                suggestions.Add($"{ol.UserName} ist mit {ol.TotalAllocatedHours}h/Monat ueberbelastet — Aufgaben umverteilen.");
            foreach (var ul in underloaded)
                suggestions.Add($"{ul.UserName} hat freie Kapazitaet ({ul.TotalAllocatedHours}h belegt) — koennte kritische Projekte unterstuetzen.");
            return Ok(new ResourceOptimizationDto(userAllocations, overloaded.Count, underloaded.Count, suggestions));
        }

        var ctx = string.Join("\n", userAllocations.Select(u =>
            $"{u.UserName}: {u.TotalAllocatedHours}h allocated across {u.Projects.Count} projects. Overdue tasks: {u.Projects.Sum(p => p.OverdueTaskCount)}"));

        var aiSuggestions = await _ai.CompleteAsync(
            "You are a resource planning expert. Analyze team allocation and suggest concrete rebalancing actions. Be specific (name people and projects). Max 5 bullet points. Use same language as names.",
            $"Team allocation:\n{ctx}\n\nOverloaded ({overloaded.Count}): {string.Join(", ", overloaded.Select(u => u.UserName))}\nUnderloaded ({underloaded.Count}): {string.Join(", ", underloaded.Select(u => u.UserName))}");

        var suggestionList = aiSuggestions.Split('\n').Where(s => s.Trim().Length > 10).Take(5).ToList();
        return Ok(new ResourceOptimizationDto(userAllocations, overloaded.Count, underloaded.Count, suggestionList));
    }

    // ─── Email summarizer ─────────────────────────────────────────────────────

    [HttpPost("email-summary")]
    public async Task<ActionResult<EmailSummaryDto>> SummarizeEmail([FromBody] EmailSummaryRequest req)
    {
        if (!_ai.IsConfigured)
            return Ok(new EmailSummaryDto("AI nicht konfiguriert.", new List<string> { "Anthropic API Key setzen." }, null, null));

        var systemPrompt = """
            You are an executive assistant. Analyze an email thread and:
            1. Write a 2-3 sentence summary
            2. List 2-3 suggested reply options (formal/brief/action-requesting)
            3. If a decision is mentioned, extract it as a structured decision object
            4. If action items are mentioned, extract them

            Return JSON:
            {
              "summary": "...",
              "replyOptions": ["option1", "option2", "option3"],
              "decision": {"title": "...", "context": "..."} or null,
              "actionItems": ["..."] or []
            }
            Respond in the same language as the email.
            """;

        var result = await _ai.ExtractStructuredAsync<EmailAnalysisResult>(systemPrompt, req.EmailText, 1024);

        if (result == null)
            return Ok(new EmailSummaryDto("Zusammenfassung nicht verfuegbar.", new List<string>(), null, null));

        return Ok(new EmailSummaryDto(result.Summary, result.ReplyOptions, result.Decision, result.ActionItems));
    }

    // ─── Learning summary ─────────────────────────────────────────────────────

    [HttpGet("learning-summary")]
    public async Task<ActionResult<AiLearningSummaryDto>> GetLearningSummary()
    {
        var feedback = await _db.AiSuggestionFeedback
            .Where(i => i.Project!.TenantId == TenantId)
            .Include(i => i.Project)
            .ToListAsync();

        var byType = feedback.GroupBy(i => i.SuggestionType)
            .Select(g => new AiLearningByTypeDto(g.Key, g.Count(i => i.Status == "accepted"), g.Count(i => i.Status == "rejected"), g.Count(i => i.Status == "edited")))
            .OrderByDescending(g => g.Accepted).ToList();

        var byProject = feedback.GroupBy(i => new { i.ProjectId, Name = i.Project?.Name ?? "" })
            .Select(g => new AiLearningByProjectDto(g.Key.ProjectId, g.Key.Name, g.Count(i => i.Status == "accepted"), g.Count(i => i.Status == "rejected"), g.Count(i => i.Status == "edited")))
            .OrderByDescending(g => g.Accepted + g.Edited).ToList();

        var accepted = feedback.Count(i => i.Status == "accepted");
        var rejected = feedback.Count(i => i.Status == "rejected");
        var edited = feedback.Count(i => i.Status == "edited");

        var insights = new List<string>();
        var strongestType = byType.FirstOrDefault();
        if (strongestType != null) insights.Add($"Am haeufigsten angenommen: '{strongestType.SuggestionType}'.");
        insights.Add(rejected > accepted ? "Mehr Vorschlaege verworfen als angenommen — Schwellwerte pruefen." : "Akzeptanzrate positiv — Vorschlagsqualitaet gut.");
        if (byProject.FirstOrDefault() is { } top) insights.Add($"{top.ProjectName} liefert die meisten Lernsignale.");

        return Ok(new AiLearningSummaryDto(feedback.Count, accepted, rejected, edited, byType, byProject, insights));
    }

    // ─── Feedback ─────────────────────────────────────────────────────────────

    [HttpGet("feedback")]
    public async Task<ActionResult<List<AiSuggestionFeedbackDto>>> GetFeedback([FromQuery] Guid? projectId = null)
    {
        var feedback = await _db.AiSuggestionFeedback
            .Where(i => i.Project!.TenantId == TenantId && (!projectId.HasValue || i.ProjectId == projectId.Value))
            .Include(i => i.User).Include(i => i.Project)
            .OrderByDescending(i => i.CreatedAt).ToListAsync();
        return Ok(feedback.Select(MapFeedback));
    }

    [HttpPost("feedback")]
    public async Task<ActionResult<AiSuggestionFeedbackDto>> AddFeedback([FromBody] CreateAiSuggestionFeedbackRequest req)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var existing = await _db.AiSuggestionFeedback.FirstOrDefaultAsync(i => i.ProjectId == req.ProjectId && i.SuggestionType == req.Type && i.SuggestionTitle == req.Title);
        if (existing != null)
        {
            existing.Status = req.Status; existing.Notes = req.Notes; existing.UserId = UserId; existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            existing.User = await _db.Users.FindAsync(UserId);
            return Ok(MapFeedback(existing));
        }

        var feedback = new AiSuggestionFeedback { ProjectId = req.ProjectId, UserId = UserId, SuggestionType = req.Type, SuggestionTitle = req.Title, Status = req.Status, Notes = req.Notes };
        _db.AiSuggestionFeedback.Add(feedback);
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = req.ProjectId, EntityId = feedback.Id, EntityType = "AiSuggestionFeedback", Action = $"hat AI-Vorschlag bewertet: {req.Title} ({req.Status})" });
        await _db.SaveChangesAsync();
        feedback.User = await _db.Users.FindAsync(UserId);
        return Ok(MapFeedback(feedback));
    }

    // ─── Apply suggestion ─────────────────────────────────────────────────────

    [HttpPost("apply-suggestion")]
    public async Task<ActionResult<ApplyAiSuggestionResponse>> ApplySuggestion([FromBody] ApplyAiSuggestionRequest req)
    {
        var project = await _db.Projects.Include(p => p.AiSuggestionFeedback).FirstOrDefaultAsync(p => p.Id == req.ProjectId && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var normalizedTarget = req.TargetType.Trim().ToLowerInvariant();
        Guid entityId; string entityTitle;

        switch (normalizedTarget)
        {
            case "task":
                var task = new ProjectTask { ProjectId = req.ProjectId, Title = req.Title, Description = string.IsNullOrWhiteSpace(req.Notes) ? req.Recommendation : $"{req.Recommendation}\n\nFreigabe-Notiz: {req.Notes}", Status = "todo", Priority = req.Type is "risk" or "governance" ? "high" : "medium", AssigneeId = UserId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), EstimatedHours = 4 };
                _db.Tasks.Add(task); entityId = task.Id; entityTitle = task.Title;
                _ = _embed.StoreEmbeddingAsync(TenantId, req.ProjectId, "task", task.Id, $"{task.Title}\n{task.Description}");
                break;
            case "risk":
                var risk = new Risk { ProjectId = req.ProjectId, OwnerId = UserId, Title = req.Title, Description = string.IsNullOrWhiteSpace(req.Notes) ? req.Recommendation : $"{req.Recommendation}\n{req.Notes}", Impact = 4, Probability = 3, Status = "open", Mitigation = "Aus AI-Freigabe erzeugt." };
                _db.Risks.Add(risk); entityId = risk.Id; entityTitle = risk.Title;
                break;
            case "decision":
                var decision = new ProjectDecision { ProjectId = req.ProjectId, OwnerId = UserId, Title = req.Title, Context = req.Recommendation, Decision = string.IsNullOrWhiteSpace(req.Notes) ? "Zur Entscheidung vorbereitet." : req.Notes, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), Status = "open" };
                _db.ProjectDecisions.Add(decision); entityId = decision.Id; entityTitle = decision.Title;
                break;
            default:
                return BadRequest(new { message = "TargetType muss task, risk oder decision sein." });
        }

        var existingFeedback = await _db.AiSuggestionFeedback.FirstOrDefaultAsync(i => i.ProjectId == req.ProjectId && i.SuggestionType == req.Type && i.SuggestionTitle == req.Title);
        if (existingFeedback == null) { _db.AiSuggestionFeedback.Add(new AiSuggestionFeedback { ProjectId = req.ProjectId, UserId = UserId, SuggestionType = req.Type, SuggestionTitle = req.Title, Status = "accepted", Notes = req.Notes ?? "" }); }
        else { existingFeedback.UserId = UserId; existingFeedback.Status = "accepted"; existingFeedback.Notes = req.Notes ?? existingFeedback.Notes; existingFeedback.UpdatedAt = DateTime.UtcNow; }

        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = req.ProjectId, EntityId = entityId, EntityType = "AiSuggestionApply", Action = $"hat AI-Vorschlag uebernommen als {normalizedTarget}: {req.Title}" });
        await _db.SaveChangesAsync();

        return Ok(new ApplyAiSuggestionResponse(req.ProjectId, normalizedTarget, entityId, entityTitle, "accepted"));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<string> BuildProjectContextAsync(Guid? projectId)
    {
        if (projectId.HasValue)
        {
            var p = await _db.Projects.Where(p => p.Id == projectId && p.TenantId == TenantId)
                .Include(p => p.Tasks).Include(p => p.Risks).Include(p => p.Decisions).FirstOrDefaultAsync();
            if (p == null) return "";
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return $"Project: {p.Name} | Status: {p.Status} | Progress: {p.ProgressPercent}% | " +
                   $"Tasks: {p.Tasks.Count(t => t.Status != "done")} open, {p.Tasks.Count(t => t.DueDate < today && t.Status != "done")} overdue | " +
                   $"Critical risks: {p.Risks.Count(r => r.Status == "open" && r.Impact * r.Probability >= 12)} | " +
                   $"Open decisions: {p.Decisions.Count(d => d.Status == "open")}";
        }

        var projects = await _db.Projects.Where(p => p.TenantId == TenantId)
            .Include(p => p.Tasks).Include(p => p.Risks).ToListAsync();
        var today2 = DateOnly.FromDateTime(DateTime.UtcNow);
        return $"Portfolio: {projects.Count} projects | " +
               $"Green: {projects.Count(p => p.Status == "green")}, Yellow: {projects.Count(p => p.Status == "yellow")}, Red: {projects.Count(p => p.Status == "red")} | " +
               $"Total overdue tasks: {projects.SelectMany(p => p.Tasks).Count(t => t.DueDate < today2 && t.Status != "done")} | " +
               $"Total critical risks: {projects.SelectMany(p => p.Risks).Count(r => r.Status == "open" && r.Impact * r.Probability >= 12)}";
    }

    private static AiSuggestionDto BuildSuggestion(Project project, string type, string title, string reason, string recommendation, string priority, List<string> sources)
        => new(project.Id, project.Name, type, title, reason, recommendation, priority, sources, ResolveFeedbackStatus(project, type, title));

    private static string ResolveFeedbackStatus(Project project, string type, string title)
        => project.AiSuggestionFeedback.Where(i => i.SuggestionType == type && i.SuggestionTitle == title).OrderByDescending(i => i.UpdatedAt).Select(i => i.Status).FirstOrDefault() ?? "open";

    private static AiSuggestionFeedbackDto MapFeedback(AiSuggestionFeedback item)
        => new(item.Id, item.ProjectId, item.SuggestionType, item.SuggestionTitle, item.Status, item.Notes, item.User?.DisplayName ?? "", item.CreatedAt);

    private static List<string> BuildSuggestedActions(Project project, List<AiAnswerSourceDto> sources)
    {
        var actions = new List<string>();
        if (sources.Any(s => s.Type == "risk")) actions.Add("Kritische Risiken mit Owner im naechsten Status-Meeting durchgehen.");
        if (sources.Any(s => s.Type == "decision")) actions.Add("Offene Entscheidungen in naechstes Steering aufnehmen.");
        if (sources.Any(s => s.Type == "milestone")) actions.Add("Meilensteinplanung mit realistischem Termin absichern.");
        if (sources.Any(s => s.Type == "knowledge")) actions.Add("Knowledge-Eintraege in konkrete Tasks oder Governance-Checks ueberfuehren.");
        if (actions.Count == 0) actions.Add($"Projekt {project.Name} weiter auf Kurs halten.");
        return actions.Distinct().ToList();
    }
}

// ─── Additional DTOs for new endpoints ───────────────────────────────────────

public record NlTaskCreateRequest(Guid ProjectId, string Text, bool DryRun = false);
public record NlTaskCreateResponse(Guid? TaskId, string Title, string Description, string AssigneeName, string Priority, DateOnly DueDate, float Confidence, bool IsDryRun);
public record MeetingAgendaDto(Guid ProjectId, string ProjectName, List<AgendaItem> Items, int TotalMinutes, DateTime GeneratedAt);
public record AgendaItem(string Title, string Description, int MinutesAllocated);
public record EmailSummaryRequest(string EmailText, Guid? ProjectId = null);
public record EmailSummaryDto(string Summary, List<string> ReplyOptions, ExtractedDecision? Decision, List<string>? ActionItems);
public record ResourceOptimizationDto(List<UserAllocationSummary> Users, int OverloadedCount, int UnderloadedCount, List<string> Suggestions);
public record UserAllocationSummary(Guid UserId, string UserName, int TotalAllocatedHours, List<ProjectLoad> Projects);
public record ProjectLoad(Guid ProjectId, string ProjectName, int AllocatedHours, int OpenTaskCount, int OverdueTaskCount, string ProjectStatus);

// Internal extraction helpers
internal class MeetingAgendaResult
{
    public List<AgendaItemRaw> Items { get; set; } = [];
    public int TotalMinutes { get; set; }
}
internal class AgendaItemRaw
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int MinutesAllocated { get; set; }
}
internal class EmailAnalysisResult
{
    public string Summary { get; set; } = "";
    public List<string> ReplyOptions { get; set; } = [];
    public ExtractedDecision? Decision { get; set; }
    public List<string>? ActionItems { get; set; }
}
