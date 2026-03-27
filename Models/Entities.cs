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
    public ICollection<AuditEntry> AuditEntries { get; set; } = new List<AuditEntry>();
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
    [MaxLength(50)] public string Category { get; set; } = "delivery";
    [MaxLength(50)] public string Stage { get; set; } = "planning";
    [MaxLength(100)] public string DeliveryModel { get; set; } = "";
    [MaxLength(200)] public string Sponsor { get; set; } = "";
    public string ExecutiveSummary { get; set; } = "";
    public string HealthSummary { get; set; } = "";
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
    public ICollection<ProjectMilestone> Milestones { get; set; } = new List<ProjectMilestone>();
    public ICollection<ProjectDecision> Decisions { get; set; } = new List<ProjectDecision>();
    public ICollection<ProjectDocument> Documents { get; set; } = new List<ProjectDocument>();
    public ICollection<ProjectGovernanceCheck> GovernanceChecks { get; set; } = new List<ProjectGovernanceCheck>();
    public ICollection<ProjectStageGate> StageGates { get; set; } = new List<ProjectStageGate>();
    public ICollection<ProjectApproval> Approvals { get; set; } = new List<ProjectApproval>();
    public ICollection<ProjectForecastSnapshot> ForecastSnapshots { get; set; } = new List<ProjectForecastSnapshot>();
    public ICollection<ProjectKnowledgeItem> KnowledgeItems { get; set; } = new List<ProjectKnowledgeItem>();
    public ICollection<AiSuggestionFeedback> AiSuggestionFeedback { get; set; } = new List<AiSuggestionFeedback>();
    public ProjectTeamsLink? TeamsLink { get; set; }
    public ProjectJiraLink? JiraLink { get; set; }
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

public class AuditEntry : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? EntityId { get; set; }
    [MaxLength(50)] public string EntityType { get; set; } = "";
    [MaxLength(50)] public string ChangeType { get; set; } = "";
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(500)] public string FromValue { get; set; } = "";
    [MaxLength(500)] public string ToValue { get; set; } = "";
    public string Detail { get; set; } = "";
    public User? User { get; set; }
    public Project? Project { get; set; }
    public Tenant? Tenant { get; set; }
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

public class ProjectMilestone : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid OwnerId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateOnly DueDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "planned";
    public Project? Project { get; set; }
    public User? Owner { get; set; }
}

public class ProjectDecision : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid OwnerId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string Context { get; set; } = "";
    public string Decision { get; set; } = "";
    public DateOnly DueDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "open";
    public Project? Project { get; set; }
    public User? Owner { get; set; }
}

public class ProjectDocument : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid OwnerId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(100)] public string Category { get; set; } = "";
    [MaxLength(500)] public string Url { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "draft";
    public Project? Project { get; set; }
    public User? Owner { get; set; }
}

public class ProjectGovernanceCheck : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid OwnerId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(100)] public string Area { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateOnly DueDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "open";
    public Project? Project { get; set; }
    public User? Owner { get; set; }
}

public class ProjectStageGate : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid OwnerId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(100)] public string StageKey { get; set; } = "";
    public int GateOrder { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "planned";
    public DateOnly DueDate { get; set; }
    public string Notes { get; set; } = "";
    public string ApprovalSummary { get; set; } = "";
    public Project? Project { get; set; }
    public User? Owner { get; set; }
    public ICollection<ProjectStageGateCheck> Checks { get; set; } = new List<ProjectStageGateCheck>();
}

public class ProjectStageGateCheck : BaseEntity
{
    public Guid StageGateId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(100)] public string RequirementType { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "open";
    public bool IsMandatory { get; set; } = true;
    public string Notes { get; set; } = "";
    public ProjectStageGate? StageGate { get; set; }
}

public class ProjectApproval : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid? StageGateId { get; set; }
    public Guid RequestedById { get; set; }
    public Guid? DecidedById { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(100)] public string ApprovalType { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public DateOnly DueDate { get; set; }
    public string DecisionNotes { get; set; } = "";
    public Project? Project { get; set; }
    public ProjectStageGate? StageGate { get; set; }
    public User? RequestedBy { get; set; }
    public User? DecidedBy { get; set; }
}

public class ProjectForecastSnapshot : BaseEntity
{
    public Guid ProjectId { get; set; }
    public DateOnly SnapshotDate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal BudgetAtCompletion { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ActualCost { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal EarnedValue { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal PlannedValue { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal EstimateAtCompletion { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal EstimateToComplete { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal CostPerformanceIndex { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal SchedulePerformanceIndex { get; set; }
    public int TotalEstimatedHours { get; set; }
    public int LoggedHours { get; set; }
    public int RemainingHours { get; set; }
    public Project? Project { get; set; }
}

public class ProjectKnowledgeItem : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid AuthorId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(50)] public string SourceType { get; set; } = "note";
    [MaxLength(500)] public string SourceLabel { get; set; } = "";
    [MaxLength(50)] public string Category { get; set; } = "general";
    [MaxLength(260)] public string SourceFileName { get; set; } = "";
    public int Version { get; set; } = 1;
    public Guid? ParentKnowledgeItemId { get; set; }
    [MaxLength(50)] public string LinkedEntityType { get; set; } = "";
    public Guid? LinkedEntityId { get; set; }
    [MaxLength(200)] public string MeetingReference { get; set; } = "";
    [Required] public string Content { get; set; } = "";
    public string TagsCsv { get; set; } = "";
    public int Importance { get; set; } = 3;
    public Project? Project { get; set; }
    public User? Author { get; set; }
}

public class AiSuggestionFeedback : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    [Required, MaxLength(50)] public string SuggestionType { get; set; } = "";
    [Required, MaxLength(200)] public string SuggestionTitle { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "accepted";
    public string Notes { get; set; } = "";
    public Project? Project { get; set; }
    public User? User { get; set; }
}

public class ProjectTeamsLink : BaseEntity
{
    public Guid ProjectId { get; set; }
    [MaxLength(200)] public string TeamName { get; set; } = "";
    [MaxLength(200)] public string ChannelName { get; set; } = "";
    [MaxLength(200)] public string TeamId { get; set; } = "";
    [MaxLength(200)] public string ChannelId { get; set; } = "";
    [MaxLength(200)] public string TenantDomain { get; set; } = "";
    [MaxLength(20)] public string SyncStatus { get; set; } = "planned";
    public DateTime? LastSyncAt { get; set; }
    public Project? Project { get; set; }
}

public class ProjectJiraLink : BaseEntity
{
    public Guid ProjectId { get; set; }
    [MaxLength(200)] public string BoardName { get; set; } = "";
    [MaxLength(100)] public string ProjectKey { get; set; } = "";
    [MaxLength(100)] public string BoardId { get; set; } = "";
    [MaxLength(1000)] public string JqlFilter { get; set; } = "";
    [MaxLength(20)] public string SyncStatus { get; set; } = "planned";
    public DateTime? LastSyncAt { get; set; }
    public Project? Project { get; set; }
}
