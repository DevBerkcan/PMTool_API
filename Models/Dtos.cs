namespace PmTool.Api.Models;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string UserName, string Email, string Role, Guid UserId, Guid TenantId);

public record ProjectDto(
    Guid Id,
    string Name,
    string Description,
    string Customer,
    string Status,
    int ProgressPercent,
    decimal BudgetTotal,
    decimal BudgetSpent,
    DateOnly StartDate,
    DateOnly EndDate,
    int TeamSize,
    string OwnerName,
    DateTime CreatedAt
);

public record ProjectTeamMemberDto(
    Guid UserId,
    string Name,
    string Email,
    string Role,
    string ProjectRole,
    string Responsibility,
    int AllocatedHours,
    int TotalCapacityHours
);

public record ProjectNoteDto(Guid Id, string Title, string Content, string AuthorName, DateTime CreatedAt);
public record ProjectLeadTaskDto(Guid Id, string Title, string Description, string OwnerName, DateOnly DueDate, string Status);

public record ProjectDetailDto(
    Guid Id,
    string Name,
    string Description,
    string Customer,
    string Objective,
    string Scope,
    string SuccessMetric,
    string Communication,
    string NextMilestone,
    List<string> Stakeholders,
    List<string> Technologies,
    string Status,
    int ProgressPercent,
    decimal BudgetTotal,
    decimal BudgetSpent,
    DateOnly StartDate,
    DateOnly EndDate,
    string OwnerName,
    DateTime CreatedAt,
    List<ProjectTeamMemberDto> TeamMembers,
    List<ProjectNoteDto> Notes,
    List<ProjectLeadTaskDto> LeadTasks
);

public record CreateProjectRequest(string Name, string Description, string Customer, decimal BudgetTotal, DateOnly StartDate, DateOnly EndDate);
public record UpdateProjectRequest(string? Name, string? Description, string? Customer, string? Objective, string? Scope, string? SuccessMetric, string? Communication, string? NextMilestone, List<string>? Stakeholders, List<string>? Technologies, string? Status, int? ProgressPercent, decimal? BudgetTotal, decimal? BudgetSpent, DateOnly? StartDate, DateOnly? EndDate);
public record PortfolioDto(List<ProjectDto> Projects, int TotalProjects, int GreenCount, int YellowCount, int RedCount, decimal TotalBudget, decimal SpentBudget, int TotalTasks, int OverdueTasks);

public record AssignProjectTeamMemberRequest(Guid UserId, string ProjectRole, string Responsibility, int AllocatedHours);
public record UpdateProjectTeamMemberRequest(string? ProjectRole, string? Responsibility, int? AllocatedHours);

public record TaskDto(Guid Id, Guid ProjectId, string Title, string Description, string Status, string Priority, Guid? AssigneeId, string? AssigneeName, DateOnly? DueDate, int EstimatedHours, int LoggedHours, int CommentCount, DateTime CreatedAt);
public record CreateTaskRequest(string Title, string? Description, string Priority, Guid? AssigneeId, DateOnly? DueDate, int EstimatedHours);
public record UpdateTaskStatusRequest(string Status);
public record CommentDto(Guid Id, string Content, string AuthorName, DateTime CreatedAt);
public record AddCommentRequest(string Content);

public record RiskDto(Guid Id, Guid ProjectId, string Title, string Description, int Impact, int Probability, int Score, string Status, string Mitigation, string OwnerName, DateTime IdentifiedAt);
public record CreateRiskRequest(string Title, string Description, int Impact, int Probability, string Mitigation, Guid OwnerId);

public record ActivityDto(Guid Id, string UserName, string Action, string EntityType, DateTime CreatedAt);
public record TeamMemberDto(Guid Id, string Name, string Email, string Role, int AllocatedHours, int TotalCapacityHours);
public record InviteTeamMemberRequest(string Name, string Email, string Role);
public record UpdateTeamMemberRequest(string? Name, string? Role);

public record CreateProjectNoteRequest(string Title, string Content);
public record CreateProjectLeadTaskRequest(string Title, string Description, Guid? OwnerId, DateOnly DueDate);
public record UpdateProjectLeadTaskStatusRequest(string Status);

public record AiChatRequest(string Message, Guid? ProjectId);
public record AiChatResponse(string Reply);
