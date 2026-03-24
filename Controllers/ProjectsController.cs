using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using System.Security.Claims;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/[controller]"), Authorize]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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

        return Ok(new PortfolioDto(
            projects.Select(MapProject).ToList(),
            projects.Count,
            projects.Count(p => p.Status == "green"),
            projects.Count(p => p.Status == "yellow"),
            projects.Count(p => p.Status == "red"),
            projects.Sum(p => p.BudgetTotal),
            projects.Sum(p => p.BudgetSpent),
            allTasks.Count(t => t.Status != "done"),
            allTasks.Count(t => t.DueDate.HasValue && t.DueDate < today && t.Status != "done"),
            projects.SelectMany(p => p.Decisions).Count(d => d.Status == "open" || d.Status == "review"),
            projects.SelectMany(p => p.Milestones).Count(m => m.DueDate < today && m.Status != "done"),
            projects.SelectMany(p => p.GovernanceChecks).Count(g => g.Status != "done")
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
            .Include(p => p.KnowledgeItems).ThenInclude(t => t.Author)
            .Include(p => p.TeamsLink)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);

        return project == null ? NotFound() : Ok(MapProjectDetail(project));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectRequest req)
    {
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
        var check = await _db.ProjectGovernanceChecks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == checkId && t.ProjectId == id && t.Project!.TenantId == TenantId);

        if (check == null) return NotFound();
        check.Status = req.Status;
        check.UpdatedAt = DateTime.UtcNow;
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
        p.KnowledgeItems.OrderByDescending(t => t.Importance).ThenByDescending(t => t.CreatedAt).Select(MapKnowledgeItem).ToList(),
        p.TeamsLink == null ? null : MapTeamsLink(p.TeamsLink)
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
    private static ProjectKnowledgeItemDto MapKnowledgeItem(ProjectKnowledgeItem item) => new(item.Id, item.Title, item.SourceType, item.SourceLabel, item.Content, SplitList(item.TagsCsv), item.Author?.DisplayName ?? "", item.Importance, item.CreatedAt);
    private static ProjectTeamsLinkDto MapTeamsLink(ProjectTeamsLink link) => new(link.ProjectId, link.TeamName, link.ChannelName, link.TeamId, link.ChannelId, link.TenantDomain, link.SyncStatus, link.LastSyncAt);

    private static List<string> SplitList(string value) => value
        .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();
}
