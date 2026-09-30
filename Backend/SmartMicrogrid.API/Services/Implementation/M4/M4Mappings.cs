using System;
using SmartMicrogrid.API.DTOs.M4;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.M4;

namespace SmartMicrogrid.API.Services.Implementation.M4
{
    public static class ActivityMapper
    {
        public static SystemActivityDto ToDto(SystemActivity activity) => new()
        {
            Id = activity.Id,
            UserId = activity.UserId,
            UserName = activity.UserName,
            Role = activity.Role,
            Action = activity.Action,
            Module = activity.Module,
            Description = activity.Description,
            EntityType = activity.EntityType,
            EntityId = activity.EntityId,
            IpAddress = activity.IpAddress,
            Timestamp = activity.Timestamp,
            Status = activity.Status
        };
    }

    public static class RoleDescriptions
    {
        public static string Describe(Role role) => role switch
        {
            Role.Admin =>
                "System Administrator - full platform administration, user and role management, configuration and audit access.",
            Role.MicrogridOperator =>
                "Microgrid Operator - operates assigned microgrids, approves reservations and completes energy transfers.",
            Role.Prosumer =>
                "Prosumer - reserves and trades energy produced by own installations.",
            _ => "Unrecognised role."
        };
    }
}
