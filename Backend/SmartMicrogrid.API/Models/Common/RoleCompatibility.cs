namespace SmartMicrogrid.API.Models.Common;

public static class RoleCompatibility
{
    public static Role ToStoredRole(Role role) => role switch
    {
        Role.Backoffice => Role.Admin,
        Role.GridOperator => Role.MicrogridOperator,
        _ => role
    };

    public static string ToAssignmentRole(Role role) => role switch
    {
        Role.Admin or Role.Backoffice => "Backoffice",
        Role.MicrogridOperator or Role.TransactionVerifier or Role.GridOperator => "GridOperator",
        _ => "Prosumer"
    };

    public static string ToLegacyRole(Role role) => ToStoredRole(role).ToString();
}
