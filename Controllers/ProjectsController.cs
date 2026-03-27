using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using PmTool.Api.Security;
using System.Security.Claims;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/[controller]"), Authorize]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? "";

    public ProjectsController(AppDbContext db) => _db = db;

    [HttpGet("portfolio")]
    public async Task<ActionResult<PortfolioDto>> GetPortfolio()
    {
        var projects = await _db.Projects
            .Where(p => p.TenantId == TenantId)
            .Include(p => p.Tasks)
            .Include(p => p.Milestones)
            .Include(p => p.Decisions)
            .Include(p => p.GovernanceChecks)
            .Include(p => p.Owner)
            .Include(p => p.TeamAssignments)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var allTasks = projects.SelectMany(p => p.Tasks).ToList();
        var capacityLoads = projects
            .SelectMany(p => p.TeamAssignments)
            .GroupBy(a => a.UserId)
            .Select(group => group.Sum(item => item.AllocatedHours))
            .ToList();
        var weightedForecasts = projects
            .Where(project => project.ProgressPercent > 0 && project.BudgetSpent > 0)
            .Select(project => project.BudgetSpent / Math.Max(project.ProgressPercent, 1) * 100)
            .ToList();
        var forecastBudget = weightedForecasts.Count > 0 ? weightedForecasts.Sum() : projects.Sum(project => project.BudgetTotal);

        return Ok(new PortfolioDto(
            projects.Select(MapProject).ToList(),
            projects.Count,
            projects.Count(p => p.Status == "green"),
            projects.Count(p => p.Status == "yellow"),
            projects.Count(p => p.Status == "red"),
            projects.Sum(p => p.BudgetTotal),
            projects.Sum(p => p.BudgetSpent),
            forecastBudget,
            forecastBudget - projects.Sum(p => p.BudgetTotal),
            allTasks.Count(t => t.Status != "done"),
            allTasks.Count(t => t.DueDate.HasValue && t.DueDate < today && t.Status != "done"),
            projects.SelectMany(p => p.Decisions).Count(d => d.Status == "open" || d.Status == "review"),
            projects.SelectMany(p => p.Milestones).Count(m => m.DueDate < today && m.Status != "done"),
            projects.SelectMany(p => p.GovernanceChecks).Count(g => g.Status != "done"),
            capacityLoads.Count(load => load > 40),
            capacityLoads.Count(load => load >= 36 && load <= 40)
        ));
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetAll([FromQuery] string? status, [FromQuery] string? customer)
    {
        var q = _db.Projects
            .Where(p => p.TenantId == TenantId)
            .Include(p => p.Owner)
            .Include(p => p.TeamAssignments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(customer)) q = q.Where(p => p.Customer.Contains(customer));

        return Ok((await q.OrderBy(p => p.Name).ToListAsync()).Select(MapProject));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectDetailDto>> GetById(Guid id)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .Include(p => p.Tasks)
            .Include(p => p.Risks)
            .Include(p => p.TeamAssignments).ThenInclude(a => a.User)
            .Include(p => p.Notes).ThenInclude(n => n.Author)
            .Include(p => p.LeadTasks).ThenInclude(t => t.Owner)
            .Include(p => p.Milestones).ThenInclude(t => t.Owner)
            .Include(p => p.Decisions).ThenInclude(t => t.Owner)
            .Include(p => p.Documents).ThenInclude(t => t.Owner)
            .Include(p => p.GovernanceChecks).ThenInclude(t => t.Owner)
            .Include(p => p.StageGates).ThenInclude(g => g.Owner)
            .Include(p => p.StageGates).ThenInclude(g => g.Checks)
            .Include(p => p.Approvals).ThenInclude(a => a.RequestedBy)
            .Include(p => p.Approvals).ThenInclude(a => a.DecidedBy)
            .Include(p => p.KnowledgeItems).ThenInclude(t => t.Author)
            .Include(p => p.TeamsLink)
            .Include(p => p.JiraLink)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);

        return project == null ? NotFound() : Ok(MapProjectDetail(project));
    }

    [HttpGet("{id}/forecast")]
    public async Task<ActionResult<ProjectForecastDto>> GetForecast(Guid id)
    {
        var project = await _db.Projects
            .Where(p => p.Id == id && p.TenantId == TenantId)
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync();

        if (project == null) return NotFound();

        var totalEstimatedHours = project.Tasks.Sum(task => task.EstimatedHours);
        var loggedHours = project.Tasks.Sum(task => task.LoggedHours);
        var remainingHours = Math.Max(totalEstimatedHours - loggedHours, 0);

        var budgetAtCompletion = project.BudgetTotal;
        var actualCost = project.BudgetSpent;
        var earnedValue = budgetAtCompletion * project.ProgressPercent / 100m;

        var totalDurationDays = Math.Max((project.EndDate.ToDateTime(TimeOnly.MinValue) - project.StartDate.ToDateTime(TimeOnly.MinValue)).Days, 1);
        var elapsedDays = Math.Clamp((DateTime.UtcNow.Date - project.StartDate.ToDateTime(TimeOnly.MinValue)).Days, 0, totalDurationDays);
        var plannedProgressPercent = Math.Clamp((decimal)elapsedDays / totalDurationDays * 100m, 0m, 100m);
        var plannedValue = budgetAtCompletion * plannedProgressPercent / 100m;

        var costVariance = earnedValue - actualCost;
        var scheduleVariance = earnedValue - plannedValue;
        var costPerformanceIndex = actualCost > 0 ? earnedValue / actualCost : 1m;
        var schedulePerformanceIndex = plannedValue > 0 ? earnedValue / plannedValue : 1m;
        var estimateAtCompletion = costPerformanceIndex > 0 ? budgetAtCompletion / costPerformanceIndex : budgetAtCompletion;
        var estimateToComplete = Math.Max(estimateAtCompletion - actualCost, 0);

        var forecastComment = costPerformanceIndex < 0.9m
            ? "Kostenverlauf ist kritisch. Budgetverbrauch liegt ueber dem erzielten Fortschritt."
            : schedulePerformanceIndex < 0.9m
                ? "Projekt liegt hinter dem Plan. Meilensteine und Restaufwand muessen neu bewertet werden."
                : "Projekt bewegt sich im erwarteten Kosten- und Terminrahmen.";

        var snapshotDate = GetWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var existingSnapshot = await _db.ProjectForecastSnapshots
            .FirstOrDefaultAsync(snapshot => snapshot.ProjectId == project.Id && snapshot.SnapshotDate == snapshotDate);

        if (existingSnapshot == null)
        {
            _db.ProjectForecastSnapshots.Add(new ProjectForecastSnapshot
            {
                ProjectId = project.Id,
                SnapshotDate = snapshotDate,
                BudgetAtCompletion = budgetAtCompletion,
                ActualCost = actualCost,
                EarnedValue = earnedValue,
                PlannedValue = plannedValue,
                EstimateAtCompletion = estimateAtCompletion,
                EstimateToComplete = estimateToComplete,
                CostPerformanceIndex = Math.Round(costPerformanceIndex, 4),
                SchedulePerformanceIndex = Math.Round(schedulePerformanceIndex, 4),
                TotalEstimatedHours = totalEstimatedHours,
                LoggedHours = loggedHours,
                RemainingHours = remainingHours
            });
            await _db.SaveChangesAsync();
        }

        return Ok(new ProjectForecastDto(
            project.Id,
            project.Name,
            budgetAtCompletion,
            actualCost,
            earnedValue,
            plannedValue,
            costVariance,
            scheduleVariance,
            estimateAtCompletion,
            estimateToComplete,
            Math.Round(costPerformanceIndex, 2),
            Math.Round(schedulePerformanceIndex, 2),
            totalEstimatedHours,
            loggedHours,
            remainingHours,
            forecastComment
        ));
    }

    [HttpGet("{id}/forecast/snapshots")]
    public async Task<ActionResult<List<ProjectForecastSnapshotDto>>> GetForecastSnapshots(Guid id)
    {
        var projectExists = await _db.Projects.AnyAsync(project => project.Id == id && project.TenantId == TenantId);
        if (!projectExists) return NotFound();

        var snapshots = await _db.ProjectForecastSnapshots
            .Where(snapshot => snapshot.ProjectId == id)
            .OrderByDescending(snapshot => snapshot.SnapshotDate)
            .Take(12)
            .ToListAsync();

        return Ok(snapshots.Select(snapshot => new ProjectForecastSnapshotDto(
            snapshot.Id,
            snapshot.SnapshotDate,
            snapshot.BudgetAtCompletion,
            snapshot.ActualCost,
            snapshot.EarnedValue,
            snapshot.PlannedValue,
            snapshot.EstimateAtCompletion,
            snapshot.EstimateToComplete,
            snapshot.CostPerformanceIndex,
            snapshot.SchedulePerformanceIndex,
            snapshot.RemainingHours
        )));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectRequest req)
    {
        if (!RoleAccess.CanManagePortfolio(CurrentRole)) return Forbid();

        var project = new Project
        {
            TenantId = TenantId,
            OwnerId = UserId,
            Name = req.Name,
            Description = req.Description,
            Customer = req.Customer,
            BudgetTotal = req.BudgetTotal,
            StartDate = req.StartDate,
            EndDate = req.EndDate
        };

        _db.Projects.Add(project);
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = project.Id, EntityId = project.Id, EntityType = "Project", Action = "hat Projekt erstellt" });
        await _db.SaveChangesAsync();

        var owner = await _db.Users.FindAsync(UserId);
        project.Owner = owner;
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, MapProject(project));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest req)
    {
        if (!RoleAccess.CanEditProject(CurrentRole)) return Forbid();

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (project == null) return NotFound();

        if (req.Name != null) project.Name = req.Name;
        if (req.Description != null) project.Description = req.Description;
        if (req.Customer != null) project.Customer = req.Customer;
        if (req.Category != null) project.Category = req.Category;
        if (req.Stage != null) project.Stage = req.Stage;
        if (req.DeliveryModel != null) project.DeliveryModel = req.DeliveryModel;
        if (req.Sponsor != null) project.Sponsor = req.Sponsor;
        if (req.ExecutiveSummary != null) project.ExecutiveSummary = req.ExecutiveSummary;
        if (req.HealthSummary != null) project.HealthSummary = req.HealthSummary;
        if (req.Objective != null) project.Objective = req.Objective;
        if (req.Scope != null) project.Scope = req.Scope;
        if (req.SuccessMetric != null) project.SuccessMetric = req.SuccessMetric;
        if (req.Communication != null) project.Communication = req.Communication;
        if (req.NextMilestone != null) project.NextMilestone = req.NextMilestone;
        if (req.Stakeholders != null) project.StakeholdersCsv = string.Join("|", req.Stakeholders.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()));
        if (req.Technologies != null) project.TechnologiesCsv = string.Join("|", req.Technologies.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()));
        if (req.Status != null) project.Status = req.Status;
        if (req.ProgressPercent.HasValue) project.ProgressPercent = req.ProgressPercent.Value;
        if (req.BudgetTotal.HasValue) project.BudgetTotal = req.BudgetTotal.Value;
        if (req.BudgetSpent.HasValue) project.BudgetSpent = req.BudgetSpent.Value;
        if (req.StartDate.HasValue) project.StartDate = req.StartDate.Value;
        if (req.EndDate.HasValue) project.EndDate = req.EndDate.Value;

        project.UpdatedAt = DateTime.UtcNow;
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = project.Id, EntityId = project.Id, EntityType = "Project", Action = "hat Projekt aktualisiert" });
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!RoleAccess.CanManagePortfolio(CurrentRole)) return Forbid();

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (project == null) return NotFound();
        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/team")]
    public async Task<ActionResult<List<ProjectTeamMemberDto>>> GetTeam(Guid id)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var team = await _db.ResourceAllocations
            .Where(a => a.ProjectId == id)
            .Include(a => a.User)
            .OrderBy(a => a.User!.DisplayName)
            .ToListAsync();

        return Ok(team.Select(MapTeamMember));
    }

    [HttpPost("{id}/team")]
    public async Task<ActionResult<ProjectTeamMemberDto>> AddTeamMember(Guid id, [FromBody] AssignProjectTeamMemberRequest req)
    {
        if (!RoleAccess.CanManageTeam(CurrentRole)) return Forbid();

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId && u.TenantId == TenantId);
        if (user == null) return BadRequest(new { message = "Teammitglied nicht gefunden." });

        var existing = await _db.ResourceAllocations.FirstOrDefaultAsync(a => a.ProjectId == id && a.UserId == req.UserId);
        if (existing != null) return Conflict(new { message = "Mitglied ist bereits dem Projekt zugeordnet." });

        var allocation = new ResourceAllocation
        {
            ProjectId = id,
            UserId = req.UserId,
            AllocatedHours = req.AllocatedHours,
            ProjectRole = req.ProjectRole,
            Responsibility = req.Responsibility
        };

        _db.ResourceAllocations.Add(allocation);
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = id, EntityId = allocation.Id, EntityType = "ProjectTeam", Action = $"hat {user.DisplayName} dem Projekt hinzugefuegt" });
        await _db.SaveChangesAsync();

        allocation.User = user;
        return Ok(MapTeamMember(allocation));
    }

    [HttpPut("{id}/team/{userId}")]
    public async Task<IActionResult> UpdateTeamMember(Guid id, Guid userId, [FromBody] UpdateProjectTeamMemberRequest req)
    {
        if (!RoleAccess.CanManageTeam(CurrentRole)) return Forbid();

        var allocation = await _db.ResourceAllocations
            .Include(a => a.Project)
            .FirstOrDefaultAsync(a => a.ProjectId == id && a.UserId == userId && a.Project!.TenantId == TenantId);

        if (allocation == null) return NotFound();

        if (req.ProjectRole != null) allocation.ProjectRole = req.ProjectRole;
        if (req.Responsibility != null) allocation.Responsibility = req.Responsibility;
        if (req.AllocatedHours.HasValue) allocation.AllocatedHours = req.AllocatedHours.Value;
        allocation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}/team/{userId}")]
    public async Task<IActionResult> RemoveTeamMember(Guid id, Guid userId)
    {
        if (!RoleAccess.CanManageTeam(CurrentRole)) return Forbid();

        var allocation = await _db.ResourceAllocations
            .Include(a => a.Project)
            .FirstOrDefaultAsync(a => a.ProjectId == id && a.UserId == userId && a.Project!.TenantId == TenantId);

        if (allocation == null) return NotFound();
        _db.ResourceAllocations.Remove(allocation);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/notes")]
    public async Task<ActionResult<List<ProjectNoteDto>>> GetNotes(Guid id)
    {
        var notes = await _db.ProjectNotes
            .Where(n => n.ProjectId == id)
            .Include(n => n.Author)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Ok(notes.Select(MapNote));
    }

    [HttpPost("{id}/notes")]
    public async Task<ActionResult<ProjectNoteDto>> AddNote(Guid id, [FromBody] CreateProjectNoteRequest req)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var note = new ProjectNote
        {
            ProjectId = id,
            AuthorId = UserId,
            Title = req.Title,
            Content = req.Content
        };

        _db.ProjectNotes.Add(note);
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = id, EntityId = note.Id, EntityType = "ProjectNote", Action = $"hat Notiz erstellt: {note.Title}" });
        await _db.SaveChangesAsync();

        var author = await _db.Users.FindAsync(UserId);
        note.Author = author;
        return Ok(MapNote(note));
    }

    [HttpGet("{id}/lead-tasks")]
    public async Task<ActionResult<List<ProjectLeadTaskDto>>> GetLeadTasks(Guid id)
    {
        var tasks = await _db.ProjectLeadTasks
            .Where(t => t.ProjectId == id)
            .Include(t => t.Owner)
            .OrderBy(t => t.DueDate)
            .ToListAsync();

        return Ok(tasks.Select(MapLeadTask));
    }

    [HttpPost("{id}/lead-tasks")]
    public async Task<ActionResult<ProjectLeadTaskDto>> AddLeadTask(Guid id, [FromBody] CreateProjectLeadTaskRequest req)
    {
        if (!RoleAccess.CanEditProject(CurrentRole)) return Forbid();

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var ownerId = req.OwnerId ?? UserId;
        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == ownerId && u.TenantId == TenantId);
        if (owner == null) return BadRequest(new { message = "Owner nicht gefunden." });

        var task = new ProjectLeadTask
        {
            ProjectId = id,
            OwnerId = ownerId,
            Title = req.Title,
            Description = req.Description,
            DueDate = req.DueDate,
            Status = "todo"
        };

        _db.ProjectLeadTasks.Add(task);
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = id, EntityId = task.Id, EntityType = "ProjectLeadTask", Action = $"hat Projektleiter-Aufgabe erstellt: {task.Title}" });
        await _db.SaveChangesAsync();

        task.Owner = owner;
        return Ok(MapLeadTask(task));
    }

    [HttpPatch("{id}/lead-tasks/{taskId}/status")]
    public async Task<IActionResult> UpdateLeadTaskStatus(Guid id, Guid taskId, [FromBody] UpdateProjectLeadTaskStatusRequest req)
    {
        if (!RoleAccess.CanEditProject(CurrentRole)) return Forbid();

        var task = await _db.ProjectLeadTasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == id && t.Project!.TenantId == TenantId);

        if (task == null) return NotFound();
        task.Status = req.Status;
        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/milestones")]
    public async Task<ActionResult<List<ProjectMilestoneDto>>> GetMilestones(Guid id)
    {
        var milestones = await _db.ProjectMilestones
            .Where(t => t.ProjectId == id)
            .Include(t => t.Owner)
            .OrderBy(t => t.DueDate)
            .ToListAsync();

        return Ok(milestones.Select(MapMilestone));
    }

    [HttpPost("{id}/milestones")]
    public async Task<ActionResult<ProjectMilestoneDto>> AddMilestone(Guid id, [FromBody] CreateProjectMilestoneRequest req)
    {
        if (!RoleAccess.CanEditProject(CurrentRole)) return Forbid();

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var ownerId = req.OwnerId ?? UserId;
        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == ownerId && u.TenantId == TenantId);
        if (owner == null) return BadRequest(new { message = "Owner nicht gefunden." });

        var milestone = new ProjectMilestone
        {
            ProjectId = id,
            OwnerId = ownerId,
            Title = req.Title,
            Description = req.Description,
            DueDate = req.DueDate,
            Status = "planned"
        };

        _db.ProjectMilestones.Add(milestone);
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = id, EntityId = milestone.Id, EntityType = "ProjectMilestone", Action = $"hat Meilenstein erstellt: {milestone.Title}" });
        await _db.SaveChangesAsync();

        milestone.Owner = owner;
        return Ok(MapMilestone(milestone));
    }

    [HttpPatch("{id}/milestones/{milestoneId}/status")]
    public async Task<IActionResult> UpdateMilestoneStatus(Guid id, Guid milestoneId, [FromBody] UpdateProjectMilestoneStatusRequest req)
    {
        if (!RoleAccess.CanEditProject(CurrentRole)) return Forbid();

        var milestone = await _db.ProjectMilestones
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == milestoneId && t.ProjectId == id && t.Project!.TenantId == TenantId);

        if (milestone == null) return NotFound();
        milestone.Status = req.Status;
        milestone.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/decisions")]
    public async Task<ActionResult<List<ProjectDecisionDto>>> GetDecisions(Guid id)
    {
        var decisions = await _db.ProjectDecisions
            .Where(t => t.ProjectId == id)
            .Include(t => t.Owner)
            .OrderBy(t => t.DueDate)
            .ToListAsync();

        return Ok(decisions.Select(MapDecision));
    }

    [HttpPost("{id}/decisions")]
    public async Task<ActionResult<ProjectDecisionDto>> AddDecision(Guid id, [FromBody] CreateProjectDecisionRequest req)
    {
        if (!RoleAccess.CanEditProject(CurrentRole)) return Forbid();

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var ownerId = req.OwnerId ?? UserId;
        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == ownerId && u.TenantId == TenantId);
        if (owner == null) return BadRequest(new { message = "Owner nicht gefunden." });

        var decision = new ProjectDecision
        {
            ProjectId = id,
            OwnerId = ownerId,
            Title = req.Title,
            Context = req.Context,
            Decision = req.Decision,
            DueDate = req.DueDate,
            Status = "open"
        };

        _db.ProjectDecisions.Add(decision);
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = id, EntityId = decision.Id, EntityType = "ProjectDecision", Action = $"hat Entscheidung erstellt: {decision.Title}" });
        await _db.SaveChangesAsync();

        decision.Owner = owner;
        return Ok(MapDecision(decision));
    }

    [HttpPatch("{id}/decisions/{decisionId}/status")]
    public async Task<IActionResult> UpdateDecisionStatus(Guid id, Guid decisionId, [FromBody] UpdateProjectDecisionStatusRequest req)
    {
        if (!RoleAccess.CanEditProject(CurrentRole)) return Forbid();

        var decision = await _db.ProjectDecisions
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == decisionId && t.ProjectId == id && t.Project!.TenantId == TenantId);

        if (decision == null) return NotFound();
        decision.Status = req.Status;
        decision.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/documents")]
    public async Task<ActionResult<List<ProjectDocumentDto>>> GetDocuments(Guid id)
    {
        var documents = await _db.ProjectDocuments
            .Where(t => t.ProjectId == id)
            .Include(t => t.Owner)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(documents.Select(MapDocument));
    }

    [HttpPost("{id}/documents")]
    public async Task<ActionResult<ProjectDocumentDto>> AddDocument(Guid id, [FromBody] CreateProjectDocumentRequest req)
    {
        if (!RoleAccess.CanEditProject(CurrentRole)) return Forbid();

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var ownerId = req.OwnerId ?? UserId;
        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == ownerId && u.TenantId == TenantId);
        if (owner == null) return BadRequest(new { message = "Owner nicht gefunden." });

        var document = new ProjectDocument
        {
            ProjectId = id,
            OwnerId = ownerId,
            Title = req.Title,
            Category = req.Category,
            Url = req.Url,
            Status = req.Status
        };

        _db.ProjectDocuments.Add(document);
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = id, EntityId = document.Id, EntityType = "ProjectDocument", Action = $"hat Dokument erstellt: {document.Title}" });
        await _db.SaveChangesAsync();

        document.Owner = owner;
        return Ok(MapDocument(document));
    }

    [HttpGet("{id}/governance-checks")]
    public async Task<ActionResult<List<ProjectGovernanceCheckDto>>> GetGovernanceChecks(Guid id)
    {
        var checks = await _db.ProjectGovernanceChecks
            .Where(t => t.ProjectId == id)
            .Include(t => t.Owner)
            .OrderBy(t => t.DueDate)
            .ToListAsync();

        return Ok(checks.Select(MapGovernanceCheck));
    }

    [HttpPost("{id}/governance-checks")]
    public async Task<ActionResult<ProjectGovernanceCheckDto>> AddGovernanceCheck(Guid id, [FromBody] CreateProjectGovernanceCheckRequest req)
    {
        if (!RoleAccess.CanManagePmo(CurrentRole)) return Forbid();

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var ownerId = req.OwnerId ?? UserId;
        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == ownerId && u.TenantId == TenantId);
        if (owner == null) return BadRequest(new { message = "Owner nicht gefunden." });

        var check = new ProjectGovernanceCheck
        {
            ProjectId = id,
            OwnerId = ownerId,
            Title = req.Title,
            Area = req.Area,
            Notes = req.Notes,
            DueDate = req.DueDate,
            Status = "open"
        };

        _db.ProjectGovernanceChecks.Add(check);
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = id, EntityId = check.Id, EntityType = "ProjectGovernanceCheck", Action = $"hat Governance-Check erstellt: {check.Title}" });
        await _db.SaveChangesAsync();

        check.Owner = owner;
        return Ok(MapGovernanceCheck(check));
    }

    [HttpPatch("{id}/governance-checks/{checkId}/status")]
    public async Task<IActionResult> UpdateGovernanceCheckStatus(Guid id, Guid checkId, [FromBody] UpdateProjectGovernanceCheckStatusRequest req)
    {
        if (!RoleAccess.CanManagePmo(CurrentRole)) return Forbid();

        var check = await _db.ProjectGovernanceChecks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == checkId && t.ProjectId == id && t.Project!.TenantId == TenantId);

        if (check == null) return NotFound();
        check.Status = req.Status;
        check.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/stage-gates")]
    public async Task<ActionResult<List<ProjectStageGateDto>>> GetStageGates(Guid id)
    {
        var gates = await _db.ProjectStageGates
            .Where(gate => gate.ProjectId == id)
            .Include(gate => gate.Owner)
            .Include(gate => gate.Checks)
            .OrderBy(gate => gate.GateOrder)
            .ThenBy(gate => gate.DueDate)
            .ToListAsync();

        return Ok(gates.Select(MapStageGate));
    }

    [HttpPost("{id}/stage-gates")]
    public async Task<ActionResult<ProjectStageGateDto>> AddStageGate(Guid id, [FromBody] CreateProjectStageGateRequest req)
    {
        if (!RoleAccess.CanManagePmo(CurrentRole)) return Forbid();

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var ownerId = req.OwnerId ?? UserId;
        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == ownerId && u.TenantId == TenantId);
        if (owner == null) return BadRequest(new { message = "Owner nicht gefunden." });

        var nextOrder = await _db.ProjectStageGates
            .Where(gate => gate.ProjectId == id)
            .Select(gate => gate.GateOrder)
            .DefaultIfEmpty(0)
            .MaxAsync();

        var gate = new ProjectStageGate
        {
            ProjectId = id,
            OwnerId = ownerId,
            Title = req.Title,
            StageKey = req.StageKey?.Trim() ?? "",
            GateOrder = req.GateOrder ?? nextOrder + 1,
            Status = "planned",
            DueDate = req.DueDate,
            Notes = req.Notes ?? "",
            ApprovalSummary = req.ApprovalSummary ?? ""
        };

        _db.ProjectStageGates.Add(gate);
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = id, EntityId = gate.Id, EntityType = "ProjectStageGate", Action = $"hat Stage Gate erstellt: {gate.Title}" });
        await _db.SaveChangesAsync();

        gate.Owner = owner;
        return Ok(MapStageGate(gate));
    }

    [HttpPatch("{id}/stage-gates/{gateId}/status")]
    public async Task<IActionResult> UpdateStageGateStatus(Guid id, Guid gateId, [FromBody] UpdateProjectStageGateStatusRequest req)
    {
        if (!RoleAccess.CanManagePmo(CurrentRole)) return Forbid();

        var gate = await _db.ProjectStageGates
            .Include(item => item.Project)
            .FirstOrDefaultAsync(item => item.Id == gateId && item.ProjectId == id && item.Project!.TenantId == TenantId);

        if (gate == null) return NotFound();
        gate.Status = req.Status;
        gate.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/stage-gates/{gateId}/checks")]
    public async Task<ActionResult<ProjectStageGateCheckDto>> AddStageGateCheck(Guid id, Guid gateId, [FromBody] CreateProjectStageGateCheckRequest req)
    {
        if (!RoleAccess.CanManagePmo(CurrentRole)) return Forbid();

        var gate = await _db.ProjectStageGates
            .Include(item => item.Project)
            .FirstOrDefaultAsync(item => item.Id == gateId && item.ProjectId == id && item.Project!.TenantId == TenantId);

        if (gate == null) return NotFound();

        var check = new ProjectStageGateCheck
        {
            StageGateId = gateId,
            Title = req.Title,
            RequirementType = req.RequirementType?.Trim() ?? "",
            Status = "open",
            IsMandatory = req.IsMandatory ?? true,
            Notes = req.Notes ?? ""
        };

        _db.ProjectStageGateChecks.Add(check);
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = id, EntityId = check.Id, EntityType = "ProjectStageGateCheck", Action = $"hat Gate-Check erstellt: {check.Title}" });
        await _db.SaveChangesAsync();

        return Ok(MapStageGateCheck(check));
    }

    [HttpPatch("{id}/stage-gates/{gateId}/checks/{checkId}/status")]
    public async Task<IActionResult> UpdateStageGateCheckStatus(Guid id, Guid gateId, Guid checkId, [FromBody] UpdateProjectStageGateCheckStatusRequest req)
    {
        if (!RoleAccess.CanManagePmo(CurrentRole)) return Forbid();

        var check = await _db.ProjectStageGateChecks
            .Include(item => item.StageGate)!.ThenInclude(gate => gate!.Project)
            .FirstOrDefaultAsync(item => item.Id == checkId && item.StageGateId == gateId && item.StageGate!.ProjectId == id && item.StageGate.Project!.TenantId == TenantId);

        if (check == null) return NotFound();
        check.Status = req.Status;
        check.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/approvals")]
    public async Task<ActionResult<List<ProjectApprovalDto>>> GetApprovals(Guid id)
    {
        var approvals = await _db.ProjectApprovals
            .Where(approval => approval.ProjectId == id)
            .Include(approval => approval.RequestedBy)
            .Include(approval => approval.DecidedBy)
            .OrderBy(approval => approval.DueDate)
            .ToListAsync();

        return Ok(approvals.Select(MapApproval));
    }

    [HttpPost("{id}/approvals")]
    public async Task<ActionResult<ProjectApprovalDto>> AddApproval(Guid id, [FromBody] CreateProjectApprovalRequest req)
    {
        if (!RoleAccess.CanEditProject(CurrentRole)) return Forbid();

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (project == null) return NotFound();

        if (req.StageGateId.HasValue)
        {
            var gateExists = await _db.ProjectStageGates.AnyAsync(gate => gate.Id == req.StageGateId.Value && gate.ProjectId == id);
            if (!gateExists) return BadRequest(new { message = "Stage Gate nicht gefunden." });
        }

        var approval = new ProjectApproval
        {
            ProjectId = id,
            StageGateId = req.StageGateId,
            RequestedById = UserId,
            Title = req.Title,
            ApprovalType = req.ApprovalType?.Trim() ?? "",
            Status = "pending",
            DueDate = req.DueDate,
            DecisionNotes = req.DecisionNotes ?? ""
        };

        _db.ProjectApprovals.Add(approval);
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = id, EntityId = approval.Id, EntityType = "ProjectApproval", Action = $"hat Freigabe angefordert: {approval.Title}" });
        await _db.SaveChangesAsync();

        approval.RequestedBy = await _db.Users.FindAsync(UserId);
        return Ok(MapApproval(approval));
    }

    [HttpPatch("{id}/approvals/{approvalId}/status")]
    public async Task<IActionResult> UpdateApprovalStatus(Guid id, Guid approvalId, [FromBody] UpdateProjectApprovalStatusRequest req)
    {
        if (!RoleAccess.CanDecideApproval(CurrentRole)) return Forbid();

        var approval = await _db.ProjectApprovals
            .Include(item => item.Project)
            .FirstOrDefaultAsync(item => item.Id == approvalId && item.ProjectId == id && item.Project!.TenantId == TenantId);

        if (approval == null) return NotFound();
        var oldStatus = approval.Status;
        approval.Status = req.Status;
        approval.DecisionNotes = req.DecisionNotes ?? approval.DecisionNotes;
        approval.DecidedById = UserId;
        approval.UpdatedAt = DateTime.UtcNow;

        _db.AuditEntries.Add(new AuditEntry
        {
            TenantId = TenantId,
            UserId = UserId,
            ProjectId = id,
            EntityId = approval.Id,
            EntityType = "ProjectApproval",
            ChangeType = "status_update",
            Title = $"Freigabe aktualisiert: {approval.Title}",
            FromValue = oldStatus,
            ToValue = approval.Status,
            Detail = string.IsNullOrWhiteSpace(approval.DecisionNotes) ? "Statusaenderung ohne Notiz." : approval.DecisionNotes
        });

        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = id, EntityId = approval.Id, EntityType = "ProjectApproval", Action = $"hat Freigabe aktualisiert: {approval.Title} ({approval.Status})" });
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/knowledge-items")]
    public async Task<ActionResult<List<ProjectKnowledgeItemDto>>> GetKnowledgeItems(Guid id)
    {
        var items = await _db.ProjectKnowledgeItems
            .Where(t => t.ProjectId == id)
            .Include(t => t.Author)
            .OrderByDescending(t => t.Importance)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(items.Select(MapKnowledgeItem));
    }

    [HttpPost("{id}/knowledge-documents")]
    public async Task<ActionResult<ProjectKnowledgeItemDto>> UploadKnowledgeDocument(Guid id, [FromBody] UploadKnowledgeDocumentRequest req)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var version = 1;
        if (req.ParentKnowledgeItemId.HasValue)
        {
            var parent = await _db.ProjectKnowledgeItems
                .FirstOrDefaultAsync(item => item.Id == req.ParentKnowledgeItemId.Value && item.ProjectId == id);

            if (parent == null) return BadRequest(new { message = "Die referenzierte Knowledge-Version wurde nicht gefunden." });
            version = Math.Max(parent.Version + 1, 2);
        }

        var item = new ProjectKnowledgeItem
        {
            ProjectId = id,
            AuthorId = UserId,
            Title = req.Title,
            SourceType = string.IsNullOrWhiteSpace(req.SourceType) ? "document_upload" : req.SourceType.Trim(),
            SourceLabel = req.SourceLabel?.Trim() ?? "",
            Category = string.IsNullOrWhiteSpace(req.Category) ? "document" : req.Category.Trim(),
            SourceFileName = req.SourceFileName?.Trim() ?? "",
            Version = version,
            ParentKnowledgeItemId = req.ParentKnowledgeItemId,
            LinkedEntityType = req.LinkedEntityType?.Trim() ?? "",
            LinkedEntityId = req.LinkedEntityId,
            MeetingReference = req.MeetingReference?.Trim() ?? "",
            Content = req.Content,
            TagsCsv = string.Join("|", (req.Tags ?? []).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim())),
            Importance = Math.Clamp(req.Importance ?? 4, 1, 5)
        };

        _db.ProjectKnowledgeItems.Add(item);
        _db.ActivityLogs.Add(new ActivityLog
        {
            TenantId = TenantId,
            UserId = UserId,
            ProjectId = id,
            EntityId = item.Id,
            EntityType = "KnowledgeDocument",
            Action = $"hat Dokumentwissen importiert: {item.Title} v{item.Version}"
        });

        await _db.SaveChangesAsync();

        item.Author = await _db.Users.FindAsync(UserId);
        return Ok(MapKnowledgeItem(item));
    }

    [HttpGet("{id}/knowledge-hub")]
    public async Task<ActionResult<ProjectKnowledgeHubDto>> GetKnowledgeHub(
        Guid id,
        [FromQuery] string? query,
        [FromQuery] string? sourceType,
        [FromQuery] int? minImportance,
        [FromQuery] int? limit)
    {
        var project = await _db.Projects
            .Include(p => p.KnowledgeItems).ThenInclude(item => item.Author)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);

        if (project == null) return NotFound();

        var tokens = TokenizeSearch(query);
        var filteredItems = project.KnowledgeItems
            .Where(item => string.IsNullOrWhiteSpace(sourceType) || item.SourceType.Equals(sourceType, StringComparison.OrdinalIgnoreCase))
            .Where(item => !minImportance.HasValue || item.Importance >= minImportance.Value)
            .Select(item => new { Item = item, Score = GetKnowledgeRelevance(item, tokens) })
            .Where(x => tokens.Count == 0 || x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Item.Importance)
            .ThenByDescending(x => x.Item.CreatedAt)
            .Take(Math.Clamp(limit ?? 50, 1, 200))
            .ToList();

        var allItems = project.KnowledgeItems.ToList();
        var sourceStats = allItems
            .GroupBy(item => item.SourceType)
            .Select(group => new KnowledgeSourceStatDto(
                group.Key,
                group.Count(),
                group.Count(item => item.Importance >= 4)))
            .OrderByDescending(group => group.Count)
            .ThenBy(group => group.Key)
            .ToList();

        var topTags = allItems
            .SelectMany(item => SplitList(item.TagsCsv))
            .GroupBy(tag => tag)
            .Select(group => new KnowledgeTagStatDto(group.Key, group.Count()))
            .OrderByDescending(group => group.Count)
            .ThenBy(group => group.Tag)
            .Take(12)
            .ToList();

        var semanticMatches = project.KnowledgeItems
            .Where(item => string.IsNullOrWhiteSpace(sourceType) || item.SourceType.Equals(sourceType, StringComparison.OrdinalIgnoreCase))
            .Where(item => !minImportance.HasValue || item.Importance >= minImportance.Value)
            .SelectMany(item => BuildKnowledgeChunks(item, tokens))
            .Where(chunk => tokens.Count == 0 || chunk.SemanticScore > 0)
            .OrderByDescending(chunk => chunk.SemanticScore)
            .ThenBy(chunk => chunk.KnowledgeTitle)
            .Take(12)
            .ToList();

        return Ok(new ProjectKnowledgeHubDto(
            project.Id,
            project.Name,
            allItems.Count,
            allItems.Count(item => item.Importance >= 4),
            sourceStats,
            topTags,
            filteredItems.Select(x => MapKnowledgeHubItem(x.Item, x.Score, tokens)).ToList(),
            semanticMatches
        ));
    }

    [HttpPost("{id}/knowledge-items")]
    public async Task<ActionResult<ProjectKnowledgeItemDto>> AddKnowledgeItem(Guid id, [FromBody] CreateProjectKnowledgeItemRequest req)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (project == null) return NotFound();

        var item = new ProjectKnowledgeItem
        {
            ProjectId = id,
            AuthorId = UserId,
            Title = req.Title,
            SourceType = string.IsNullOrWhiteSpace(req.SourceType) ? "note" : req.SourceType.Trim(),
            SourceLabel = req.SourceLabel?.Trim() ?? "",
            Category = string.IsNullOrWhiteSpace(req.Category) ? "general" : req.Category.Trim(),
            SourceFileName = req.SourceFileName?.Trim() ?? "",
            Version = req.ParentKnowledgeItemId.HasValue ? 2 : 1,
            ParentKnowledgeItemId = req.ParentKnowledgeItemId,
            LinkedEntityType = req.LinkedEntityType?.Trim() ?? "",
            LinkedEntityId = req.LinkedEntityId,
            MeetingReference = req.MeetingReference?.Trim() ?? "",
            Content = req.Content,
            TagsCsv = string.Join("|", (req.Tags ?? []).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim())),
            Importance = Math.Clamp(req.Importance ?? 3, 1, 5)
        };

        _db.ProjectKnowledgeItems.Add(item);
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = id, EntityId = item.Id, EntityType = "ProjectKnowledgeItem", Action = $"hat Knowledge-Eintrag erstellt: {item.Title}" });
        await _db.SaveChangesAsync();

        item.Author = await _db.Users.FindAsync(UserId);
        return Ok(MapKnowledgeItem(item));
    }

    [HttpGet("{id}/teams-link")]
    public async Task<ActionResult<ProjectTeamsLinkDto?>> GetTeamsLink(Guid id)
    {
        var project = await _db.Projects
            .Include(p => p.TeamsLink)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);

        if (project == null) return NotFound();
        return Ok(project.TeamsLink == null ? null : MapTeamsLink(project.TeamsLink));
    }

    [HttpPut("{id}/teams-link")]
    public async Task<ActionResult<ProjectTeamsLinkDto>> UpsertTeamsLink(Guid id, [FromBody] UpsertProjectTeamsLinkRequest req)
    {
        if (!RoleAccess.CanConfigureIntegrations(CurrentRole)) return Forbid();

        var project = await _db.Projects
            .Include(p => p.TeamsLink)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);

        if (project == null) return NotFound();

        var link = project.TeamsLink;
        if (link == null)
        {
            link = new ProjectTeamsLink { ProjectId = id };
            _db.ProjectTeamsLinks.Add(link);
        }

        link.TeamName = req.TeamName;
        link.ChannelName = req.ChannelName;
        link.TeamId = req.TeamId;
        link.ChannelId = req.ChannelId;
        link.TenantDomain = req.TenantDomain;
        link.SyncStatus = req.SyncStatus;
        link.LastSyncAt = req.SyncStatus == "connected" ? DateTime.UtcNow : link.LastSyncAt;
        link.UpdatedAt = DateTime.UtcNow;

        _db.ActivityLogs.Add(new ActivityLog
        {
            TenantId = TenantId,
            UserId = UserId,
            ProjectId = id,
            EntityId = link.Id,
            EntityType = "ProjectTeamsLink",
            Action = $"hat Teams-Link aktualisiert: {req.TeamName} / {req.ChannelName}"
        });

        await _db.SaveChangesAsync();
        return Ok(MapTeamsLink(link));
    }

    [HttpGet("{id}/jira-link")]
    public async Task<ActionResult<ProjectJiraLinkDto?>> GetJiraLink(Guid id)
    {
        var project = await _db.Projects
            .Include(p => p.JiraLink)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);

        if (project == null) return NotFound();
        return Ok(project.JiraLink == null ? null : MapJiraLink(project.JiraLink));
    }

    [HttpPut("{id}/jira-link")]
    public async Task<ActionResult<ProjectJiraLinkDto>> UpsertJiraLink(Guid id, [FromBody] UpsertProjectJiraLinkRequest req)
    {
        if (!RoleAccess.CanConfigureIntegrations(CurrentRole)) return Forbid();

        var project = await _db.Projects
            .Include(p => p.JiraLink)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);

        if (project == null) return NotFound();

        var link = project.JiraLink;
        if (link == null)
        {
            link = new ProjectJiraLink { ProjectId = id };
            _db.ProjectJiraLinks.Add(link);
        }

        link.BoardName = req.BoardName?.Trim() ?? "";
        link.ProjectKey = req.ProjectKey?.Trim().ToUpperInvariant() ?? "";
        link.BoardId = req.BoardId?.Trim() ?? "";
        link.JqlFilter = req.JqlFilter?.Trim() ?? "";
        link.SyncStatus = string.IsNullOrWhiteSpace(req.SyncStatus) ? "planned" : req.SyncStatus.Trim();
        link.LastSyncAt = link.SyncStatus == "connected" ? DateTime.UtcNow : link.LastSyncAt;
        link.UpdatedAt = DateTime.UtcNow;

        _db.ActivityLogs.Add(new ActivityLog
        {
            TenantId = TenantId,
            UserId = UserId,
            ProjectId = id,
            EntityId = link.Id,
            EntityType = "ProjectJiraLink",
            Action = $"hat Jira-Link aktualisiert: {link.ProjectKey}"
        });

        await _db.SaveChangesAsync();
        return Ok(MapJiraLink(link));
    }

    private static ProjectDto MapProject(Project p) => new(
        p.Id,
        p.Name,
        p.Description,
        p.Customer,
        p.Category,
        p.Stage,
        p.Status,
        p.ProgressPercent,
        p.BudgetTotal,
        p.BudgetSpent,
        p.StartDate,
        p.EndDate,
        p.TeamAssignments?.Count ?? 0,
        p.Owner?.DisplayName ?? "",
        p.CreatedAt
    );

    private static ProjectDetailDto MapProjectDetail(Project p) => new(
        p.Id,
        p.Name,
        p.Description,
        p.Customer,
        p.Category,
        p.Stage,
        p.DeliveryModel,
        p.Sponsor,
        p.ExecutiveSummary,
        p.HealthSummary,
        p.Objective,
        p.Scope,
        p.SuccessMetric,
        p.Communication,
        p.NextMilestone,
        SplitList(p.StakeholdersCsv),
        SplitList(p.TechnologiesCsv),
        p.Status,
        p.ProgressPercent,
        p.BudgetTotal,
        p.BudgetSpent,
        p.StartDate,
        p.EndDate,
        p.Owner?.DisplayName ?? "",
        p.CreatedAt,
        p.TeamAssignments.OrderBy(a => a.User!.DisplayName).Select(MapTeamMember).ToList(),
        p.Notes.OrderByDescending(n => n.CreatedAt).Select(MapNote).ToList(),
        p.LeadTasks.OrderBy(t => t.DueDate).Select(MapLeadTask).ToList(),
        p.Milestones.OrderBy(t => t.DueDate).Select(MapMilestone).ToList(),
        p.Decisions.OrderBy(t => t.DueDate).Select(MapDecision).ToList(),
        p.Documents.OrderByDescending(t => t.CreatedAt).Select(MapDocument).ToList(),
        p.GovernanceChecks.OrderBy(t => t.DueDate).Select(MapGovernanceCheck).ToList(),
        p.StageGates.OrderBy(t => t.GateOrder).ThenBy(t => t.DueDate).Select(MapStageGate).ToList(),
        p.Approvals.OrderBy(t => t.DueDate).Select(MapApproval).ToList(),
        p.KnowledgeItems.OrderByDescending(t => t.Importance).ThenByDescending(t => t.CreatedAt).Select(MapKnowledgeItem).ToList(),
        p.TeamsLink == null ? null : MapTeamsLink(p.TeamsLink),
        p.JiraLink == null ? null : MapJiraLink(p.JiraLink)
    );

    private static ProjectTeamMemberDto MapTeamMember(ResourceAllocation allocation) => new(
        allocation.UserId,
        allocation.User?.DisplayName ?? "",
        allocation.User?.Email ?? "",
        allocation.User?.Role ?? "",
        allocation.ProjectRole,
        allocation.Responsibility,
        allocation.AllocatedHours,
        40
    );

    private static ProjectNoteDto MapNote(ProjectNote note) => new(note.Id, note.Title, note.Content, note.Author?.DisplayName ?? "", note.CreatedAt);
    private static ProjectLeadTaskDto MapLeadTask(ProjectLeadTask task) => new(task.Id, task.Title, task.Description, task.Owner?.DisplayName ?? "", task.DueDate, task.Status);
    private static ProjectMilestoneDto MapMilestone(ProjectMilestone milestone) => new(milestone.Id, milestone.Title, milestone.Description, milestone.Owner?.DisplayName ?? "", milestone.DueDate, milestone.Status);
    private static ProjectDecisionDto MapDecision(ProjectDecision decision) => new(decision.Id, decision.Title, decision.Context, decision.Decision, decision.Owner?.DisplayName ?? "", decision.DueDate, decision.Status);
    private static ProjectDocumentDto MapDocument(ProjectDocument document) => new(document.Id, document.Title, document.Category, document.Url, document.Status, document.Owner?.DisplayName ?? "", document.CreatedAt);
    private static ProjectGovernanceCheckDto MapGovernanceCheck(ProjectGovernanceCheck check) => new(check.Id, check.Title, check.Area, check.Notes, check.Owner?.DisplayName ?? "", check.DueDate, check.Status);
    private static ProjectStageGateCheckDto MapStageGateCheck(ProjectStageGateCheck check) => new(check.Id, check.Title, check.RequirementType, check.Status, check.IsMandatory, check.Notes);
    private static ProjectStageGateDto MapStageGate(ProjectStageGate gate) => new(gate.Id, gate.Title, gate.StageKey, gate.GateOrder, gate.Status, gate.DueDate, gate.Owner?.DisplayName ?? "", gate.Notes, gate.ApprovalSummary, gate.Checks.OrderBy(check => check.CreatedAt).Select(MapStageGateCheck).ToList());
    private static ProjectApprovalDto MapApproval(ProjectApproval approval) => new(approval.Id, approval.StageGateId, approval.Title, approval.ApprovalType, approval.Status, approval.DueDate, approval.RequestedBy?.DisplayName ?? "", approval.DecidedBy?.DisplayName ?? "", approval.DecisionNotes);
    private static ProjectKnowledgeItemDto MapKnowledgeItem(ProjectKnowledgeItem item) => new(item.Id, item.Title, item.SourceType, item.SourceLabel, item.Category, item.SourceFileName, item.Version, item.ParentKnowledgeItemId, item.LinkedEntityType, item.LinkedEntityId, item.MeetingReference, item.Content, SplitList(item.TagsCsv), item.Author?.DisplayName ?? "", item.Importance, item.CreatedAt);
    private static ProjectTeamsLinkDto MapTeamsLink(ProjectTeamsLink link) => new(link.ProjectId, link.TeamName, link.ChannelName, link.TeamId, link.ChannelId, link.TenantDomain, link.SyncStatus, link.LastSyncAt);
    private static ProjectJiraLinkDto MapJiraLink(ProjectJiraLink link) => new(link.ProjectId, link.BoardName, link.ProjectKey, link.BoardId, link.JqlFilter, link.SyncStatus, link.LastSyncAt);

    private static List<string> SplitList(string value) => value
        .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();

    private static ProjectKnowledgeHubItemDto MapKnowledgeHubItem(ProjectKnowledgeItem item, int relevanceScore, List<string> tokens) => new(
        item.Id,
        item.Title,
        item.SourceType,
        item.SourceLabel,
        item.Category,
        item.SourceFileName,
        item.Version,
        item.ParentKnowledgeItemId,
        item.LinkedEntityType,
        item.LinkedEntityId,
        item.MeetingReference,
        item.Content,
        BuildExcerpt(item.Content, tokens),
        SplitList(item.TagsCsv),
        item.Author?.DisplayName ?? "",
        item.Importance,
        item.CreatedAt,
        relevanceScore
    );

    private static List<string> TokenizeSearch(string? query) => (query ?? "")
        .ToLowerInvariant()
        .Split(new[] { ' ', ',', '.', ';', ':', '\n', '\r', '\t', '-', '_', '/', '\\', '(', ')' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(token => token.Length >= 3)
        .Distinct()
        .ToList();

    private static int GetKnowledgeRelevance(ProjectKnowledgeItem item, List<string> tokens)
    {
        if (tokens.Count == 0)
        {
            return item.Importance * 10;
        }

        var haystack = string.Join(" ", new[]
        {
            item.Title,
            item.SourceType,
            item.SourceLabel,
            item.Content,
            item.TagsCsv
        }).ToLowerInvariant();

        var score = item.Importance * 5;
        foreach (var token in tokens)
        {
            if (item.Title.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 12;
            if (item.SourceLabel.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 6;
            if (item.SourceType.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 4;
            if (item.TagsCsv.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 8;
            if (haystack.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 3;
        }

        return score;
    }

    private static List<KnowledgeChunkDto> BuildKnowledgeChunks(ProjectKnowledgeItem item, List<string> tokens)
    {
        var chunks = SplitIntoChunks(item.Content);
        return chunks.Select((chunk, index) => new KnowledgeChunkDto(
            item.Id,
            item.Title,
            item.SourceType,
            item.Category,
            index + 1,
            chunk,
            GetSemanticChunkScore(item, chunk, tokens)
        )).ToList();
    }

    private static int GetSemanticChunkScore(ProjectKnowledgeItem item, string chunk, List<string> tokens)
    {
        if (tokens.Count == 0)
        {
            return item.Importance * 8;
        }

        var score = item.Importance * 4;
        foreach (var token in tokens)
        {
            if (chunk.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 8;
            if (item.Title.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 10;
            if (item.TagsCsv.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 6;
            if (item.Category.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 5;
        }

        if (item.LinkedEntityType.Length > 0) score += 2;
        if (item.Version > 1) score += 1;
        return score;
    }

    private static List<string> SplitIntoChunks(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var paragraphs = content
            .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(paragraph => paragraph.Replace('\r', ' ').Replace('\n', ' ').Trim())
            .Where(paragraph => paragraph.Length > 0)
            .ToList();

        if (paragraphs.Count > 0)
        {
            return paragraphs
                .SelectMany(paragraph => paragraph.Length <= 260
                    ? new[] { paragraph }
                    : paragraph.Chunk(220).Select(chunk => new string(chunk)).ToArray())
                .ToList();
        }

        return content.Length <= 260
            ? [content.Trim()]
            : content.Chunk(220).Select(chunk => new string(chunk).Trim()).Where(chunk => chunk.Length > 0).ToList();
    }

    private static string BuildExcerpt(string content, List<string> tokens)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "";
        }

        var normalized = content.Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (normalized.Length <= 180 || tokens.Count == 0)
        {
            return normalized.Length <= 180 ? normalized : normalized[..180] + "...";
        }

        var lower = normalized.ToLowerInvariant();
        var matchIndex = tokens
            .Select(token => lower.IndexOf(token, StringComparison.Ordinal))
            .Where(index => index >= 0)
            .DefaultIfEmpty(0)
            .Min();

        var start = Math.Max(0, matchIndex - 60);
        var length = Math.Min(180, normalized.Length - start);
        var excerpt = normalized.Substring(start, length).Trim();

        if (start > 0) excerpt = "..." + excerpt;
        if (start + length < normalized.Length) excerpt += "...";
        return excerpt;
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var day = date.DayOfWeek;
        var diff = day == DayOfWeek.Sunday ? 6 : (int)day - 1;
        return date.AddDays(-diff);
    }
}
