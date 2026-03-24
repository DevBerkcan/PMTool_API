using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using System.Security.Claims;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/[controller]"), Authorize]
public class TeamController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.TenantId == TenantId && u.Email == email))
            return Conflict(new { message = "E-Mail ist bereits vorhanden." });

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
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new TeamMemberDto(user.Id, user.DisplayName, user.Email, user.Role, 0, 40));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamMemberRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == TenantId);
        if (user == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(req.Name)) user.DisplayName = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.Role)) user.Role = req.Role.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == TenantId);
        if (user == null) return NotFound();
        if (user.Id == UserId) return BadRequest(new { message = "Aktueller Benutzer kann nicht entfernt werden." });

        var allocations = await _db.ResourceAllocations.Where(a => a.UserId == id).ToListAsync();
        _db.ResourceAllocations.RemoveRange(allocations);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
