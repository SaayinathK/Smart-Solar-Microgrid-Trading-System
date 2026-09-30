namespace SmartMicrogrid.API.Models.M4
{
    /// <summary>
    /// Canonical audit action names. Kept as constants so the Web UI filters,
    /// the Postman collection and the documentation cannot drift apart.
    /// </summary>
    public static class AuditStatus
    {
        public const string Success = "Success";
        public const string Failure = "Failure";
    }

    public static class AuditAction
    {
        public const string LoginSuccess = "LOGIN_SUCCESS";
        public const string LoginFailed = "LOGIN_FAILED";

        public const string UserCreated = "USER_CREATED";
        public const string UserUpdated = "USER_UPDATED";
        public const string UserActivated = "USER_ACTIVATED";
        public const string UserDeactivated = "USER_DEACTIVATED";
        public const string UserSuspended = "USER_SUSPENDED";
        public const string UserReactivated = "USER_REACTIVATED";
        public const string UserStatusChanged = "USER_STATUS_CHANGED";
        public const string RoleAssigned = "ROLE_ASSIGNED";
        public const string RoleChanged = "ROLE_CHANGED";
        public const string UserDeleted = "USER_DELETED";

        public const string ConfigurationUpdated = "SYSTEM_CONFIGURATION_UPDATED";
        public const string ReportGenerated = "ADMIN_REPORT_GENERATED";
    }

    public static class AuditModule
    {
        public const string Authentication = "Authentication";
        public const string UserManagement = "User Management";
        public const string SystemConfiguration = "System Configuration";
        public const string Reporting = "Administrative Reports";
    }
}
