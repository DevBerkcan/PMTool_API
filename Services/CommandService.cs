using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;

namespace PmTool.Api.Services;

/// <summary>
/// Command-driven AI operating system for project management.
/// Parses slash-commands (/today, /risks, /priority, etc.) and returns
/// structured CommandResult with sections, items, severity, and action buttons.
/// </summary>
public class CommandService
{
    private readonly AppDbContext _db;
    private readonly AnthropicService _ai;
    private readonly ILogger<CommandService> _log;

    public CommandService(AppDbContext db, AnthropicService ai, ILogger<CommandService> log)
    {
        _db = db; _ai = ai; _log = log;
    }

    // ─── Public entry point ───────────────────────────────────────────────────

    public async Task<CommandResult> ExecuteAsync(Guid tenantId, Guid userId, string userName, string input, Guid? contextProjectId)
    {
        var parts = input.Trim().TrimStart('/').Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts.Length > 0 ? parts[0].ToLowerInvariant() : "help";
        var args = parts.Length > 1 ? parts[1].Trim() : "";

        _log.LogInformation("Command /{Cmd} args='{Args}' user={User}", cmd, args, userName);

        return cmd switch
        {
            "today"          => await TodayAsync(tenantId, userId, userName),
            "tomorrow"       => await TomorrowAsync(tenantId),
            "nextdays"       => await NextDaysAsync(tenantId),
            "priority"       => await PriorityAsync(tenantId),
            "risks"          => await RisksAsync(tenantId),
            "meetings"       => await MeetingsAsync(tenantId),
            "projects"       => await ProjectsAsync(tenantId),
            "focus"          => await FocusAsync(tenantId),
            "summary"        => await SummaryAsync(tenantId, args, contextProjectId),
            "followups"      => await FollowupsAsync(tenantId),
            "overdue"        => await OverdueAsync(tenantId),
            "whatdidimiss"   => await WhatDidIMissAsync(tenantId, userId),
            "escalations"    => await EscalationsAsync(tenantId),
            "statusmail"     => await StatusMailAsync(tenantId, args, contextProjectId),
            "teamload"       => await TeamLoadAsync(tenantId),
            "decisionlog"    => await DecisionLogAsync(tenantId),
            "preparemeeting" => await PrepareMeetingAsync(tenantId, args, contextProjectId),
            "customerpulse"  => await CustomerPulseAsync(tenantId, args, contextProjectId),
            "weekreview"     => await WeekReviewAsync(tenantId),
            "nextbestaction" => await NextBestActionAsync(tenantId),
            "stucktasks"     => await StuckTasksAsync(tenantId),
            _                => UnknownCommand(cmd),
        };
    }

    // ─── Prioritization scoring ───────────────────────────────────────────────

    private static double PriorityScore(DateOnly? dueDate, string projectStatus, bool isUnassigned, bool projectIsCritical)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        double deadlineUrgency = dueDate == null ? 1 : dueDate < today ? 10
            : dueDate == today ? 9
            : (dueDate.Value.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).TotalDays <= 3 ? 7
            : (dueDate.Value.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).TotalDays <= 7 ? 5
            : (dueDate.Value.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).TotalDays <= 14 ? 3 : 1;

        double businessImpact = projectStatus == "red" ? 10 : projectStatus == "yellow" ? 6 : 3;
        double blockerBonus = (isUnassigned ? 2 : 0) + (projectIsCritical ? 3 : 0);

        return (deadlineUrgency * 0.4) + (businessImpact * 0.3) + (3 * 0.2) + (blockerBonus * 0.1);
    }

    private static double RiskScore(int impact, int probability) => impact * probability;

    private static string TaskSeverity(DateOnly? dueDate, string projectStatus)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dueDate.HasValue && dueDate < today) return (today.ToDateTime(TimeOnly.MinValue) - dueDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays > 14 ? "critical" : "warning";
        if (projectStatus == "red") return "warning";
        return "info";
    }

    private static string RiskSeverity(int impact, int probability)
    {
        var score = impact * probability;
        return score >= 12 ? "critical" : score >= 6 ? "warning" : "info";
    }

    private static string DaysOverdueLabel(DateOnly dueDate)
    {
        var days = (int)(DateOnly.FromDateTime(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue) - dueDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
        return days > 0 ? $"{days}d überfällig" : "heute fällig";
    }

    private static CommandAction NavAction(string label, string url) =>
        new(label, "navigate", null, null, url);
    private static CommandAction OpenProjectAction(string projectId) =>
        new("Projekt öffnen", "openProject", null, projectId, null);
    private static CommandAction ChatAction(string label, string payload) =>
        new(label, "chat", null, null, payload);

    // ─── /today ──────────────────────────────────────────────────────────────

    private async Task<CommandResult> TodayAsync(Guid tenantId, Guid userId, string userName)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        var projects = await _db.Projects
            .Where(p => p.TenantId == tenantId)
            .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
            .Include(p => p.Risks)
            .ToListAsync();

        var todayTasks = projects.SelectMany(p => p.Tasks)
            .Where(t => t.Status != "done" && t.DueDate <= today)
            .Select(t => new { task = t, project = projects.First(p => p.Id == t.ProjectId) })
            .OrderByDescending(x => PriorityScore(x.task.DueDate, x.project.Status, x.task.AssigneeId == null, x.project.Risks.Any(r => r.Impact * r.Probability >= 12)))
            .ToList();

        var top3 = todayTasks.Take(3).ToList();
        var overdueTasks = todayTasks.Where(x => x.task.DueDate < today).ToList();
        var dueTodayTasks = todayTasks.Where(x => x.task.DueDate == today).ToList();

        var meetings = await _db.ProjectMeetings
            .Where(m => _db.Projects.Any(p => p.Id == m.ProjectId && p.TenantId == tenantId)
                     && m.MeetingDate.Date == now.Date)
            .Include(m => m.Project)
            .OrderBy(m => m.MeetingDate)
            .ToListAsync();

        var sections = new List<CommandSection>();

        // Top 3 priorities
        var priorityItems = top3.Select(x => new CommandItem(
            x.task.Title,
            x.task.Description?.Length > 80 ? x.task.Description[..80] + "…" : x.task.Description,
            x.project.Name,
            x.task.Assignee?.DisplayName,
            x.task.DueDate?.ToString("dd.MM.yyyy"),
            x.task.Status,
            x.task.Priority,
            x.task.DueDate < today ? DaysOverdueLabel(x.task.DueDate!.Value) : "heute",
            TaskSeverity(x.task.DueDate, x.project.Status),
            [OpenProjectAction(x.project.Id.ToString())]
        )).ToList();

        if (priorityItems.Any())
            sections.Add(new("🎯 Top Prioritäten heute", "🎯", top3.Any(x => x.task.DueDate < today) ? "critical" : "warning", priorityItems));

        // Meetings
        if (meetings.Any())
        {
            var meetingItems = meetings.Select(m => new CommandItem(
                m.Title,
                m.MeetingDate.ToString("HH:mm") + " Uhr" + (m.Location?.Length > 0 ? $" · {m.Location}" : ""),
                m.Project?.Name,
                null, m.MeetingDate.ToString("HH:mm"), null, null, null, "info",
                [OpenProjectAction(m.ProjectId.ToString())]
            )).ToList();
            sections.Add(new("📅 Termine heute", "📅", "info", meetingItems));
        }

        // Overdue
        if (overdueTasks.Any())
        {
            var overdueItems = overdueTasks.Take(5).Select(x => new CommandItem(
                x.task.Title, null, x.project.Name,
                x.task.Assignee?.DisplayName,
                x.task.DueDate?.ToString("dd.MM.yyyy"), x.task.Status, x.task.Priority,
                DaysOverdueLabel(x.task.DueDate!.Value),
                "critical",
                [OpenProjectAction(x.project.Id.ToString())]
            )).ToList();
            sections.Add(new("⚠️ Überfällig", "⚠️", "critical", overdueItems));
        }

        // Unassigned blockers
        var unassigned = projects.SelectMany(p => p.Tasks)
            .Where(t => t.Status != "done" && t.AssigneeId == null && t.DueDate <= today.AddDays(3))
            .Take(4).ToList();
        if (unassigned.Any())
        {
            var blockerItems = unassigned.Select(t =>
            {
                var proj = projects.First(p => p.Id == t.ProjectId);
                return new CommandItem(t.Title, "Nicht zugewiesen!", proj.Name, null,
                    t.DueDate?.ToString("dd.MM.yyyy"), t.Status, t.Priority, null, "warning",
                    [OpenProjectAction(proj.Id.ToString()), ChatAction("Zuweisen", $"/createTask {t.Title}")]);
            }).ToList();
            sections.Add(new("🚫 Blocker (nicht zugewiesen)", "🚫", "warning", blockerItems));
        }

        var overallSeverity = overdueTasks.Count > 3 ? "critical" : todayTasks.Count > 0 ? "warning" : "ok";
        var summary = $"{userName}: {todayTasks.Count} offene Items heute · {meetings.Count} Meeting(s) · {overdueTasks.Count} überfällig";

        return new CommandResult("today", "", $"Ihr Tag — {today:dddd, dd. MMMM}", summary, overallSeverity, sections,
            [ChatAction("Fokus setzen", "/focus"), ChatAction("Alle Prioritäten", "/priority")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /tomorrow ───────────────────────────────────────────────────────────

    private async Task<CommandResult> TomorrowAsync(Guid tenantId)
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var tomorrowDt = DateTime.UtcNow.Date.AddDays(1);

        var projects = await _db.Projects.Where(p => p.TenantId == tenantId)
            .Include(p => p.Tasks).ThenInclude(t => t.Assignee).ToListAsync();

        var tasks = projects.SelectMany(p => p.Tasks)
            .Where(t => t.Status != "done" && t.DueDate == tomorrow)
            .Select(t => new { task = t, project = projects.First(p => p.Id == t.ProjectId) })
            .OrderBy(x => x.project.Status == "red" ? 0 : x.project.Status == "yellow" ? 1 : 2)
            .ToList();

        var meetings = await _db.ProjectMeetings
            .Where(m => _db.Projects.Any(p => p.Id == m.ProjectId && p.TenantId == tenantId)
                     && m.MeetingDate.Date == tomorrowDt)
            .Include(m => m.Project)
            .OrderBy(m => m.MeetingDate)
            .ToListAsync();

        var meetingsWithoutDocs = meetings.Where(m => m.ExtractionStatus == "none").ToList();

        var sections = new List<CommandSection>();

        if (meetings.Any())
        {
            var items = meetings.Select(m => new CommandItem(
                m.Title, m.MeetingDate.ToString("HH:mm") + " Uhr", m.Project?.Name,
                null, null, null, null,
                meetingsWithoutDocs.Contains(m) ? "Keine Vorbereitung dokumentiert" : null,
                meetingsWithoutDocs.Contains(m) ? "warning" : "info",
                [OpenProjectAction(m.ProjectId.ToString()), ChatAction("Vorbereiten", $"/preparemeeting {m.Project?.Name}")]
            )).ToList();
            sections.Add(new("📅 Morgen Meetings", "📅", meetingsWithoutDocs.Any() ? "warning" : "info", items));
        }

        if (tasks.Any())
        {
            var items = tasks.Select(x => new CommandItem(
                x.task.Title, null, x.project.Name,
                x.task.Assignee?.DisplayName, x.task.DueDate?.ToString("dd.MM.yyyy"),
                x.task.Status, x.task.Priority, null,
                x.project.Status == "red" ? "warning" : "info",
                [OpenProjectAction(x.project.Id.ToString())]
            )).ToList();
            sections.Add(new("✅ Fällige Tasks morgen", "✅", "info", items));
        }

        var summary = $"{tasks.Count} Tasks fällig · {meetings.Count} Meeting(s) geplant";
        return new CommandResult("tomorrow", "", $"Planung für morgen ({tomorrow:dd.MM.yyyy})", summary,
            meetings.Any() && meetingsWithoutDocs.Count == meetings.Count ? "warning" : "info",
            sections,
            [ChatAction("Alle nächsten Tage", "/nextdays")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /nextdays ────────────────────────────────────────────────────────────

    private async Task<CommandResult> NextDaysAsync(Guid tenantId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(7);

        var projects = await _db.Projects.Where(p => p.TenantId == tenantId)
            .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
            .Include(p => p.Milestones)
            .ToListAsync();

        var upcomingTasks = projects.SelectMany(p => p.Tasks)
            .Where(t => t.Status != "done" && t.DueDate >= today && t.DueDate <= cutoff)
            .Select(t => new { task = t, project = projects.First(p => p.Id == t.ProjectId) })
            .OrderBy(x => x.task.DueDate)
            .ToList();

        var upcomingMilestones = projects.SelectMany(p => p.Milestones)
            .Where(m => m.Status != "done" && m.DueDate >= today && m.DueDate <= cutoff)
            .Select(m => new { milestone = m, project = projects.First(p => p.Id == m.ProjectId) })
            .OrderBy(x => x.milestone.DueDate)
            .ToList();

        // Detect conflicts: >2 items same day
        var byDay = upcomingTasks.GroupBy(x => x.task.DueDate!.Value).Where(g => g.Count() > 2).ToList();

        var sections = new List<CommandSection>();

        if (upcomingTasks.Any())
        {
            var items = upcomingTasks.Take(10).Select(x => new CommandItem(
                x.task.Title, null, x.project.Name,
                x.task.Assignee?.DisplayName, x.task.DueDate?.ToString("dd.MM.yyyy"),
                x.task.Status, x.task.Priority, null,
                byDay.Any(g => g.Key == x.task.DueDate) ? "warning" : "info",
                [OpenProjectAction(x.project.Id.ToString())]
            )).ToList();
            sections.Add(new("📋 Anstehende Tasks (7 Tage)", "📋", byDay.Any() ? "warning" : "info", items));
        }

        if (upcomingMilestones.Any())
        {
            var items = upcomingMilestones.Select(x => new CommandItem(
                x.milestone.Title, x.milestone.Description, x.project.Name,
                null, x.milestone.DueDate.ToString("dd.MM.yyyy"), x.milestone.Status, null, null,
                x.project.Status == "red" ? "critical" : "warning",
                [OpenProjectAction(x.project.Id.ToString())]
            )).ToList();
            sections.Add(new("🏁 Meilensteine", "🏁", "warning", items));
        }

        if (byDay.Any())
        {
            var conflictItems = byDay.Select(g => new CommandItem(
                $"{g.Key:dd.MM.yyyy} — {g.Count()} Tasks am gleichen Tag",
                string.Join(", ", g.Select(x => x.task.Title).Take(3)),
                null, null, g.Key.ToString("dd.MM.yyyy"), null, null,
                $"{g.Count()} Konflikte", "warning", []
            )).ToList();
            sections.Add(new("⚡ Terminkonflikt-Warnung", "⚡", "warning", conflictItems));
        }

        return new CommandResult("nextdays", "", "Nächste 7 Tage",
            $"{upcomingTasks.Count} Tasks · {upcomingMilestones.Count} Meilensteine · {byDay.Count} Terminkonflikte",
            byDay.Any() ? "warning" : "info", sections,
            [ChatAction("Prioritäten", "/priority")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /priority ───────────────────────────────────────────────────────────

    private async Task<CommandResult> PriorityAsync(Guid tenantId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var projects = await _db.Projects.Where(p => p.TenantId == tenantId)
            .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
            .Include(p => p.Risks)
            .ToListAsync();

        var scored = projects.SelectMany(p => p.Tasks)
            .Where(t => t.Status != "done")
            .Select(t =>
            {
                var proj = projects.First(p => p.Id == t.ProjectId);
                var hasCriticalRisk = proj.Risks.Any(r => r.Impact * r.Probability >= 12);
                var score = PriorityScore(t.DueDate, proj.Status, t.AssigneeId == null, hasCriticalRisk);
                return new { task = t, project = proj, score };
            })
            .OrderByDescending(x => x.score)
            .Take(10)
            .ToList();

        // LLM narration for top 3
        string llmNarration = "";
        if (_ai.IsConfigured && scored.Count >= 1)
        {
            var top3ctx = string.Join("\n", scored.Take(3).Select((x, i) =>
                $"{i + 1}. [{x.project.Name}] {x.task.Title} — Due: {x.task.DueDate}, Priority: {x.task.Priority}, Project: {x.project.Status}, Score: {x.score:F1}"));
            llmNarration = await _ai.CompleteAsync(
                "Du bist ein PM-Assistent. Erkläre in 2-3 kurzen deutschen Sätzen, WARUM diese 3 Tasks jetzt die höchste Priorität haben. Sei konkret und handlungsorientiert.",
                top3ctx, 300);
        }

        var items = scored.Select((x, i) => new CommandItem(
            $"{i + 1}. {x.task.Title}",
            i < 3 && llmNarration.Length > 0 ? null : x.task.Description?.Length > 60 ? x.task.Description[..60] + "…" : x.task.Description,
            x.project.Name,
            x.task.Assignee?.DisplayName,
            x.task.DueDate?.ToString("dd.MM.yyyy"),
            x.task.Status, x.task.Priority,
            $"Score {x.score:F1}",
            x.task.DueDate < today ? "critical" : x.project.Status == "red" ? "warning" : "info",
            [OpenProjectAction(x.project.Id.ToString())]
        )).ToList();

        var sections = new List<CommandSection> { new("🏆 Top Prioritäten", "🏆", scored.Any(x => x.task.DueDate < today) ? "critical" : "warning", items) };

        return new CommandResult("priority", "", "Prioritäten über alle Projekte",
            llmNarration.Length > 0 ? llmNarration : $"{scored.Count} priorisierte Tasks",
            scored.Any(x => x.task.DueDate < today) ? "critical" : "warning",
            sections,
            [ChatAction("Fokus", "/focus"), ChatAction("Überfällige", "/overdue")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /risks ──────────────────────────────────────────────────────────────

    private async Task<CommandResult> RisksAsync(Guid tenantId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var projects = await _db.Projects.Where(p => p.TenantId == tenantId)
            .Include(p => p.Risks).ThenInclude(r => r.Owner)
            .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
            .Include(p => p.Decisions)
            .ToListAsync();

        var criticalRisks = projects.SelectMany(p => p.Risks)
            .Where(r => r.Status == "open" && r.Impact * r.Probability >= 12)
            .Select(r => new { risk = r, project = projects.First(p => p.Id == r.ProjectId) })
            .OrderByDescending(x => RiskScore(x.risk.Impact, x.risk.Probability))
            .ToList();

        var allRisks = projects.SelectMany(p => p.Risks)
            .Where(r => r.Status == "open" && r.Impact * r.Probability < 12)
            .Select(r => new { risk = r, project = projects.First(p => p.Id == r.ProjectId) })
            .OrderByDescending(x => RiskScore(x.risk.Impact, x.risk.Probability))
            .Take(5).ToList();

        var blockedTasks = projects.SelectMany(p => p.Tasks)
            .Where(t => t.Status != "done" && t.AssigneeId == null && t.DueDate <= today.AddDays(7))
            .Select(t => new { task = t, project = projects.First(p => p.Id == t.ProjectId) })
            .Take(5).ToList();

        var overdueDecisions = projects.SelectMany(p => p.Decisions)
            .Where(d => d.Status == "open" && d.DueDate < today)
            .Select(d => new { decision = d, project = projects.First(p => p.Id == d.ProjectId) })
            .Take(5).ToList();

        var sections = new List<CommandSection>();

        if (criticalRisks.Any())
        {
            var items = criticalRisks.Select(x => new CommandItem(
                x.risk.Title, x.risk.Description?.Length > 80 ? x.risk.Description[..80] + "…" : x.risk.Description,
                x.project.Name, x.risk.Owner?.DisplayName, null, x.risk.Status, null,
                $"IxW={x.risk.Impact * x.risk.Probability}", "critical",
                [OpenProjectAction(x.project.Id.ToString()), NavAction("Risikomatrix", $"/projects/{x.project.Id}/risks")]
            )).ToList();
            sections.Add(new("🔴 Kritische Risiken (IxW≥12)", "🔴", "critical", items));
        }

        if (allRisks.Any())
        {
            var items = allRisks.Select(x => new CommandItem(
                x.risk.Title, null, x.project.Name, x.risk.Owner?.DisplayName,
                null, x.risk.Status, null, $"IxW={x.risk.Impact * x.risk.Probability}",
                RiskSeverity(x.risk.Impact, x.risk.Probability),
                [OpenProjectAction(x.project.Id.ToString())]
            )).ToList();
            sections.Add(new("⚠️ Weitere offene Risiken", "⚠️", "warning", items));
        }

        if (blockedTasks.Any())
        {
            var items = blockedTasks.Select(x => new CommandItem(
                x.task.Title, "Kein Verantwortlicher zugewiesen!", x.project.Name,
                null, x.task.DueDate?.ToString("dd.MM.yyyy"), x.task.Status, x.task.Priority, null,
                "warning", [OpenProjectAction(x.project.Id.ToString())]
            )).ToList();
            sections.Add(new("🚫 Blocker — nicht zugewiesen", "🚫", "warning", items));
        }

        if (overdueDecisions.Any())
        {
            var items = overdueDecisions.Select(x => new CommandItem(
                x.decision.Title, x.decision.Context?.Length > 60 ? x.decision.Context[..60] + "…" : x.decision.Context,
                x.project.Name, null, x.decision.DueDate.ToString("dd.MM.yyyy"),
                x.decision.Status, null, DaysOverdueLabel(x.decision.DueDate), "critical",
                [OpenProjectAction(x.project.Id.ToString())]
            )).ToList();
            sections.Add(new("❓ Fehlende Entscheidungen", "❓", "critical", items));
        }

        var overallSeverity = criticalRisks.Any() ? "critical" : allRisks.Any() || blockedTasks.Any() ? "warning" : "ok";
        return new CommandResult("risks", "", "Risiko-Übersicht",
            $"{criticalRisks.Count} kritisch · {allRisks.Count} weitere · {blockedTasks.Count} Blocker · {overdueDecisions.Count} offene Entscheidungen",
            overallSeverity, sections,
            [ChatAction("Eskalationen", "/escalations")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /meetings ────────────────────────────────────────────────────────────

    private async Task<CommandResult> MeetingsAsync(Guid tenantId)
    {
        var now = DateTime.UtcNow;
        var cutoffPast = now.AddDays(-7);

        var meetings = await _db.ProjectMeetings
            .Where(m => _db.Projects.Any(p => p.Id == m.ProjectId && p.TenantId == tenantId))
            .Include(m => m.Project)
            .OrderByDescending(m => m.MeetingDate)
            .Take(30)
            .ToListAsync();

        var todayMeetings = meetings.Where(m => m.MeetingDate.Date == now.Date).OrderBy(m => m.MeetingDate).ToList();
        var undocumented = meetings.Where(m => m.MeetingDate < now && m.MeetingDate >= cutoffPast && m.ExtractionStatus == "none").ToList();

        var sections = new List<CommandSection>();

        if (todayMeetings.Any())
        {
            var items = todayMeetings.Select(m => new CommandItem(
                m.Title, m.MeetingDate.ToString("HH:mm") + " Uhr" + (m.Location?.Length > 0 ? $" · {m.Location}" : ""),
                m.Project?.Name, null, null, m.ExtractionStatus, null, null, "info",
                [OpenProjectAction(m.ProjectId.ToString()), ChatAction("Vorbereiten", $"/preparemeeting {m.Project?.Name}")]
            )).ToList();
            sections.Add(new("📅 Heute", "📅", "info", items));
        }

        if (undocumented.Any())
        {
            var items = undocumented.Select(m => new CommandItem(
                m.Title, m.MeetingDate.ToString("dd.MM.yyyy HH:mm"),
                m.Project?.Name, null, null, "Kein Extrakt", null, "Aktion erforderlich", "warning",
                [OpenProjectAction(m.ProjectId.ToString())]
            )).ToList();
            sections.Add(new("📝 Ohne Dokumentation (letzte 7 Tage)", "📝", "warning", items));
        }

        return new CommandResult("meetings", "", "Meeting-Übersicht",
            $"{todayMeetings.Count} heute · {undocumented.Count} ohne Dokumentation",
            undocumented.Any() ? "warning" : "info", sections,
            [ChatAction("Follow-ups", "/followups")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /projects ────────────────────────────────────────────────────────────

    private async Task<CommandResult> ProjectsAsync(Guid tenantId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var projects = await _db.Projects.Where(p => p.TenantId == tenantId)
            .Include(p => p.Tasks)
            .Include(p => p.Risks)
            .Include(p => p.Milestones)
            .Include(p => p.Decisions)
            .OrderBy(p => p.Status == "red" ? 0 : p.Status == "yellow" ? 1 : 2)
            .ToListAsync();

        CommandItem ProjectItem(Project p) => new(
            p.Name,
            $"{p.ProgressPercent}% · {p.Customer}",
            null, null,
            p.EndDate.ToString("dd.MM.yyyy"),
            p.Status,
            null,
            $"{p.Tasks.Count(t => t.Status != "done")} Tasks offen · {p.Risks.Count(r => r.Status == "open")} Risiken",
            p.Status == "red" ? "critical" : p.Status == "yellow" ? "warning" : "ok",
            [OpenProjectAction(p.Id.ToString())]
        );

        var critical = projects.Where(p => p.Status == "red").Select(ProjectItem).ToList();
        var warning = projects.Where(p => p.Status == "yellow").Select(ProjectItem).ToList();
        var ok = projects.Where(p => p.Status == "green").Select(ProjectItem).ToList();

        var sections = new List<CommandSection>();
        if (critical.Any()) sections.Add(new("🔴 Kritisch", "🔴", "critical", critical));
        if (warning.Any()) sections.Add(new("🟡 Warnung", "🟡", "warning", warning));
        if (ok.Any()) sections.Add(new("🟢 OK", "🟢", "ok", ok));

        return new CommandResult("projects", "", "Alle aktiven Projekte",
            $"{projects.Count} Projekte · {critical.Count} kritisch · {warning.Count} Warnung · {ok.Count} OK",
            critical.Any() ? "critical" : warning.Any() ? "warning" : "ok",
            sections,
            [ChatAction("Eskalationen", "/escalations"), ChatAction("Teamauslastung", "/teamload")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /focus ──────────────────────────────────────────────────────────────

    private async Task<CommandResult> FocusAsync(Guid tenantId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var projects = await _db.Projects.Where(p => p.TenantId == tenantId)
            .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
            .Include(p => p.Risks)
            .ToListAsync();

        var top = projects.SelectMany(p => p.Tasks)
            .Where(t => t.Status != "done")
            .Select(t =>
            {
                var proj = projects.First(p => p.Id == t.ProjectId);
                var hasCriticalRisk = proj.Risks.Any(r => r.Impact * r.Probability >= 12);
                return new { task = t, project = proj, score = PriorityScore(t.DueDate, proj.Status, t.AssigneeId == null, hasCriticalRisk) };
            })
            .OrderByDescending(x => x.score)
            .FirstOrDefault();

        if (top == null)
            return new CommandResult("focus", "", "Fokus", "Keine offenen Tasks gefunden. Gut gemacht!", "ok", [], [], DateTime.UtcNow.ToString("o"));

        string why = "";
        if (_ai.IsConfigured)
        {
            why = await _ai.CompleteAsync(
                "Du bist ein PM-Assistent. Erkläre in EINEM prägnanten deutschen Satz, warum genau dieser Task jetzt die höchste Priorität hat.",
                $"Task: {top.task.Title}\nProjekt: {top.project.Name} (Status: {top.project.Status})\nFällig: {top.task.DueDate}\nPriorität: {top.task.Priority}\nScore: {top.score:F1}",
                150);
        }

        var item = new CommandItem(
            top.task.Title,
            why.Length > 0 ? why : top.task.Description,
            top.project.Name,
            top.task.Assignee?.DisplayName,
            top.task.DueDate?.ToString("dd.MM.yyyy"),
            top.task.Status, top.task.Priority,
            $"Score {top.score:F1}",
            top.task.DueDate < today ? "critical" : "warning",
            [OpenProjectAction(top.project.Id.ToString())]
        );

        return new CommandResult("focus", "", "Ihr Fokus jetzt",
            why.Length > 0 ? why : $"Höchste Priorität: {top.task.Title} in {top.project.Name}",
            "warning",
            [new("🎯 Jetzt fokussieren", "🎯", "warning", [item])],
            [OpenProjectAction(top.project.Id.ToString()), ChatAction("Alle Prioritäten", "/priority")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /summary [project] ───────────────────────────────────────────────────

    private async Task<CommandResult> SummaryAsync(Guid tenantId, string args, Guid? contextProjectId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        Project? project;
        if (contextProjectId.HasValue)
            project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == contextProjectId && p.TenantId == tenantId);
        else if (!string.IsNullOrWhiteSpace(args))
            project = await _db.Projects.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Name.Contains(args));
        else
            project = await _db.Projects.Where(p => p.TenantId == tenantId && p.Status == "red").FirstOrDefaultAsync()
                      ?? await _db.Projects.Where(p => p.TenantId == tenantId).FirstOrDefaultAsync();

        if (project == null)
            return new CommandResult("summary", args, "Projekt nicht gefunden",
                $"Kein Projekt gefunden für '{args}'. Verfügbare Projekte mit /projects anzeigen.",
                "info", [], [], DateTime.UtcNow.ToString("o"));

        await _db.Entry(project).Collection(p => p.Tasks).Query().Include(t => t.Assignee).LoadAsync();
        await _db.Entry(project).Collection(p => p.Risks).Query().Include(r => r.Owner).LoadAsync();
        await _db.Entry(project).Collection(p => p.Decisions).Query().Include(d => d.Owner).LoadAsync();
        await _db.Entry(project).Collection(p => p.Milestones).Query().Include(m => m.Owner).LoadAsync();
        await _db.Entry(project).Collection(p => p.Notes).LoadAsync();

        var openTasks = project.Tasks.Where(t => t.Status != "done").OrderBy(t => t.DueDate).Take(5).ToList();
        var critRisks = project.Risks.Where(r => r.Status == "open" && r.Impact * r.Probability >= 12).ToList();
        var allRisks = project.Risks.Where(r => r.Status == "open").ToList();
        var openDecisions = project.Decisions.Where(d => d.Status == "open").Take(5).ToList();
        var nextMilestone = project.Milestones.Where(m => m.Status != "done" && m.DueDate >= today).OrderBy(m => m.DueDate).FirstOrDefault();

        var ctx = $"""
            Projekt: {project.Name} | Status: {project.Status} | Fortschritt: {project.ProgressPercent}%
            Kunde: {project.Customer} | Zeitraum: {project.StartDate:dd.MM.yy} – {project.EndDate:dd.MM.yy}
            Budget: {project.BudgetTotal:C0} geplant, {project.BudgetSpent:C0} verbraucht
            Offene Tasks: {openTasks.Count} | Kritische Risiken: {critRisks.Count} | Offene Entscheidungen: {openDecisions.Count}
            Nächster Meilenstein: {nextMilestone?.Title} am {nextMilestone?.DueDate}
            Top Tasks: {string.Join(", ", openTasks.Take(3).Select(t => t.Title))}
            Kritische Risiken: {string.Join(", ", critRisks.Take(2).Select(r => r.Title))}
            """;

        var llmSummary = _ai.IsConfigured
            ? await _ai.CompleteAsync("Erstelle eine prägnante Projektzusammenfassung (3-4 Sätze) auf Deutsch. Fokus: Status, wichtigste Risiken, nächste Schritte. Sei direkt und handlungsorientiert.", ctx, 400)
            : $"{project.Name}: {project.ProgressPercent}% abgeschlossen. {openTasks.Count} offene Tasks, {critRisks.Count} kritische Risiken.";

        var sections = new List<CommandSection>();

        // Status
        sections.Add(new("📊 Projektstatus", "📊", project.Status == "red" ? "critical" : project.Status == "yellow" ? "warning" : "ok",
        [new CommandItem($"{project.ProgressPercent}% abgeschlossen", $"Budget: {project.BudgetSpent:C0} / {project.BudgetTotal:C0}",
            null, null, project.EndDate.ToString("dd.MM.yyyy"), project.Status, null, null,
            project.Status == "red" ? "critical" : project.Status == "yellow" ? "warning" : "ok", [])]
        ));

        // Open tasks
        if (openTasks.Any())
        {
            var items = openTasks.Select(t => new CommandItem(t.Title, null, null,
                t.Assignee?.DisplayName, t.DueDate?.ToString("dd.MM.yyyy"), t.Status, t.Priority, null,
                t.DueDate < today ? "critical" : "info", [])).ToList();
            sections.Add(new("✅ Offene Tasks", "✅", openTasks.Any(t => t.DueDate < today) ? "warning" : "info", items));
        }

        // Critical risks
        if (critRisks.Any())
        {
            var items = critRisks.Select(r => new CommandItem(r.Title, r.Mitigation?.Length > 60 ? r.Mitigation[..60] + "…" : r.Mitigation,
                null, r.Owner?.DisplayName, null, r.Status, null, $"IxW={r.Impact * r.Probability}", "critical", [])).ToList();
            sections.Add(new("🔴 Kritische Risiken", "🔴", "critical", items));
        }

        // Open decisions
        if (openDecisions.Any())
        {
            var items = openDecisions.Select(d => new CommandItem(d.Title, d.Context?.Length > 60 ? d.Context[..60] + "…" : d.Context,
                null, d.Owner?.DisplayName, d.DueDate.ToString("dd.MM.yyyy"), d.Status, null,
                d.DueDate < today ? DaysOverdueLabel(d.DueDate) : null,
                d.DueDate < today ? "critical" : "info", [])).ToList();
            sections.Add(new("❓ Offene Entscheidungen", "❓", openDecisions.Any(d => d.DueDate < today) ? "critical" : "info", items));
        }

        return new CommandResult("summary", project.Name, $"Zusammenfassung: {project.Name}", llmSummary,
            project.Status == "red" ? "critical" : project.Status == "yellow" ? "warning" : "ok",
            sections,
            [OpenProjectAction(project.Id.ToString()), ChatAction("Status-Mail", $"/statusmail {project.Name}")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /followups ──────────────────────────────────────────────────────────

    private async Task<CommandResult> FollowupsAsync(Guid tenantId)
    {
        var cutoff = DateTime.UtcNow.AddDays(-14);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var recentMeetings = await _db.ProjectMeetings
            .Where(m => _db.Projects.Any(p => p.Id == m.ProjectId && p.TenantId == tenantId)
                     && m.MeetingDate >= cutoff && m.ExtractionStatus == "extracted")
            .Include(m => m.Project)
            .OrderByDescending(m => m.MeetingDate)
            .ToListAsync();

        var meetingIds = recentMeetings.Select(m => m.ProjectId).Distinct().ToList();
        var relatedTasks = await _db.Tasks
            .Where(t => meetingIds.Contains(t.ProjectId) && t.Status != "done" && t.CreatedAt >= cutoff)
            .Include(t => t.Assignee)
            .ToListAsync();

        var grouped = recentMeetings
            .GroupBy(m => m.ProjectId)
            .Select(g =>
            {
                var proj = g.First().Project!;
                var tasks = relatedTasks.Where(t => t.ProjectId == g.Key).ToList();
                return new { Project = proj, Meetings = g.ToList(), Tasks = tasks };
            })
            .ToList();

        var sections = grouped.Select(g =>
        {
            var items = g.Tasks.Select(t => new CommandItem(
                t.Title, $"Aus Meeting: {g.Meetings.OrderByDescending(m => m.MeetingDate).First().Title}",
                null, t.Assignee?.DisplayName, t.DueDate?.ToString("dd.MM.yyyy"),
                t.Status, t.Priority, t.DueDate < today ? DaysOverdueLabel(t.DueDate!.Value) : null,
                t.DueDate < today ? "warning" : "info",
                [OpenProjectAction(g.Project.Id.ToString())]
            )).ToList();

            if (!items.Any())
                items.Add(new CommandItem("Keine offenen Follow-up Tasks gefunden", null, null, null, null, null, null, null, "ok", []));

            return new CommandSection($"📁 {g.Project.Name}", "📁", items.Any(i => i.Severity == "warning") ? "warning" : "info", items);
        }).ToList();

        return new CommandResult("followups", "", "Offene Follow-ups aus Meetings",
            $"{recentMeetings.Count} Meetings analysiert · {relatedTasks.Count} Follow-up Tasks",
            relatedTasks.Any(t => t.DueDate < today) ? "warning" : "info",
            sections,
            [ChatAction("Alle Meetings", "/meetings")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /overdue ─────────────────────────────────────────────────────────────

    private async Task<CommandResult> OverdueAsync(Guid tenantId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var projects = await _db.Projects.Where(p => p.TenantId == tenantId)
            .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
            .Include(p => p.Decisions).ThenInclude(d => d.Owner)
            .Include(p => p.Milestones).ThenInclude(m => m.Owner)
            .ToListAsync();

        var overdueTasks = projects.SelectMany(p => p.Tasks)
            .Where(t => t.Status != "done" && t.DueDate.HasValue && t.DueDate < today)
            .Select(t => new { task = t, project = projects.First(p => p.Id == t.ProjectId) })
            .OrderBy(x => x.task.DueDate)
            .ToList();

        var overdueDecisions = projects.SelectMany(p => p.Decisions)
            .Where(d => d.Status == "open" && d.DueDate < today)
            .Select(d => new { decision = d, project = projects.First(p => p.Id == d.ProjectId) })
            .OrderBy(x => x.decision.DueDate)
            .ToList();

        var overdueMilestones = projects.SelectMany(p => p.Milestones)
            .Where(m => m.Status != "done" && m.DueDate < today)
            .Select(m => new { milestone = m, project = projects.First(p => p.Id == m.ProjectId) })
            .OrderBy(x => x.milestone.DueDate)
            .ToList();

        var sections = new List<CommandSection>();

        if (overdueTasks.Any())
        {
            var items = overdueTasks.Select(x => new CommandItem(
                x.task.Title, null, x.project.Name,
                x.task.Assignee?.DisplayName, x.task.DueDate?.ToString("dd.MM.yyyy"),
                x.task.Status, x.task.Priority, DaysOverdueLabel(x.task.DueDate!.Value),
                "critical", [OpenProjectAction(x.project.Id.ToString())]
            )).ToList();
            sections.Add(new($"📋 Überfällige Tasks ({overdueTasks.Count})", "📋", "critical", items));
        }

        if (overdueDecisions.Any())
        {
            var items = overdueDecisions.Select(x => new CommandItem(
                x.decision.Title, x.decision.Context, x.project.Name,
                x.decision.Owner?.DisplayName, x.decision.DueDate.ToString("dd.MM.yyyy"),
                x.decision.Status, null, DaysOverdueLabel(x.decision.DueDate), "critical",
                [OpenProjectAction(x.project.Id.ToString())]
            )).ToList();
            sections.Add(new($"❓ Überfällige Entscheidungen ({overdueDecisions.Count})", "❓", "critical", items));
        }

        if (overdueMilestones.Any())
        {
            var items = overdueMilestones.Select(x => new CommandItem(
                x.milestone.Title, x.milestone.Description, x.project.Name,
                x.milestone.Owner?.DisplayName, x.milestone.DueDate.ToString("dd.MM.yyyy"),
                x.milestone.Status, null, DaysOverdueLabel(x.milestone.DueDate), "critical",
                [OpenProjectAction(x.project.Id.ToString())]
            )).ToList();
            sections.Add(new($"🏁 Überfällige Meilensteine ({overdueMilestones.Count})", "🏁", "critical", items));
        }

        var total = overdueTasks.Count + overdueDecisions.Count + overdueMilestones.Count;
        return new CommandResult("overdue", "", "Alle überfälligen Items",
            $"{total} überfällige Items: {overdueTasks.Count} Tasks · {overdueDecisions.Count} Entscheidungen · {overdueMilestones.Count} Meilensteine",
            total > 0 ? "critical" : "ok", sections,
            [ChatAction("Prioritäten", "/priority"), ChatAction("Risiken", "/risks")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /whatdidimiss ────────────────────────────────────────────────────────

    private async Task<CommandResult> WhatDidIMissAsync(Guid tenantId, Guid userId)
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);

        var logs = await _db.ActivityLogs
            .Where(a => a.TenantId == tenantId && a.CreatedAt >= cutoff)
            .Include(a => a.User)
            .Include(a => a.Project)
            .OrderByDescending(a => a.CreatedAt)
            .Take(30)
            .ToListAsync();

        var grouped = logs.GroupBy(a => a.Project?.Name ?? "Allgemein")
            .Select(g => new CommandSection(
                $"📁 {g.Key}", "📁", "info",
                g.Select(a => new CommandItem(
                    a.Action, null, a.Project?.Name, a.User?.DisplayName,
                    a.CreatedAt.ToString("HH:mm"), null, null,
                    a.CreatedAt.ToString("dd.MM. HH:mm"), "info",
                    a.ProjectId.HasValue ? [OpenProjectAction(a.ProjectId.Value.ToString())] : new List<CommandAction>()
                )).ToList()
            )).ToList();

        return new CommandResult("whatdidimiss", "", "Was habe ich verpasst? (letzte 24h)",
            $"{logs.Count} Aktivitäten in {grouped.Count} Projekten",
            logs.Any() ? "info" : "ok", grouped,
            [ChatAction("Fokus", "/focus")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /escalations ─────────────────────────────────────────────────────────

    private async Task<CommandResult> EscalationsAsync(Guid tenantId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var projects = await _db.Projects.Where(p => p.TenantId == tenantId && p.Status == "red")
            .Include(p => p.Risks)
            .Include(p => p.Milestones)
            .ToListAsync();

        var critRisks = await _db.Risks
            .Where(r => _db.Projects.Any(p => p.Id == r.ProjectId && p.TenantId == tenantId)
                     && r.Status == "open" && r.Impact * r.Probability >= 12)
            .Include(r => r.Project).Include(r => r.Owner)
            .ToListAsync();

        var overdueMilestones = await _db.ProjectMilestones
            .Where(m => _db.Projects.Any(p => p.Id == m.ProjectId && p.TenantId == tenantId)
                     && m.Status != "done" && m.DueDate < today)
            .Include(m => m.Project).Include(m => m.Owner)
            .ToListAsync();

        var sections = new List<CommandSection>();

        if (projects.Any())
        {
            var items = projects.Select(p => new CommandItem(
                p.Name, $"Status Rot · {p.ProgressPercent}% Fortschritt",
                p.Customer, null, p.EndDate.ToString("dd.MM.yyyy"), "red", null, null, "critical",
                [OpenProjectAction(p.Id.ToString())]
            )).ToList();
            sections.Add(new("🔴 Kritische Projekte", "🔴", "critical", items));
        }

        if (critRisks.Any())
        {
            var items = critRisks.Select(r => new CommandItem(
                r.Title, r.Description?.Length > 80 ? r.Description[..80] + "…" : r.Description,
                r.Project?.Name, r.Owner?.DisplayName, null, r.Status, null,
                $"IxW={r.Impact * r.Probability}", "critical",
                [OpenProjectAction(r.ProjectId.ToString())]
            )).ToList();
            sections.Add(new("⚡ Eskalation-Risiken", "⚡", "critical", items));
        }

        if (overdueMilestones.Any())
        {
            var items = overdueMilestones.Select(m => new CommandItem(
                m.Title, null, m.Project?.Name, m.Owner?.DisplayName,
                m.DueDate.ToString("dd.MM.yyyy"), m.Status, null, DaysOverdueLabel(m.DueDate),
                "critical", [OpenProjectAction(m.ProjectId.ToString())]
            )).ToList();
            sections.Add(new("🏁 Überfällige Meilensteine", "🏁", "critical", items));
        }

        var total = projects.Count + critRisks.Count + overdueMilestones.Count;
        return new CommandResult("escalations", "", "Eskalations-Übersicht",
            $"{total} Eskalationspunkte für Management",
            total > 0 ? "critical" : "ok", sections,
            [ChatAction("Status-Mail erstellen", "/statusmail")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /statusmail ──────────────────────────────────────────────────────────

    private async Task<CommandResult> StatusMailAsync(Guid tenantId, string args, Guid? contextProjectId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        Project? project = contextProjectId.HasValue
            ? await _db.Projects.FirstOrDefaultAsync(p => p.Id == contextProjectId && p.TenantId == tenantId)
            : !string.IsNullOrWhiteSpace(args)
                ? await _db.Projects.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Name.Contains(args))
                : await _db.Projects.Where(p => p.TenantId == tenantId).FirstOrDefaultAsync();

        if (project == null)
            return new CommandResult("statusmail", args, "Projekt nicht gefunden",
                "Bitte Projektname angeben: /statusmail [Projektname]", "info", [], [], DateTime.UtcNow.ToString("o"));

        await _db.Entry(project).Collection(p => p.Tasks).Query().Include(t => t.Assignee).LoadAsync();
        await _db.Entry(project).Collection(p => p.Risks).Query().Include(r => r.Owner).LoadAsync();
        await _db.Entry(project).Collection(p => p.Decisions).LoadAsync();
        await _db.Entry(project).Collection(p => p.Milestones).LoadAsync();

        var openTasks = project.Tasks.Where(t => t.Status != "done").Count();
        var doneTasks = project.Tasks.Where(t => t.Status == "done").Count();
        var critRisks = project.Risks.Where(r => r.Status == "open" && r.Impact * r.Probability >= 12).ToList();
        var nextMilestone = project.Milestones.Where(m => m.Status != "done" && m.DueDate >= today).OrderBy(m => m.DueDate).FirstOrDefault();
        var overdueDecisions = project.Decisions.Where(d => d.Status == "open" && d.DueDate < today).Count();

        var ctx = $"""
            Projekt: {project.Name} | Kunde: {project.Customer}
            Status: {project.Status} | Fortschritt: {project.ProgressPercent}%
            Budget: {project.BudgetSpent:C0} von {project.BudgetTotal:C0} verbraucht
            Laufzeit: {project.StartDate:dd.MM.yyyy} – {project.EndDate:dd.MM.yyyy}
            Tasks: {doneTasks} erledigt, {openTasks} offen
            Nächster Meilenstein: {nextMilestone?.Title} am {nextMilestone?.DueDate:dd.MM.yyyy}
            Kritische Risiken: {critRisks.Count} | Offene Entscheidungen überfällig: {overdueDecisions}
            """;

        var emailBody = _ai.IsConfigured
            ? await _ai.CompleteAsync(
                $"Erstelle einen professionellen Projektstatus-E-Mail-Text auf Deutsch für Stakeholder des Projekts. Betreff + E-Mail-Body. Verwende heutige Daten ({today:dd.MM.yyyy}). Sei präzise, strukturiert, professionell. Keine internen Details, nur Management-relevante Fakten.",
                ctx, 600)
            : $"Betreff: Projektstatus {project.Name} — {today:dd.MM.yyyy}\n\nSehr geehrte Damen und Herren,\n\nhiermit informieren wir Sie über den aktuellen Stand des Projekts {project.Name}.\n\nFortschritt: {project.ProgressPercent}%\nStatus: {project.Status}\n\nMit freundlichen Grüßen";

        var item = new CommandItem(emailBody, "Zum Kopieren bereit", null, null, null, null, null, null, "info",
            [new("📋 Kopieren", "chat", null, null, emailBody)]);

        return new CommandResult("statusmail", project.Name, $"Status-Mail: {project.Name}",
            $"E-Mail-Entwurf für {project.Customer} generiert", "info",
            [new("📧 E-Mail-Entwurf", "📧", "info", [item])],
            [OpenProjectAction(project.Id.ToString())],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /teamload ────────────────────────────────────────────────────────────

    private async Task<CommandResult> TeamLoadAsync(Guid tenantId)
    {
        var allocations = await _db.ResourceAllocations
            .Where(a => _db.Projects.Any(p => p.Id == a.ProjectId && p.TenantId == tenantId))
            .Include(a => a.User)
            .Include(a => a.Project)
            .ToListAsync();

        var openTasks = await _db.Tasks
            .Where(t => _db.Projects.Any(p => p.Id == t.ProjectId && p.TenantId == tenantId) && t.Status != "done")
            .ToListAsync();

        var userGroups = allocations
            .GroupBy(a => a.UserId)
            .Select(g =>
            {
                var totalHours = g.Sum(a => a.AllocatedHours);
                var taskCount = openTasks.Count(t => t.AssigneeId == g.Key);
                var user = g.First().User;
                var severity = totalHours > 40 ? "critical" : totalHours > 30 ? "warning" : "ok";
                var projects = g.Select(a => $"{a.Project?.Name} ({a.AllocatedHours}h)");
                return new CommandItem(
                    user?.DisplayName ?? "Unbekannt",
                    string.Join(", ", projects),
                    null, null, null, null, null,
                    $"{totalHours}h allokiert · {taskCount} Tasks offen",
                    severity,
                    []
                );
            })
            .OrderByDescending(i => i.Score)
            .ToList();

        var overloaded = userGroups.Count(i => i.Severity == "critical");
        var sections = new List<CommandSection> { new("👥 Team-Auslastung", "👥", overloaded > 0 ? "critical" : "warning", userGroups) };

        return new CommandResult("teamload", "", "Team-Auslastung",
            $"{userGroups.Count} Personen · {overloaded} überlastet",
            overloaded > 0 ? "critical" : "ok", sections,
            [ChatAction("Prioritäten", "/priority")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /decisionlog ─────────────────────────────────────────────────────────

    private async Task<CommandResult> DecisionLogAsync(Guid tenantId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(-30);

        var decisions = await _db.ProjectDecisions
            .Where(d => _db.Projects.Any(p => p.Id == d.ProjectId && p.TenantId == tenantId)
                     && (d.Status == "open" || d.DueDate >= cutoff))
            .Include(d => d.Project)
            .Include(d => d.Owner)
            .OrderBy(d => d.Status == "open" ? 0 : 1)
            .ThenBy(d => d.DueDate)
            .ToListAsync();

        var open = decisions.Where(d => d.Status == "open").ToList();
        var closed = decisions.Where(d => d.Status != "open").Take(5).ToList();

        var sections = new List<CommandSection>();

        if (open.Any())
        {
            var items = open.Select(d => new CommandItem(
                d.Title, d.Context?.Length > 80 ? d.Context[..80] + "…" : d.Context,
                d.Project?.Name, d.Owner?.DisplayName, d.DueDate.ToString("dd.MM.yyyy"),
                d.Status, null, d.DueDate < today ? DaysOverdueLabel(d.DueDate) : null,
                d.DueDate < today ? "critical" : "warning",
                [OpenProjectAction(d.ProjectId.ToString())]
            )).ToList();
            sections.Add(new($"❓ Offene Entscheidungen ({open.Count})", "❓", open.Any(d => d.DueDate < today) ? "critical" : "warning", items));
        }

        if (closed.Any())
        {
            var items = closed.Select(d => new CommandItem(
                d.Title, d.Decision?.Length > 80 ? d.Decision[..80] + "…" : d.Decision,
                d.Project?.Name, d.Owner?.DisplayName, d.DueDate.ToString("dd.MM.yyyy"),
                d.Status, null, null, "ok", []
            )).ToList();
            sections.Add(new("✅ Kürzlich entschieden", "✅", "ok", items));
        }

        return new CommandResult("decisionlog", "", "Entscheidungsprotokoll",
            $"{open.Count} offen · {closed.Count} kürzlich entschieden",
            open.Any(d => d.DueDate < today) ? "critical" : open.Any() ? "warning" : "ok",
            sections, [], DateTime.UtcNow.ToString("o"));
    }

    // ─── /preparemeeting ──────────────────────────────────────────────────────

    private async Task<CommandResult> PrepareMeetingAsync(Guid tenantId, string args, Guid? contextProjectId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        Project? project = contextProjectId.HasValue
            ? await _db.Projects.FirstOrDefaultAsync(p => p.Id == contextProjectId && p.TenantId == tenantId)
            : !string.IsNullOrWhiteSpace(args)
                ? await _db.Projects.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Name.Contains(args))
                : await _db.Projects.Where(p => p.TenantId == tenantId).FirstOrDefaultAsync();

        if (project == null)
            return new CommandResult("preparemeeting", args, "Kein Projekt gefunden",
                "Angabe: /preparemeeting [Projektname]", "info", [], [], DateTime.UtcNow.ToString("o"));

        await _db.Entry(project).Collection(p => p.Tasks).Query().Include(t => t.Assignee).LoadAsync();
        await _db.Entry(project).Collection(p => p.Risks).Query().Include(r => r.Owner).LoadAsync();
        await _db.Entry(project).Collection(p => p.Decisions).LoadAsync();
        await _db.Entry(project).Collection(p => p.Notes).LoadAsync();

        var overdueTasks = project.Tasks.Where(t => t.Status != "done" && t.DueDate < today).Take(5).ToList();
        var critRisks = project.Risks.Where(r => r.Status == "open" && r.Impact * r.Probability >= 12).Take(3).ToList();
        var openDecisions = project.Decisions.Where(d => d.Status == "open").Take(5).ToList();
        var lastNote = project.Notes.OrderByDescending(n => n.CreatedAt).FirstOrDefault();

        var ctx = $"""
            Projekt: {project.Name} | Status: {project.Status}
            Überfällige Tasks: {string.Join(", ", overdueTasks.Select(t => t.Title))}
            Kritische Risiken: {string.Join(", ", critRisks.Select(r => r.Title))}
            Offene Entscheidungen: {string.Join(", ", openDecisions.Select(d => d.Title))}
            Letzte Notiz: {(lastNote?.Content?.Length > 200 ? lastNote.Content[..200] : lastNote?.Content)}
            """;

        var agenda = _ai.IsConfigured
            ? await _ai.CompleteAsync(
                "Erstelle eine strukturierte Meeting-Agenda auf Deutsch mit 4-6 Punkten. Jeder Punkt: Titel, Ziel, Zeitschätzung (Minuten). Dann 3 wichtige Gesprächspunkte ('Talking Points') die der PM ansprechen sollte.",
                ctx, 500)
            : $"Meeting-Vorbereitung {project.Name}:\n1. Status-Update ({overdueTasks.Count} überfällige Tasks)\n2. Risiken besprechen ({critRisks.Count} kritisch)\n3. Entscheidungen ({openDecisions.Count} offen)";

        var sections = new List<CommandSection>
        {
            new("📋 Agenda & Talking Points", "📋", "info", [
                new CommandItem(agenda, null, null, null, null, null, null, null, "info", [])
            ])
        };

        if (overdueTasks.Any())
        {
            var items = overdueTasks.Select(t => new CommandItem(
                t.Title, null, null, t.Assignee?.DisplayName,
                t.DueDate?.ToString("dd.MM.yyyy"), t.Status, t.Priority, DaysOverdueLabel(t.DueDate!.Value),
                "critical", [])).ToList();
            sections.Add(new("⚠️ Dringende Punkte", "⚠️", "critical", items));
        }

        return new CommandResult("preparemeeting", project.Name, $"Meeting-Vorbereitung: {project.Name}",
            $"Agenda generiert für {project.Name} · {overdueTasks.Count} dringende Punkte",
            overdueTasks.Any() ? "warning" : "info", sections,
            [OpenProjectAction(project.Id.ToString())],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /customerpulse ───────────────────────────────────────────────────────

    private async Task<CommandResult> CustomerPulseAsync(Guid tenantId, string args, Guid? contextProjectId)
    {
        Project? project = contextProjectId.HasValue
            ? await _db.Projects.FirstOrDefaultAsync(p => p.Id == contextProjectId && p.TenantId == tenantId)
            : !string.IsNullOrWhiteSpace(args)
                ? await _db.Projects.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Name.Contains(args))
                : await _db.Projects.Where(p => p.TenantId == tenantId).OrderBy(p => p.Status == "red" ? 0 : 1).FirstOrDefaultAsync();

        if (project == null)
            return new CommandResult("customerpulse", args, "Kein Projekt gefunden",
                "Angabe: /customerpulse [Projektname]", "info", [], [], DateTime.UtcNow.ToString("o"));

        await _db.Entry(project).Collection(p => p.Notes).LoadAsync();
        await _db.Entry(project).Collection(p => p.Decisions).LoadAsync();
        await _db.Entry(project).Collection(p => p.Contacts).LoadAsync();

        var recentNotes = project.Notes.OrderByDescending(n => n.CreatedAt).Take(3).ToList();
        var openDecisions = project.Decisions.Where(d => d.Status == "open").Take(3).ToList();

        var ctx = $"""
            Projekt: {project.Name} | Kunde: {project.Customer}
            Status: {project.Status} | Fortschritt: {project.ProgressPercent}%
            Laufzeit: {project.StartDate:dd.MM.yyyy} – {project.EndDate:dd.MM.yyyy}
            Budget: {project.BudgetSpent:C0} / {project.BudgetTotal:C0}
            Notizen: {string.Join("; ", recentNotes.Select(n => n.Content?.Length > 100 ? n.Content[..100] : n.Content))}
            Offene Entscheidungen: {string.Join(", ", openDecisions.Select(d => d.Title))}
            """;

        var pulse = _ai.IsConfigured
            ? await _ai.CompleteAsync(
                "Erstelle einen kundenorientierten Projektstatus-Snapshot auf Deutsch. Keine internen Details. Professionell, positiv-realistisch. Max 150 Wörter. Enthält: aktueller Stand, nächste Schritte, offene Punkte die der Kunde entscheiden muss.",
                ctx, 350)
            : $"Projekt {project.Name} ist zu {project.ProgressPercent}% abgeschlossen.";

        return new CommandResult("customerpulse", project.Name, $"Kundenstatus: {project.Customer}",
            pulse, project.Status == "red" ? "warning" : "ok",
            [new("📊 Kundenstatus-Snapshot", "📊", "info", [new CommandItem(pulse, null, project.Customer, null, null, null, null, null, "info", [])])],
            [OpenProjectAction(project.Id.ToString()), ChatAction("Status-Mail", $"/statusmail {project.Name}")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /weekreview ──────────────────────────────────────────────────────────

    private async Task<CommandResult> WeekReviewAsync(Guid tenantId)
    {
        var monday = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + (int)DayOfWeek.Monday);
        if (monday > DateTime.UtcNow.Date) monday = monday.AddDays(-7);

        var doneTasks = await _db.Tasks
            .Where(t => _db.Projects.Any(p => p.Id == t.ProjectId && p.TenantId == tenantId)
                     && t.Status == "done" && t.UpdatedAt >= monday)
            .Include(t => t.Assignee)
            .Include(t => t.Project)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync();

        var logs = await _db.ActivityLogs
            .Where(a => a.TenantId == tenantId && a.CreatedAt >= monday)
            .Include(a => a.Project)
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .ToListAsync();

        var grouped = doneTasks.GroupBy(t => t.Project?.Name ?? "Unbekannt")
            .Select(g => new CommandSection(
                $"✅ {g.Key}", "✅", "ok",
                g.Select(t => new CommandItem(
                    t.Title, null, null, t.Assignee?.DisplayName,
                    t.UpdatedAt.ToString("dd.MM."), null, null, null, "ok", []
                )).ToList()
            )).ToList();

        return new CommandResult("weekreview", "", $"Wochenrückblick — KW {GetWeekNumber(DateTime.UtcNow)}",
            $"{doneTasks.Count} Tasks erledigt · {logs.Count} Aktivitäten diese Woche",
            "ok", grouped,
            [ChatAction("Nächste Woche planen", "/nextdays")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /nextbestaction ──────────────────────────────────────────────────────

    private async Task<CommandResult> NextBestActionAsync(Guid tenantId)
    {
        var result = await FocusAsync(tenantId);
        var topItem = result.Sections.FirstOrDefault()?.Items.FirstOrDefault();
        if (topItem == null)
            return new CommandResult("nextbestaction", "", "Nächste beste Aktion",
                "Keine offenen Items gefunden.", "ok", [], [], DateTime.UtcNow.ToString("o"));

        string impact = "";
        if (_ai.IsConfigured)
            impact = await _ai.CompleteAsync(
                "Erkläre in EINEM deutschen Satz, welchen konkreten Geschäftsnutzen es hat, DIESE eine Aufgabe als nächstes zu erledigen.",
                $"Aufgabe: {topItem.Title}\nProjekt: {topItem.ProjectName}\nPriorität: {topItem.Score}",
                120);

        return new CommandResult("nextbestaction", "", "Ihre nächste beste Aktion",
            impact.Length > 0 ? impact : topItem.Title,
            "warning",
            [new("⚡ Jetzt handeln", "⚡", "warning", [topItem with { Detail = impact }])],
            result.SuggestedActions,
            DateTime.UtcNow.ToString("o"));
    }

    // ─── /stucktasks ──────────────────────────────────────────────────────────

    private async Task<CommandResult> StuckTasksAsync(Guid tenantId)
    {
        var cutoff = DateTime.UtcNow.AddDays(-5);

        var stuck = await _db.Tasks
            .Where(t => _db.Projects.Any(p => p.Id == t.ProjectId && p.TenantId == tenantId)
                     && t.Status != "done" && t.UpdatedAt < cutoff)
            .Include(t => t.Assignee)
            .Include(t => t.Project)
            .OrderBy(t => t.UpdatedAt)
            .ToListAsync();

        var sections = new List<CommandSection>();

        if (stuck.Any())
        {
            var items = stuck.Select(t =>
            {
                var daysSince = (int)(DateTime.UtcNow - t.UpdatedAt).TotalDays;
                return new CommandItem(
                    t.Title, $"Keine Änderung seit {daysSince} Tagen",
                    t.Project?.Name, t.Assignee?.DisplayName,
                    t.DueDate?.ToString("dd.MM.yyyy"), t.Status, t.Priority,
                    $"{daysSince}d keine Bewegung",
                    daysSince > 14 ? "critical" : "warning",
                    [OpenProjectAction(t.ProjectId.ToString())]
                );
            }).ToList();
            sections.Add(new($"🧱 Stalled Tasks ({stuck.Count})", "🧱", stuck.Any(t => (DateTime.UtcNow - t.UpdatedAt).TotalDays > 14) ? "critical" : "warning", items));
        }

        return new CommandResult("stucktasks", "", "Tasks ohne Fortschritt (5+ Tage)",
            stuck.Any() ? $"{stuck.Count} Tasks blockiert oder vergessen" : "Alle Tasks werden aktiv bearbeitet",
            stuck.Count > 5 ? "critical" : stuck.Any() ? "warning" : "ok",
            sections,
            [ChatAction("Prioritäten", "/priority"), ChatAction("Risiken", "/risks")],
            DateTime.UtcNow.ToString("o"));
    }

    // ─── Unknown command ──────────────────────────────────────────────────────

    private static CommandResult UnknownCommand(string cmd)
    {
        var available = new[]
        {
            "/today", "/tomorrow", "/nextdays", "/priority", "/risks", "/meetings", "/projects",
            "/focus", "/summary [Projekt]", "/followups", "/overdue", "/whatdidimiss",
            "/escalations", "/statusmail [Projekt]", "/teamload", "/decisionlog",
            "/preparemeeting [Projekt]", "/customerpulse [Projekt]", "/weekreview",
            "/nextbestaction", "/stucktasks"
        };
        return new CommandResult(cmd, "", $"Unbekannter Befehl: /{cmd}",
            "Verfügbare Befehle: " + string.Join(" · ", available),
            "info",
            [new CommandSection("📖 Verfügbare Befehle", "📖", "info",
                available.Select(c => new CommandItem(c, null, null, null, null, null, null, null, "info", [])).ToList())],
            [],
            DateTime.UtcNow.ToString("o"));
    }

    private static int GetWeekNumber(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        return cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }
}
