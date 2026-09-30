namespace SmartMicrogrid.API.Models.Common;

public static class RoleCompatibility
{
    public static Role ToStoredRole(Role role) => role;

    public static string ToAssignmentRole(Role role) => role.ToString();

    public static string ToLegacyRole(Role role) => role.ToString();
}
