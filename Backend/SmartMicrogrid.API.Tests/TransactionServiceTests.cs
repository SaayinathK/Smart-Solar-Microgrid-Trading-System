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
            Assert.Equal("NIC123", result.ProsumerId);
            Assert.Equal("grid1", result.MicrogridNodeId);
            Assert.Equal("slot1", result.EnergySlotId);
            Assert.Equal(25, result.EnergyAmount);
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
        public async Task CreateAsync_MissingReservation_IsNotFound()
        {
            _reservations.Setup(x => x.GetReservationAsync("missing"))
                .ReturnsAsync((ReservationDto?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CreateAsync(new CreateTransactionRequest { ReservationId = "missing" }, "verifier"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public async Task CreateAsync_InvalidRequest_IsRejected(string? reservationId)
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync(new CreateTransactionRequest { ReservationId = reservationId! }, "verifier"));
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
            Assert.Equal($"SMART-MICROGRID|TRANSACTION|{transaction.Id}|{result!.TransactionCode}", result.QrCodeData);
            Assert.Equal("QRGenerated", result.Status);
            Assert.Equal(transaction.Id, result.TransactionId);
            Assert.Equal("r1", result.ReservationId);
            Assert.Equal(transaction.QrCodeData, result.QrCodeData);
            Assert.NotEmpty(result.TransactionCode);
        }

        [Theory]
        [InlineData("QRGenerated")]
        [InlineData("VerificationPending")]
        [InlineData("Verified")]
        [InlineData("EnergyTransferInProgress")]
        [InlineData("Completed")]
        [InlineData("Rejected")]
        [InlineData("Cancelled")]
        public async Task GenerateQrAsync_NonPendingState_IsRejected(string status)
        {
            var transaction = Transaction(status: status);
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GenerateQrAsync(transaction.Id, "verifier"));
            _transactions.Verify(x => x.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
        }

        [Fact]
        public async Task GenerateQrAsync_MissingTransaction_ReturnsNull()
        {
            _transactions.Setup(x => x.GetByIdAsync("missing")).ReturnsAsync((Transaction?)null);
            Assert.Null(await _service.GenerateQrAsync("missing", "verifier"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GenerateQrAsync_InvalidId_IsRejected(string? id)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateQrAsync(id!, "verifier"));
        }

        [Fact]
        public async Task VerifyAsync_CorrectQr_RecordsVerifierTimeAndVerifiedStatus()
        {
            var transaction = Transaction(status: "QRGenerated");
            transaction.QrCodeData = "SMART-MICROGRID|TRANSACTION|tx1|code1";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            _transactions.Setup(x => x.UpdateAsync(transaction)).ReturnsAsync(transaction);
            _reservations.Setup(x => x.GetReservationAsync("r1")).ReturnsAsync(Reservation("Approved"));

            var result = await _service.VerifyAsync(transaction.Id,
                new VerifyTransactionRequest { QrCodeData = transaction.QrCodeData }, "verifier1");

            Assert.NotNull(result);
            Assert.Equal("Verified", result!.Status);
            Assert.Equal("verifier1", result.VerifiedBy);
            Assert.NotNull(result.VerificationTime);
        }

        [Fact]
        public async Task VerifyAsync_WrongQr_IsRejected()
        {
            var transaction = Transaction(status: "QRGenerated");
            transaction.QrCodeData = "valid-qr";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyAsync(transaction.Id,
                new VerifyTransactionRequest { QrCodeData = "wrong-qr" }, "verifier1"));
        }

        [Fact]
        public async Task VerifyAsync_EmptyQr_IsRejected()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.VerifyAsync("tx1",
                new VerifyTransactionRequest { QrCodeData = "" }, "verifier1"));
        }

        [Fact]
        public async Task VerifyAsync_NullRequest_IsRejected()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.VerifyAsync("tx1", null!, "verifier1"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task VerifyAsync_InvalidId_IsRejected(string? id)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.VerifyAsync(id!,
                new VerifyTransactionRequest { QrCodeData = "qr" }, "verifier1"));
        }

        [Fact]
        public async Task VerifyAsync_QrForDifferentTransaction_IsRejected()
        {
            var transaction = Transaction(status: "QRGenerated");
            transaction.QrCodeData = "qr-for-tx1";
            _transactions.Setup(x => x.GetByIdAsync("tx2")).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyAsync("tx2",
                new VerifyTransactionRequest { QrCodeData = "qr-for-tx2" }, "verifier1"));
        }

        [Fact]
        public async Task VerifyAsync_TransactionHasNoQr_IsRejected()
        {
            var transaction = Transaction(status: "QRGenerated");
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyAsync(transaction.Id,
                new VerifyTransactionRequest { QrCodeData = "some-qr" }, "verifier1"));
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Verified")]
        [InlineData("EnergyTransferInProgress")]
        [InlineData("Completed")]
        [InlineData("Rejected")]
        [InlineData("Cancelled")]
        public async Task VerifyAsync_InvalidLifecycleState_IsRejected(string status)
        {
            var transaction = Transaction(status: status);
            transaction.QrCodeData = "valid-qr";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyAsync(transaction.Id,
                new VerifyTransactionRequest { QrCodeData = "valid-qr" }, "verifier1"));
            _reservations.Verify(x => x.GetReservationAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task VerifyAsync_MissingReservation_IsRejectedAndTransactionRejected()
        {
            var transaction = Transaction(status: "QRGenerated");
            transaction.QrCodeData = "valid-qr";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            _transactions.Setup(x => x.UpdateAsync(transaction)).ReturnsAsync(transaction);
            _reservations.Setup(x => x.GetReservationAsync("r1")).ReturnsAsync((ReservationDto?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyAsync(transaction.Id,
                new VerifyTransactionRequest { QrCodeData = "valid-qr" }, "verifier1"));
            Assert.Equal("Rejected", transaction.Status);
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Rejected")]
        [InlineData("Cancelled")]
        [InlineData("Expired")]
        [InlineData("Completed")]
        public async Task VerifyAsync_ReservationNoLongerApproved_IsRejected(string status)
        {
            var transaction = Transaction(status: "QRGenerated");
            transaction.QrCodeData = "valid-qr";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            _transactions.Setup(x => x.UpdateAsync(transaction)).ReturnsAsync(transaction);
            _reservations.Setup(x => x.GetReservationAsync("r1")).ReturnsAsync(Reservation(status));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyAsync(transaction.Id,
                new VerifyTransactionRequest { QrCodeData = "valid-qr" }, "verifier1"));
            Assert.Equal("Rejected", transaction.Status);
        }

        [Theory]
        [InlineData("prosumer")]
        [InlineData("microgrid")]
        [InlineData("slot")]
        [InlineData("amount")]
        public async Task VerifyAsync_ReservationDataMismatch_IsRejected(string field)
        {
            var reservation = Reservation("Approved");
            switch (field)
            {
                case "prosumer": reservation.ProsumerId = "other"; break;
                case "microgrid": reservation.MicrogridNodeId = "other"; break;
                case "slot": reservation.EnergySlotId = "other"; break;
                case "amount": reservation.EnergyAmount = 30; break;
            }
            var transaction = Transaction(status: "QRGenerated");
            transaction.QrCodeData = "valid-qr";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            _transactions.Setup(x => x.UpdateAsync(transaction)).ReturnsAsync(transaction);
            _reservations.Setup(x => x.GetReservationAsync("r1")).ReturnsAsync(reservation);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyAsync(transaction.Id,
                new VerifyTransactionRequest { QrCodeData = "valid-qr" }, "verifier1"));
            Assert.Equal("Rejected", transaction.Status);
        }

        [Fact]
        public async Task VerifyAsync_MissingTransaction_IsNotFound()
        {
            _transactions.Setup(x => x.GetByIdAsync("missing")).ReturnsAsync((Transaction?)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.VerifyAsync("missing",
                new VerifyTransactionRequest { QrCodeData = "qr" }, "verifier1"));
        }

        [Fact]
        public async Task CompleteAsync_VerifiedTransaction_CompletesAndRecordsTransferTime()
        {
            var transaction = Transaction(status: "Verified");
            transaction.VerifiedBy = "verifier1";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            _transactions.Setup(x => x.UpdateAsync(transaction)).ReturnsAsync(transaction);

            var result = await _service.CompleteAsync(transaction.Id,
                new CompleteTransactionRequest { Confirmation = "CONFIRMED" }, "verifier1");

            Assert.NotNull(result);
            Assert.Equal("Completed", result!.Status);
            Assert.NotNull(result.EnergyTransferTime);
            _transactions.Verify(x => x.UpdateAsync(transaction), Times.Exactly(2));
            _reservations.Verify(x => x.GetReservationAsync(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("QRGenerated")]
        [InlineData("VerificationPending")]
        [InlineData("Rejected")]
        [InlineData("Cancelled")]
        [InlineData("Completed")]
        public async Task CompleteAsync_NonVerifiableState_IsRejected(string status)
        {
            var transaction = Transaction(status: status);
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteAsync(transaction.Id,
                new CompleteTransactionRequest { Confirmation = "CONFIRMED" }, "verifier1"));
            _transactions.Verify(x => x.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
        }

        [Fact]
        public async Task CompleteAsync_InvalidConfirmation_IsRejected()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CompleteAsync("tx1",
                new CompleteTransactionRequest { Confirmation = "no" }, "verifier1"));
            _transactions.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CompleteAsync_NullRequest_IsRejected()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CompleteAsync("tx1", null!, "verifier1"));
        }

        [Fact]
        public async Task CompleteAsync_MissingTransaction_ReturnsNull()
        {
            _transactions.Setup(x => x.GetByIdAsync("missing")).ReturnsAsync((Transaction?)null);
            Assert.Null(await _service.CompleteAsync("missing",
                new CompleteTransactionRequest { Confirmation = "CONFIRMED" }, "verifier1"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task CompleteAsync_InvalidId_IsRejected(string? id)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CompleteAsync(id!,
                new CompleteTransactionRequest { Confirmation = "CONFIRMED" }, "verifier1"));
        }

        [Fact]
        public async Task CompleteAsync_DifferentVerifier_IsForbidden()
        {
            var transaction = Transaction(status: "Verified");
            transaction.VerifiedBy = "verifier1";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.CompleteAsync(transaction.Id,
                new CompleteTransactionRequest { Confirmation = "CONFIRMED" }, "verifier2"));
            _transactions.Verify(x => x.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
        }

        [Theory]
        [InlineData("pending", "QRGenerated")]
        [InlineData("QRGenerated", "VerificationPending")]
        [InlineData("QRGenerated", "Verified")]
        [InlineData("VerificationPending", "Verified")]
        [InlineData("Verified", "EnergyTransferInProgress")]
        [InlineData("EnergyTransferInProgress", "Completed")]
        public async Task UpdateStatusAsync_WorkflowControlledTargets_CannotBeSetGenerically(string current, string requested)
        {
            var transaction = Transaction(status: current);
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateStatusAsync(transaction.Id, requested, "verifier1"));
        }

        [Theory]
        [InlineData("Completed", "Pending")]
        [InlineData("Rejected", "Pending")]
        [InlineData("Cancelled", "Pending")]
        [InlineData("Pending", "Completed")]
        [InlineData("Pending", "Verified")]
        [InlineData("QRGenerated", "Completed")]
        public async Task UpdateStatusAsync_InvalidTransitions_AreRejected(string current, string requested)
        {
            var transaction = Transaction(status: current);
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateStatusAsync(transaction.Id, requested, "verifier1"));
        }

        [Fact]
        public async Task UpdateStatusAsync_InvalidStatus_IsRejected()
        {
            var transaction = Transaction(status: "Pending");
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateStatusAsync(transaction.Id, "Unknown", "verifier1"));
        }

        [Theory]
        [InlineData("Pending", "Cancelled")]
        [InlineData("QRGenerated", "Rejected")]
        [InlineData("QRGenerated", "Cancelled")]
        [InlineData("VerificationPending", "Rejected")]
        [InlineData("VerificationPending", "Cancelled")]
        [InlineData("Verified", "Rejected")]
        [InlineData("Verified", "Cancelled")]
        [InlineData("EnergyTransferInProgress", "Rejected")]
        public async Task UpdateStatusAsync_AllowedTerminalTransition_Succeeds(string current, string requested)
        {
            var transaction = Transaction(status: current);
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            _transactions.Setup(x => x.UpdateAsync(transaction)).ReturnsAsync(transaction);

            var result = await _service.UpdateStatusAsync(transaction.Id, requested, "verifier1");

            Assert.NotNull(result);
            Assert.Equal(requested, result!.Status);
        }

        [Fact]
        public async Task UpdateStatusAsync_EmptyIdOrStatus_IsRejected()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateStatusAsync("", "Cancelled", "v"));
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateStatusAsync("tx1", "", "v"));
        }

        [Fact]
        public async Task GetByIdAsync_ProsumerCannotReadAnotherOwnersTransaction()
        {
            var transaction = Transaction(status: "Pending");
            transaction.ProsumerId = "owner";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetByIdAsync(transaction.Id, "other", "Prosumer"));
        }

        [Fact]
        public async Task GetByIdAsync_UnknownRoleIsForbidden()
        {
            var transaction = Transaction(status: "Pending");
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetByIdAsync(transaction.Id, "user", "UnknownRole"));
        }

        [Fact]
        public async Task RepositoryFailure_IsPropagated()
        {
            _transactions.Setup(x => x.GetByIdAsync("tx1"))
                .ThrowsAsync(new InvalidOperationException("repository failure"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GetByIdAsync("tx1", "user", "Admin"));
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

        private static Transaction Transaction(string status) => new()
        {
            Id = "507f1f77bcf86cd799439011",
            ReservationId = "r1",
            ProsumerId = "NIC123",
            MicrogridNodeId = "grid1",
            EnergySlotId = "slot1",
            EnergyAmount = 25,
            Status = status
        };
    }
}
