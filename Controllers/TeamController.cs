using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using PmTool.Api.Security;
using System.Security.Claims;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/[controller]"), Authorize]
public class TeamController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? "";

    public TeamController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<TeamMemberDto>>> GetAll()
    {
        var users = await _db.Users
            .Where(u => u.TenantId == TenantId)
            .OrderBy(u => u.DisplayName)
            .GroupJoin(
                _db.ResourceAllocations,
                user => user.Id,
                allocation => allocation.UserId,
                (user, allocations) => new TeamMemberDto(
                    user.Id,
                    user.DisplayName,
                    user.Email,
                    user.Role,
                    allocations.Sum(a => a.AllocatedHours),
                    40
                ))
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("invite")]
    public async Task<ActionResult<TeamMemberDto>> Invite([FromBody] InviteTeamMemberRequest req)
    {
        if (!RoleAccess.CanManageTeam(CurrentRole)) return Forbid();

        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.TenantId == TenantId && u.Email == email))
            return Conflict(new { message = "E-Mail ist bereits vorhanden." });
        if (!RoleCatalog.IsValidRole(req.Role))
            return BadRequest(new { message = "Rolle ist nicht gueltig." });

        var user = new User
        {
            TenantId = TenantId,
            DisplayName = req.Name.Trim(),
            Email = email,
            Role = string.IsNullOrWhiteSpace(req.Role) ? "Member" : req.Role.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
        };

        _db.Users.Add(user);
        _db.ActivityLogs.Add(new ActivityLog
        {
            TenantId = TenantId,
            UserId = UserId,
            EntityId = user.Id,
            EntityType = "User",
            Action = $"hat Teammitglied {user.DisplayName} eingeladen"
        });
        _db.AuditEntries.Add(new AuditEntry
        {
            TenantId = TenantId,
            UserId = UserId,
            EntityId = user.Id,
            EntityType = "UserRole",
            ChangeType = "create",
            Title = $"Rolle fuer {user.DisplayName} gesetzt",
            FromValue = "",
            ToValue = user.Role,
            Detail = $"Neues Teammitglied wurde mit Rolle '{user.Role}' angelegt."
        });
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new TeamMemberDto(user.Id, user.DisplayName, user.Email, user.Role, 0, 40));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamMemberRequest req)
    {
        if (!RoleAccess.CanManageTeam(CurrentRole)) return Forbid();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == TenantId);
        if (user == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(req.Name)) user.DisplayName = req.Name.Trim();
        var oldRole = user.Role;
        if (!string.IsNullOrWhiteSpace(req.Role))
        {
            if (!RoleCatalog.IsValidRole(req.Role))
                return BadRequest(new { message = "Rolle ist nicht gueltig." });
            user.Role = req.Role.Trim();
        }
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.Equals(oldRole, user.Role, StringComparison.OrdinalIgnoreCase))
        {
            _db.AuditEntries.Add(new AuditEntry
            {
                TenantId = TenantId,
                UserId = UserId,
                EntityId = user.Id,
                EntityType = "UserRole",
                ChangeType = "update",
                Title = $"Rolle fuer {user.DisplayName} geaendert",
                FromValue = oldRole,
                ToValue = user.Role,
                Detail = $"Rollenwechsel von '{oldRole}' zu '{user.Role}'."
            });
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(Guid id)
    {
        if (!RoleAccess.CanManageTeam(CurrentRole)) return Forbid();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == TenantId);
        if (user == null) return NotFound();
        if (user.Id == UserId) return BadRequest(new { message = "Aktueller Benutzer kann nicht entfernt werden." });

        var blockers = await GetUserDeleteBlockers(id);
        if (blockers.Count > 0)
        {
            return Conflict(new
            {
                message = $"Benutzer kann nicht geloescht werden, weil noch {blockers.Sum(x => x.Count)} Abhaengigkeiten vorhanden sind.",
                blockers = blockers.Select(x => new { key = x.Key, label = x.Label, count = x.Count })
            });
        }

        var allocations = await _db.ResourceAllocations.Where(a => a.UserId == id).ToListAsync();
        var activityLogs = await _db.ActivityLogs.Where(a => a.UserId == id).ToListAsync();
        var auditEntries = await _db.AuditEntries.Where(a => a.UserId == id).ToListAsync();
        _db.ResourceAllocations.RemoveRange(allocations);
        _db.ActivityLogs.RemoveRange(activityLogs);
        _db.AuditEntries.RemoveRange(auditEntries);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<List<DeleteBlocker>> GetUserDeleteBlockers(Guid userId)
    {
        var checks = new List<(string key, string label, Task<int> query)>
        {
            ("ownedProjects", "Projektverantwortung", _db.Projects.CountAsync(p => p.OwnerId == userId)),
            ("assignedTasks", "Zugewiesene Tasks", _db.Tasks.CountAsync(t => t.AssigneeId == userId)),
            ("taskComments", "Task-Kommentare", _db.TaskComments.CountAsync(c => c.AuthorId == userId)),
            ("ownedRisks", "Risikoverantwortung", _db.Risks.CountAsync(r => r.OwnerId == userId)),
            ("projectNotes", "Projektnotizen", _db.ProjectNotes.CountAsync(n => n.AuthorId == userId)),
            ("leadTasks", "Projektleiter-Aufgaben", _db.ProjectLeadTasks.CountAsync(t => t.OwnerId == userId)),
            ("milestones", "Meilensteine", _db.ProjectMilestones.CountAsync(m => m.OwnerId == userId)),
            ("decisions", "Entscheidungen", _db.ProjectDecisions.CountAsync(d => d.OwnerId == userId)),
            ("documents", "Dokumente", _db.ProjectDocuments.CountAsync(d => d.OwnerId == userId)),
            ("governanceChecks", "Governance-Checks", _db.ProjectGovernanceChecks.CountAsync(c => c.OwnerId == userId)),
            ("stageGates", "Stage Gates", _db.ProjectStageGates.CountAsync(g => g.OwnerId == userId)),
            ("requestedApprovals", "Angeforderte Freigaben", _db.ProjectApprovals.CountAsync(a => a.RequestedById == userId)),
            ("decidedApprovals", "Entschiedene Freigaben", _db.ProjectApprovals.CountAsync(a => a.DecidedById == userId)),
            ("knowledgeItems", "Knowledge-Eintraege", _db.ProjectKnowledgeItems.CountAsync(k => k.AuthorId == userId)),
            ("aiFeedback", "AI-Feedback", _db.AiSuggestionFeedback.CountAsync(f => f.UserId == userId)),
        };

        var blockers = new List<DeleteBlocker>();
        foreach (var check in checks)
        {
            var count = await check.query;
            if (count > 0)
            {
                blockers.Add(new DeleteBlocker(check.key, check.label, count));
            }
        }

        return blockers;
    }

    private sealed record DeleteBlocker(string Key, string Label, int Count);
}
