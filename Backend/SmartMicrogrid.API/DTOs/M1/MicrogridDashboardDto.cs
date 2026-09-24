namespace SmartMicrogrid.API.DTOs.M1
{
    public class MicrogridDashboardDto
    {
        public long TotalMicrogrids { get; set; }
        public long ActiveMicrogrids { get; set; }
        public long InactiveMicrogrids { get; set; }
        public long MaintenanceMicrogrids { get; set; }
        public long OfflineMicrogrids { get; set; }
        public double TotalCapacity { get; set; }
        public double TotalAvailableCapacity { get; set; }
        public double TotalReservedCapacity { get; set; }
        public double TotalUsedCapacity { get; set; }
        public double TotalBatteryCapacity { get; set; }
        public double CurrentBatteryLevel { get; set; }
        public double AverageBatteryPercentage { get; set; }
        public long ActiveEnergySlots { get; set; }
        public double TotalAvailableEnergy { get; set; }
    }
}
