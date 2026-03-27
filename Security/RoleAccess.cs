namespace PmTool.Api.Security;

public static class RoleAccess
{
    public static bool CanManagePortfolio(string? role) => HasAnyRole(role, "admin", "management", "projektleiter", "pmo");
    public static bool CanManageTeam(string? role) => HasAnyRole(role, "admin", "management", "projektleiter", "pmo");
    public static bool CanEditProject(string? role) => HasAnyRole(role, "admin", "management", "projektleiter", "pmo", "product owner");
    public static bool CanManagePmo(string? role) => HasAnyRole(role, "admin", "management", "projektleiter", "pmo");
    public static bool CanDecideApproval(string? role) => HasAnyRole(role, "admin", "management", "pmo");
    public static bool CanConfigureIntegrations(string? role) => HasAnyRole(role, "admin", "management", "projektleiter", "pmo");

    private static bool HasAnyRole(string? role, params string[] allowedRoles)
    {
        var normalized = Normalize(role);
        return allowedRoles.Select(Normalize).Contains(normalized);
    }

    private static string Normalize(string? role) => (role ?? "").Trim().ToLowerInvariant();
}
