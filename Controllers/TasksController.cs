using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PmTool.Api.Models;
using System.Security.Claims;

namespace PmTool.Api.Controllers;

[ApiController, Route("api/v1/projects/{projectId}/tasks"), Authorize]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    private Guid UserId   => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public TasksController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<TaskDto>>> GetAll(Guid projectId, [FromQuery] string? status)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == TenantId);
        if (project == null) return NotFound();
        var q = _db.Tasks.Where(t => t.ProjectId == projectId).Include(t => t.Assignee).Include(t => t.Comments).AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(t => t.Status == status);
        return Ok((await q.OrderByDescending(t => t.CreatedAt).ToListAsync()).Select(MapTask));
    }

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create(Guid projectId, [FromBody] CreateTaskRequest req)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == TenantId);
        if (project == null) return NotFound();
        var task = new ProjectTask { ProjectId = projectId, Title = req.Title, Description = req.Description ?? "", Priority = req.Priority, AssigneeId = req.AssigneeId, DueDate = req.DueDate, EstimatedHours = req.EstimatedHours };
        _db.Tasks.Add(task);
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = projectId, EntityId = task.Id, EntityType = "Task", Action = $"hat Task erstellt: {task.Title}" });
        await _db.SaveChangesAsync();
        return Ok(MapTask(task));
    }

    [HttpPatch("{taskId}/status")]
    public async Task<IActionResult> UpdateStatus(Guid projectId, Guid taskId, [FromBody] UpdateTaskStatusRequest req)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);
        if (task == null) return NotFound();
        var old = task.Status; task.Status = req.Status; task.UpdatedAt = DateTime.UtcNow;
        _db.ActivityLogs.Add(new ActivityLog { TenantId = TenantId, UserId = UserId, ProjectId = projectId, EntityId = task.Id, EntityType = "Task", Action = $"Status geaendert: {old} -> {req.Status}" });
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("{taskId}")]
    public async Task<IActionResult> Delete(Guid projectId, Guid taskId)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);
        if (task == null) return NotFound();
        _db.Tasks.Remove(task); await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpPost("{taskId}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(Guid projectId, Guid taskId, [FromBody] AddCommentRequest req)
    {
        var comment = new TaskComment { TaskId = taskId, AuthorId = UserId, Content = req.Content };
        _db.TaskComments.Add(comment); await _db.SaveChangesAsync();
        var author = await _db.Users.FindAsync(UserId);
        return Ok(new CommentDto(comment.Id, comment.Content, author?.DisplayName ?? "", comment.CreatedAt));
    }

    private static TaskDto MapTask(ProjectTask t) => new(t.Id, t.ProjectId, t.Title, t.Description, t.Status, t.Priority, t.AssigneeId, t.Assignee?.DisplayName, t.DueDate, t.EstimatedHours, t.LoggedHours, t.Comments?.Count ?? 0, t.CreatedAt);
}
