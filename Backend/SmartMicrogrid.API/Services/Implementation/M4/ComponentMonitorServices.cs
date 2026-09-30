using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartMicrogrid.API.DTOs.M4;
using SmartMicrogrid.API.Models.M1;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services.Interfaces.M4;

namespace SmartMicrogrid.API.Services.Implementation.M4
{
    /// <summary>
    /// Read-only M1 adapter. It never calls an M1 write method, so M4 cannot
    /// mutate microgrid, capacity, battery or energy-slot data.
    /// </summary>
    public class MicrogridMonitorService : IMicrogridMonitorService
    {
        private readonly IMicrogridRepository _microgridRepository;

        public MicrogridMonitorService(IMicrogridRepository microgridRepository)
        {
            _microgridRepository = microgridRepository;
        }

        public async Task<ComponentStatDto> GetCountAsync()
        {
            var nodes = await SafeFetchAsync();
            if (nodes == null)
                return Unavailable("M1 microgrid data is not reachable.");

            var active = nodes.Count(n =>
                n.IsActive && string.Equals(n.Status, "Active", StringComparison.OrdinalIgnoreCase));

            return new ComponentStatDto
            {
                Count = nodes.Count,
                Available = active,
                Unit = "nodes",
                Status = DataSourceStatus.Available,
                Note = $"{active} active of {nodes.Count} registered."
            };
        }

        public async Task<ComponentStatDto> GetCapacitySummaryAsync()
        {
            var nodes = await SafeFetchAsync();
            if (nodes == null)
                return Unavailable("M1 capacity data is not reachable.");

            return new ComponentStatDto
            {
                Count = nodes.Count,
                Total = nodes.Sum(n => n.Capacity),
                Available = nodes.Sum(n => n.AvailableCapacity),
                Unit = "kWh",
                Status = DataSourceStatus.Available,
                Note = $"Total capacity {nodes.Sum(n => n.Capacity):0.#} kWh."
            };
        }

        private async Task<List<MicrogridNode>?> SafeFetchAsync()
        {
            try
            {
                return (await _microgridRepository.GetAllAsync()).ToList();
            }
            catch
            {
                return null;
            }
        }

        private static ComponentStatDto Unavailable(string note) => new()
        {
            Count = 0,
            Available = 0,
            Unit = "-",
            Status = DataSourceStatus.Unavailable,
            Note = note
        };
    }

    /// <summary>
    /// Read-only M2 adapter.
    /// </summary>
    public class ReservationMonitorService : IReservationMonitorService
    {
        private readonly IReservationRepository _reservationRepository;

        public ReservationMonitorService(IReservationRepository reservationRepository)
        {
            _reservationRepository = reservationRepository;
        }

        public async Task<ComponentStatDto> GetCountAsync()
        {
            try
            {
                var count = await _reservationRepository.CountAsync();
                return new ComponentStatDto
                {
                    Count = count,
                    Available = count,
                    Unit = "reservations",
                    Status = DataSourceStatus.Available,
                    Note = "M2 reservation ledger."
                };
            }
            catch
            {
                return Unavailable();
            }
        }

        public async Task<ComponentStatDto> GetPendingCountAsync()
        {
            try
            {
                var count = await _reservationRepository.CountAsync("Pending");
                return new ComponentStatDto
                {
                    Count = count,
                    Available = count,
                    Unit = "pending",
                    Status = DataSourceStatus.Available,
                    Note = "Reservations awaiting operator approval."
                };
            }
            catch
            {
                return Unavailable();
            }
        }

        private static ComponentStatDto Unavailable() => new()
        {
            Count = 0,
            Available = 0,
            Unit = "-",
            Status = DataSourceStatus.Unavailable,
            Note = "M2 reservation data is not reachable."
        };
    }

    /// <summary>
    /// Read-only M3 adapter.
    /// </summary>
    public class TransactionMonitorService : ITransactionMonitorService
    {
        private readonly ITransactionRepository _transactionRepository;

        public TransactionMonitorService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<ComponentStatDto> GetCountAsync()
        {
            var transactions = await SafeFetchAsync();
            if (transactions == null)
                return Unavailable();

            return new ComponentStatDto
            {
                Count = transactions.Count,
                Available = transactions.Count(t => IsCompleted(t)),
                Unit = "transactions",
                Status = DataSourceStatus.Available,
                Note = "M3 transaction ledger."
            };
        }

        public async Task<ComponentStatDto> GetCompletedCountAsync()
        {
            var transactions = await SafeFetchAsync();
            if (transactions == null)
                return Unavailable();

            var completed = transactions.Count(t => IsCompleted(t));
            return new ComponentStatDto
            {
                Count = completed,
                Available = completed,
                Unit = "completed",
                Status = DataSourceStatus.Available,
                Note = "Transactions with energy transfer verified."
            };
        }

        private async Task<List<SmartMicrogrid.API.Models.Transactions.Transaction>?> SafeFetchAsync()
        {
            try
            {
                return await _transactionRepository.GetAllAsync();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsCompleted(SmartMicrogrid.API.Models.Transactions.Transaction transaction)
        {
            return string.Equals(transaction.Status, "Completed", StringComparison.OrdinalIgnoreCase);
        }

        private static ComponentStatDto Unavailable() => new()
        {
            Count = 0,
            Available = 0,
            Unit = "-",
            Status = DataSourceStatus.Unavailable,
            Note = "M3 transaction data is not reachable."
        };
    }
}
