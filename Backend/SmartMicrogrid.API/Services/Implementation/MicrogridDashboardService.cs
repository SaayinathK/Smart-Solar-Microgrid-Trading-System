using System.Linq;
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M1;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services.Interfaces;

namespace SmartMicrogrid.API.Services.Implementation
{
    public class MicrogridDashboardService : IMicrogridDashboardService
    {
        private readonly IMicrogridRepository _microgridRepository;
        private readonly IEnergySlotRepository _slotRepository;

        public MicrogridDashboardService(IMicrogridRepository microgridRepository, IEnergySlotRepository slotRepository)
        {
            _microgridRepository = microgridRepository;
            _slotRepository = slotRepository;
        }

        public async Task<MicrogridDashboardDto> GetDashboardStatsAsync()
        {
            var nodes = (await _microgridRepository.GetAllAsync()).ToList();

            var totalCount = nodes.Count;
            var activeCount = nodes.Count(n => n.IsActive && n.Status.Equals("Active", System.StringComparison.OrdinalIgnoreCase));
            var inactiveCount = nodes.Count(n => n.Status.Equals("Inactive", System.StringComparison.OrdinalIgnoreCase));
            var maintenanceCount = nodes.Count(n => n.Status.Equals("Maintenance", System.StringComparison.OrdinalIgnoreCase));
            var offlineCount = nodes.Count(n => n.Status.Equals("Offline", System.StringComparison.OrdinalIgnoreCase));

            var totalCapacity = nodes.Sum(n => n.Capacity);
            var availableCapacity = nodes.Sum(n => n.AvailableCapacity);
            var reservedCapacity = nodes.Sum(n => n.ReservedCapacity);
            var usedCapacity = nodes.Sum(n => n.UsedCapacity);

            var totalBatteryCap = nodes.Sum(n => n.BatteryCapacity);
            var currentBatteryLvl = nodes.Sum(n => n.CurrentBatteryLevel);
            var avgBatteryPct = nodes.Any() ? nodes.Average(n => n.BatteryPercentage) : 0.0;

            var slots = (await _slotRepository.GetAllAsync()).ToList();
            var activeNodeIds = nodes.Where(n => n.IsActive && n.Status.Equals("Active", System.StringComparison.OrdinalIgnoreCase)).Select(n => n.Id).ToHashSet();
            var activeSlotsCount = slots.Count(s => activeNodeIds.Contains(s.MicrogridNodeId) && (s.Status == "Available" || s.Status == "PartiallyReserved") && s.AvailableAmount > 0 && s.StartTime > System.DateTime.UtcNow && s.EndTime > System.DateTime.UtcNow);
            var availableEnergy = await _slotRepository.GetTotalAvailableEnergyAsync();

            return new MicrogridDashboardDto
            {
                TotalMicrogrids = totalCount,
                ActiveMicrogrids = activeCount,
                InactiveMicrogrids = inactiveCount,
                MaintenanceMicrogrids = maintenanceCount,
                OfflineMicrogrids = offlineCount,
                TotalCapacity = totalCapacity,
                TotalAvailableCapacity = availableCapacity,
                TotalReservedCapacity = reservedCapacity,
                TotalUsedCapacity = usedCapacity,
                TotalBatteryCapacity = totalBatteryCap,
                CurrentBatteryLevel = currentBatteryLvl,
                AverageBatteryPercentage = avgBatteryPct,
                ActiveEnergySlots = activeSlotsCount,
                TotalAvailableEnergy = availableEnergy
            };
        }
    }
}
