using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PmTool.Api.Models;

public abstract class BaseEntity
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Tenant : BaseEntity
{
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    [Required, MaxLength(100)] public string Slug { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

public class User : BaseEntity
{
    [Required, MaxLength(200)] public string DisplayName { get; set; } = "";
    [Required, MaxLength(200)] public string Email { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
    [MaxLength(50)] public string Role { get; set; } = "Member";
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
}

public class Project : BaseEntity
{
    public Guid TenantId { get; set; }
    [Required, MaxLength(300)] public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    [Required, MaxLength(200)] public string Customer { get; set; } = "";
    public string Objective { get; set; } = "";
    public string Scope { get; set; } = "";
    public string SuccessMetric { get; set; } = "";
    public string Communication { get; set; } = "";
    public string NextMilestone { get; set; } = "";
    public string StakeholdersCsv { get; set; } = "";
    public string TechnologiesCsv { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "green";
    public int ProgressPercent { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal BudgetTotal { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal BudgetSpent { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    public ICollection<Risk> Risks { get; set; } = new List<Risk>();
    public ICollection<ActivityLog> Activities { get; set; } = new List<ActivityLog>();
    public ICollection<ResourceAllocation> TeamAssignments { get; set; } = new List<ResourceAllocation>();
    public ICollection<ProjectNote> Notes { get; set; } = new List<ProjectNote>();
    public ICollection<ProjectLeadTask> LeadTasks { get; set; } = new List<ProjectLeadTask>();
}

public class ProjectTask : BaseEntity
{
    public Guid ProjectId { get; set; }
    [Required, MaxLength(400)] public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "todo";
    [MaxLength(20)] public string Priority { get; set; } = "medium";
    public Guid? AssigneeId { get; set; }
    public DateOnly? DueDate { get; set; }
    public int EstimatedHours { get; set; }
    public int LoggedHours { get; set; }
    public Project? Project { get; set; }
    public User? Assignee { get; set; }
    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
}

public class TaskComment : BaseEntity
{
    public Guid TaskId { get; set; }
    public Guid AuthorId { get; set; }
    [Required] public string Content { get; set; } = "";
    public ProjectTask? Task { get; set; }
    public User? Author { get; set; }
}

public class Risk : BaseEntity
{
    public Guid ProjectId { get; set; }
    [Required, MaxLength(400)] public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int Impact { get; set; } = 1;
    public int Probability { get; set; } = 1;
    [MaxLength(20)] public string Status { get; set; } = "open";
    public string Mitigation { get; set; } = "";
    public Guid OwnerId { get; set; }
    public Project? Project { get; set; }
    public User? Owner { get; set; }
}

public class ActivityLog : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? EntityId { get; set; }
    [MaxLength(50)] public string EntityType { get; set; } = "";
    [MaxLength(200)] public string Action { get; set; } = "";
    public User? User { get; set; }
    public Project? Project { get; set; }
}

public class ResourceAllocation : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public int AllocatedHours { get; set; }
    [MaxLength(100)] public string ProjectRole { get; set; } = "";
    public string Responsibility { get; set; } = "";
    public User? User { get; set; }
    public Project? Project { get; set; }
}

public class ProjectNote : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid AuthorId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [Required] public string Content { get; set; } = "";
    public Project? Project { get; set; }
    public User? Author { get; set; }
}

public class ProjectLeadTask : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid OwnerId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateOnly DueDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "todo";
    public Project? Project { get; set; }
    public User? Owner { get; set; }
}
