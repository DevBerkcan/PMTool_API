namespace PmTool.Api.Security;

public static class RoleCatalog
{
    public static readonly string[] AvailableRoles =
    [
        "Admin",
        "Management",
        "PMO",
        "Projektleiter",
        "Product Owner",
        "Entwickler",
        "QA Engineer",
        "Business Analyst",
        "Designer",
        "Trainer"
    ];

    public static bool IsValidRole(string? role) => AvailableRoles.Contains(role?.Trim() ?? "", StringComparer.OrdinalIgnoreCase);

    public static Dictionary<string, bool> BuildPermissionMatrix(string? role) => new()
    {
        ["managePortfolio"] = RoleAccess.CanManagePortfolio(role),
        ["manageTeam"] = RoleAccess.CanManageTeam(role),
        ["editProject"] = RoleAccess.CanEditProject(role),
        ["managePmo"] = RoleAccess.CanManagePmo(role),
        ["decideApproval"] = RoleAccess.CanDecideApproval(role),
        ["configureIntegrations"] = RoleAccess.CanConfigureIntegrations(role),
    };
}
