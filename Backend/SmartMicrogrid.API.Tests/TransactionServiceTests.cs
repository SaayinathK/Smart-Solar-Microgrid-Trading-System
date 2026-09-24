using Moq;
using SmartMicrogrid.API.DTOs.Transactions;
using SmartMicrogrid.API.Models.Transactions;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services;
using SmartMicrogrid.API.Services.Implementation;
using SmartMicrogrid.API.Services.Interfaces;
using Xunit;

namespace SmartMicrogrid.API.Tests
{
    public class TransactionServiceTests
    {
        private readonly Mock<ITransactionRepository> _transactions = new();
        private readonly Mock<IReservationApiClient> _reservations = new();
        private readonly TransactionService _service;

        public TransactionServiceTests()
        {
            _service = new TransactionService(_transactions.Object, _reservations.Object);
        }

        [Fact]
        public async Task CreateAsync_ApprovedReservation_CreatesPendingTransaction()
        {
            _reservations.Setup(x => x.GetReservationAsync("r1"))
                .ReturnsAsync(Reservation("Approved"));
            _transactions.Setup(x => x.GetByReservationIdAsync("r1"))
                .ReturnsAsync((Transaction?)null);
            _transactions.Setup(x => x.CreateAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction value) =>
                {
                    value.Id = "507f1f77bcf86cd799439011";
                    return value;
                });

            var result = await _service.CreateAsync(new CreateTransactionRequest { ReservationId = "r1" }, "verifier");

            Assert.Equal("Pending", result.Status);
            Assert.Equal("r1", result.ReservationId);
            _transactions.Verify(x => x.CreateAsync(It.IsAny<Transaction>()), Times.Once);
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Rejected")]
        [InlineData("Cancelled")]
        [InlineData("Expired")]
        [InlineData("Completed")]
        public async Task CreateAsync_NonApprovedReservation_IsRejected(string status)
        {
            _reservations.Setup(x => x.GetReservationAsync("r1"))
                .ReturnsAsync(Reservation(status));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateAsync(new CreateTransactionRequest { ReservationId = "r1" }, "verifier"));

            _transactions.Verify(x => x.CreateAsync(It.IsAny<Transaction>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ExistingTransaction_ReturnsConflict()
        {
            _reservations.Setup(x => x.GetReservationAsync("r1"))
                .ReturnsAsync(Reservation("Approved"));
            _transactions.Setup(x => x.GetByReservationIdAsync("r1"))
                .ReturnsAsync(new Transaction { Id = "507f1f77bcf86cd799439011", ReservationId = "r1" });

            var error = await Assert.ThrowsAsync<TransactionConflictException>(() =>
                _service.CreateAsync(new CreateTransactionRequest { ReservationId = "r1" }, "verifier"));

            Assert.Equal("A transaction already exists for this reservation.", error.Message);
        }

        [Fact]
        public async Task GenerateQrAsync_PendingTransaction_UsesRequiredPayloadAndStatus()
        {
            var transaction = new Transaction
            {
                Id = "507f1f77bcf86cd799439011",
                ReservationId = "r1",
                Status = "Pending"
            };
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            _transactions.Setup(x => x.UpdateAsync(transaction)).ReturnsAsync(transaction);

            var result = await _service.GenerateQrAsync(transaction.Id, "verifier");

            Assert.NotNull(result);
            Assert.StartsWith($"SMART-MICROGRID|TRANSACTION|{transaction.Id}|", result!.QrCodeData);
            Assert.Equal("QRGenerated", result.Status);
        }

        private static ReservationDto Reservation(string status) => new()
        {
            Id = "r1",
            ProsumerId = "NIC123",
            MicrogridNodeId = "grid1",
            EnergySlotId = "slot1",
            EnergyAmount = 25,
            Status = status
        };
    }
}
