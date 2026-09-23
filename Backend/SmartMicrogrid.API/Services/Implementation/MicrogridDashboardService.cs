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
            var activeCount = nodes.Count(n => n.Status == "Active");
            var inactiveCount = nodes.Count(n => n.Status == "Inactive");
            var maintenanceCount = nodes.Count(n => n.Status == "Maintenance");
            var offlineCount = nodes.Count(n => n.Status == "Offline");

            var totalCapacity = nodes.Sum(n => n.Capacity);
            var availableCapacity = nodes.Sum(n => n.AvailableCapacity);
            var reservedCapacity = nodes.Sum(n => n.ReservedCapacity);
            var usedCapacity = nodes.Sum(n => n.UsedCapacity);

            var totalBatteryCap = nodes.Sum(n => n.BatteryCapacity);
            var currentBatteryLvl = nodes.Sum(n => n.CurrentBatteryLevel);
            var avgBatteryPct = nodes.Any() ? nodes.Average(n => n.BatteryPercentage) : 0.0;

            var activeSlotsCount = await _slotRepository.GetCountAsync("Available");
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
