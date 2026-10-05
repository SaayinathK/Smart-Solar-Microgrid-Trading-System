// ===========================================================================================================
// File: TransactionServiceTests.cs
// Project: Smart Solar Microgrid Trading System
// Module: M3 – Transaction & Verification Management
// Section Owned: M3 – Transaction & Verification Management
// Author: J. Jathusan (IT23297418)
// Description: Unit/Integration test suite verifying TransactionService operations and validations.
// ===========================================================================================================
using Moq;
using SmartMicrogrid.API.DTOs.M2;
using SmartMicrogrid.API.DTOs.Transactions;
using SmartMicrogrid.API.Models.Common;
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
        private readonly Mock<IReservationService> _reservations = new();
        private readonly Mock<IUserRepository> _users = new();
        private readonly TransactionService _service;
        /// <summary>
        /// Initializes a new instance of the TransactionServiceTests class.
        /// </summary>

        public TransactionServiceTests()
        {
            // Initialize dependencies and state
            _users.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new User { Id = id, Role = Role.MicrogridOperator });
            _reservations.Setup(x => x.GetAssignedMicrogridIdsAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "grid1" });
            _service = new TransactionService(_transactions.Object, _reservations.Object, _users.Object);
        }
        /// <summary>
        /// Creates or registers a new async_approved reservation_creates pending transaction record.
        /// </summary>

        [Fact]
        public async Task CreateAsync_ApprovedReservation_CreatesPendingTransaction()
        {
            // Execute create async_approved reservation_creates pending transaction operations
            _reservations.Setup(x => x.GetByIdAsync("r1", "operator1"))
                .ReturnsAsync(Reservation("Approved"));
            _transactions.Setup(x => x.GetByReservationIdAsync("r1"))
                .ReturnsAsync((Transaction?)null);
            _transactions.Setup(x => x.CreateAsync(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction value) =>
                {
                    value.Id = "507f1f77bcf86cd799439011";
                    return value;
                });

            var result = await _service.CreateAsync(new CreateTransactionRequest { ReservationId = "r1" }, "operator1", "operator1");

            Assert.Equal("Pending", result.Status);
            Assert.Equal("r1", result.ReservationId);
            Assert.Equal("NIC123", result.ProsumerId);
            Assert.Equal("grid1", result.MicrogridNodeId);
            Assert.Equal("slot1", result.EnergySlotId);
            Assert.Equal(25, result.EnergyAmount);
            _transactions.Verify(x => x.CreateAsync(It.IsAny<Transaction>()), Times.Once);
        }
        /// <summary>
        /// Creates or registers a new async_non approved reservation_is rejected record.
        /// </summary>

        [Theory]
        [InlineData("Pending")]
        [InlineData("Rejected")]
        [InlineData("Cancelled")]
        [InlineData("Expired")]
        [InlineData("Completed")]
        public async Task CreateAsync_NonApprovedReservation_IsRejected(string status)
        {
            // Execute create async_non approved reservation_is rejected operations
            _reservations.Setup(x => x.GetByIdAsync("r1", "operator1"))
                .ReturnsAsync(Reservation(status));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateAsync(new CreateTransactionRequest { ReservationId = "r1" }, "operator1", "operator1"));

            _transactions.Verify(x => x.CreateAsync(It.IsAny<Transaction>()), Times.Never);
        }
        /// <summary>
        /// Creates or registers a new async_existing transaction_returns conflict record.
        /// </summary>

        [Fact]
        public async Task CreateAsync_ExistingTransaction_ReturnsConflict()
        {
            // Execute create async_existing transaction_returns conflict operations
            _reservations.Setup(x => x.GetByIdAsync("r1", "operator1"))
                .ReturnsAsync(Reservation("Approved"));
            _transactions.Setup(x => x.GetByReservationIdAsync("r1"))
                .ReturnsAsync(new Transaction { Id = "507f1f77bcf86cd799439011", ReservationId = "r1" });

            var error = await Assert.ThrowsAsync<TransactionConflictException>(() =>
                _service.CreateAsync(new CreateTransactionRequest { ReservationId = "r1" }, "operator1", "operator1"));

            Assert.Equal("A transaction already exists for this reservation.", error.Message);
        }
        /// <summary>
        /// Creates or registers a new async_missing reservation_is not found record.
        /// </summary>

        [Fact]
        public async Task CreateAsync_MissingReservation_IsNotFound()
        {
            // Execute create async_missing reservation_is not found operations
            _reservations.Setup(x => x.GetByIdAsync("missing", "operator1"))
                .ReturnsAsync((ReservationResponseDto?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.CreateAsync(new CreateTransactionRequest { ReservationId = "missing" }, "operator1", "operator1"));
        }
        /// <summary>
        /// Creates or registers a new async_invalid request_is rejected record.
        /// </summary>

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public async Task CreateAsync_InvalidRequest_IsRejected(string? reservationId)
        {
            // Execute create async_invalid request_is rejected operations
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateAsync(new CreateTransactionRequest { ReservationId = reservationId! }, "operator1", "operator1"));
        }
        /// <summary>
        /// Performs generate qr async_pending transaction_uses required payload and status operation.
        /// </summary>

        [Fact]
        public async Task GenerateQrAsync_PendingTransaction_UsesRequiredPayloadAndStatus()
        {
            // Execute generate qr async_pending transaction_uses required payload and status operations
            var transaction = new Transaction
            {
                Id = "507f1f77bcf86cd799439011",
                ReservationId = "r1",
                MicrogridNodeId = "grid1",
                Status = "Pending"
            };
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            _transactions.Setup(x => x.UpdateAsync(transaction)).ReturnsAsync(transaction);

            var result = await _service.GenerateQrAsync(transaction.Id, "operator1");

            Assert.NotNull(result);
            Assert.Equal($"SMART-MICROGRID|TRANSACTION|{transaction.Id}|{result!.TransactionCode}", result.QrCodeData);
            Assert.Equal("QRGenerated", result.Status);
            Assert.Equal(transaction.Id, result.TransactionId);
            Assert.Equal("r1", result.ReservationId);
            Assert.Equal(transaction.QrCodeData, result.QrCodeData);
            Assert.NotEmpty(result.TransactionCode);
        }
        /// <summary>
        /// Performs generate qr async_non pending state_is rejected operation.
        /// </summary>

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
            // Execute generate qr async_non pending state_is rejected operations
            var transaction = Transaction(status: status);
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GenerateQrAsync(transaction.Id, "operator1"));
            _transactions.Verify(x => x.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
        }
        /// <summary>
        /// Performs generate qr async_missing transaction_returns null operation.
        /// </summary>

        [Fact]
        public async Task GenerateQrAsync_MissingTransaction_ReturnsNull()
        {
            // Execute generate qr async_missing transaction_returns null operations
            _transactions.Setup(x => x.GetByIdAsync("missing")).ReturnsAsync((Transaction?)null);
            Assert.Null(await _service.GenerateQrAsync("missing", "operator1"));
        }
        /// <summary>
        /// Performs generate qr async_invalid id_is rejected operation.
        /// </summary>

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GenerateQrAsync_InvalidId_IsRejected(string? id)
        {
            // Execute generate qr async_invalid id_is rejected operations
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateQrAsync(id!, "operator1"));
        }
        /// <summary>
        /// Verifies and validates async_correct qr_records verifier time and verified status criteria.
        /// </summary>

        [Fact]
        public async Task VerifyAsync_CorrectQr_RecordsVerifierTimeAndVerifiedStatus()
        {
            // Execute verify async_correct qr_records verifier time and verified status operations
            var transaction = Transaction(status: "QRGenerated");
            transaction.QrCodeData = "SMART-MICROGRID|TRANSACTION|tx1|code1";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            _transactions.Setup(x => x.UpdateAsync(transaction)).ReturnsAsync(transaction);
            _reservations.Setup(x => x.GetByIdAsync("r1", "verifier1")).ReturnsAsync(Reservation("Approved"));

            var result = await _service.VerifyAsync(transaction.Id,
                new VerifyTransactionRequest { QrCodeData = transaction.QrCodeData }, "verifier1", "verifier1");

            Assert.NotNull(result);
            Assert.Equal("Verified", result!.Status);
            Assert.Equal("verifier1", result.VerifiedBy);
            Assert.NotNull(result.VerificationTime);
        }
        /// <summary>
        /// Verifies and validates async_wrong qr_is rejected criteria.
        /// </summary>

        [Fact]
        public async Task VerifyAsync_WrongQr_IsRejected()
        {
            // Execute verify async_wrong qr_is rejected operations
            var transaction = Transaction(status: "QRGenerated");
            transaction.QrCodeData = "valid-qr";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyAsync(transaction.Id,
                new VerifyTransactionRequest { QrCodeData = "wrong-qr" }, "verifier1", "verifier1"));
        }
        /// <summary>
        /// Verifies and validates async_empty qr_is rejected criteria.
        /// </summary>

        [Fact]
        public async Task VerifyAsync_EmptyQr_IsRejected()
        {
            // Execute verify async_empty qr_is rejected operations
            await Assert.ThrowsAsync<ArgumentException>(() => _service.VerifyAsync("tx1",
                new VerifyTransactionRequest { QrCodeData = "" }, "verifier1", "verifier1"));
        }
        /// <summary>
        /// Verifies and validates async_null request_is rejected criteria.
        /// </summary>

        [Fact]
        public async Task VerifyAsync_NullRequest_IsRejected()
        {
            // Execute verify async_null request_is rejected operations
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.VerifyAsync("tx1", null!, "verifier1", "verifier1"));
        }
        /// <summary>
        /// Verifies and validates async_invalid id_is rejected criteria.
        /// </summary>

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task VerifyAsync_InvalidId_IsRejected(string? id)
        {
            // Execute verify async_invalid id_is rejected operations
            await Assert.ThrowsAsync<ArgumentException>(() => _service.VerifyAsync(id!,
                new VerifyTransactionRequest { QrCodeData = "qr" }, "verifier1", "verifier1"));
        }
        /// <summary>
        /// Verifies and validates async_qr for different transaction_is rejected criteria.
        /// </summary>

        [Fact]
        public async Task VerifyAsync_QrForDifferentTransaction_IsRejected()
        {
            // Execute verify async_qr for different transaction_is rejected operations
            var transaction = Transaction(status: "QRGenerated");
            transaction.QrCodeData = "qr-for-tx1";
            _transactions.Setup(x => x.GetByIdAsync("tx2")).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyAsync("tx2",
                new VerifyTransactionRequest { QrCodeData = "qr-for-tx2" }, "verifier1", "verifier1"));
        }
        /// <summary>
        /// Verifies and validates async_transaction has no qr_is rejected criteria.
        /// </summary>

        [Fact]
        public async Task VerifyAsync_TransactionHasNoQr_IsRejected()
        {
            // Execute verify async_transaction has no qr_is rejected operations
            var transaction = Transaction(status: "QRGenerated");
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyAsync(transaction.Id,
                new VerifyTransactionRequest { QrCodeData = "some-qr" }, "verifier1", "verifier1"));
        }
        /// <summary>
        /// Verifies and validates async_invalid lifecycle state_is rejected criteria.
        /// </summary>

        [Theory]
        [InlineData("Pending")]
        [InlineData("Verified")]
        [InlineData("EnergyTransferInProgress")]
        [InlineData("Completed")]
        [InlineData("Rejected")]
        [InlineData("Cancelled")]
        public async Task VerifyAsync_InvalidLifecycleState_IsRejected(string status)
        {
            // Execute verify async_invalid lifecycle state_is rejected operations
            var transaction = Transaction(status: status);
            transaction.QrCodeData = "valid-qr";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyAsync(transaction.Id,
                new VerifyTransactionRequest { QrCodeData = "valid-qr" }, "verifier1", "verifier1"));
            _reservations.Verify(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        /// <summary>
        /// Verifies and validates async_missing reservation_is rejected and transaction rejected criteria.
        /// </summary>

        [Fact]
        public async Task VerifyAsync_MissingReservation_IsRejectedAndTransactionRejected()
        {
            // Execute verify async_missing reservation_is rejected and transaction rejected operations
            var transaction = Transaction(status: "QRGenerated");
            transaction.QrCodeData = "valid-qr";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            _transactions.Setup(x => x.UpdateAsync(transaction)).ReturnsAsync(transaction);
            _reservations.Setup(x => x.GetByIdAsync("r1", "verifier1")).ReturnsAsync((ReservationResponseDto?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyAsync(transaction.Id,
                new VerifyTransactionRequest { QrCodeData = "valid-qr" }, "verifier1", "verifier1"));
            Assert.Equal("Rejected", transaction.Status);
        }
        /// <summary>
        /// Verifies and validates async_reservation no longer approved_is rejected criteria.
        /// </summary>

        [Theory]
        [InlineData("Pending")]
        [InlineData("Rejected")]
        [InlineData("Cancelled")]
        [InlineData("Expired")]
        [InlineData("Completed")]
        public async Task VerifyAsync_ReservationNoLongerApproved_IsRejected(string status)
        {
            // Execute verify async_reservation no longer approved_is rejected operations
            var transaction = Transaction(status: "QRGenerated");
            transaction.QrCodeData = "valid-qr";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            _transactions.Setup(x => x.UpdateAsync(transaction)).ReturnsAsync(transaction);
            _reservations.Setup(x => x.GetByIdAsync("r1", "verifier1")).ReturnsAsync(Reservation(status));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyAsync(transaction.Id,
                new VerifyTransactionRequest { QrCodeData = "valid-qr" }, "verifier1", "verifier1"));
            Assert.Equal("Rejected", transaction.Status);
        }
        /// <summary>
        /// Verifies and validates async_approved but expired reservation_is rejected criteria.
        /// </summary>

        [Fact]
        public async Task VerifyAsync_ApprovedButExpiredReservation_IsRejected()
        {
            // Execute verify async_approved but expired reservation_is rejected operations
            var transaction = Transaction(status: "QRGenerated");
            transaction.QrCodeData = "valid-qr";
            var reservation = Reservation("Approved");
            reservation.EndTime = DateTime.UtcNow.AddSeconds(-1);
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            _reservations.Setup(x => x.GetByIdAsync("r1", "verifier1")).ReturnsAsync(reservation);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyAsync(transaction.Id,
                new VerifyTransactionRequest { QrCodeData = "valid-qr" }, "verifier1", "verifier1"));
        }
        /// <summary>
        /// Verifies and validates async_reservation data mismatch_is rejected criteria.
        /// </summary>

        [Theory]
        [InlineData("prosumer")]
        [InlineData("microgrid")]
        [InlineData("slot")]
        [InlineData("amount")]
        public async Task VerifyAsync_ReservationDataMismatch_IsRejected(string field)
        {
            // Execute verify async_reservation data mismatch_is rejected operations
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
            _reservations.Setup(x => x.GetByIdAsync("r1", "verifier1")).ReturnsAsync(reservation);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.VerifyAsync(transaction.Id,
                new VerifyTransactionRequest { QrCodeData = "valid-qr" }, "verifier1", "verifier1"));
            Assert.Equal("Rejected", transaction.Status);
        }
        /// <summary>
        /// Verifies and validates async_missing transaction_is not found criteria.
        /// </summary>

        [Fact]
        public async Task VerifyAsync_MissingTransaction_IsNotFound()
        {
            // Execute verify async_missing transaction_is not found operations
            _transactions.Setup(x => x.GetByIdAsync("missing")).ReturnsAsync((Transaction?)null);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.VerifyAsync("missing",
                new VerifyTransactionRequest { QrCodeData = "qr" }, "verifier1", "verifier1"));
        }
        /// <summary>
        /// Performs complete async_verified transaction_completes and records transfer time operation.
        /// </summary>

        [Fact]
        public async Task CompleteAsync_VerifiedTransaction_CompletesAndRecordsTransferTime()
        {
            // Execute complete async_verified transaction_completes and records transfer time operations
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
            _reservations.Verify(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        /// <summary>
        /// Performs complete async_non verifiable state_is rejected operation.
        /// </summary>

        [Theory]
        [InlineData("Pending")]
        [InlineData("QRGenerated")]
        [InlineData("VerificationPending")]
        [InlineData("Rejected")]
        [InlineData("Cancelled")]
        [InlineData("Completed")]
        public async Task CompleteAsync_NonVerifiableState_IsRejected(string status)
        {
            // Execute complete async_non verifiable state_is rejected operations
            var transaction = Transaction(status: status);
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteAsync(transaction.Id,
                new CompleteTransactionRequest { Confirmation = "CONFIRMED" }, "verifier1"));
            _transactions.Verify(x => x.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
        }
        /// <summary>
        /// Performs complete async_invalid confirmation_is rejected operation.
        /// </summary>

        [Fact]
        public async Task CompleteAsync_InvalidConfirmation_IsRejected()
        {
            // Execute complete async_invalid confirmation_is rejected operations
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CompleteAsync("tx1",
                new CompleteTransactionRequest { Confirmation = "no" }, "verifier1"));
            _transactions.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
        }
        /// <summary>
        /// Performs complete async_null request_is rejected operation.
        /// </summary>

        [Fact]
        public async Task CompleteAsync_NullRequest_IsRejected()
        {
            // Execute complete async_null request_is rejected operations
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CompleteAsync("tx1", null!, "verifier1"));
        }
        /// <summary>
        /// Performs complete async_missing transaction_returns null operation.
        /// </summary>

        [Fact]
        public async Task CompleteAsync_MissingTransaction_ReturnsNull()
        {
            // Execute complete async_missing transaction_returns null operations
            _transactions.Setup(x => x.GetByIdAsync("missing")).ReturnsAsync((Transaction?)null);
            Assert.Null(await _service.CompleteAsync("missing",
                new CompleteTransactionRequest { Confirmation = "CONFIRMED" }, "verifier1"));
        }
        /// <summary>
        /// Performs complete async_invalid id_is rejected operation.
        /// </summary>

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task CompleteAsync_InvalidId_IsRejected(string? id)
        {
            // Execute complete async_invalid id_is rejected operations
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CompleteAsync(id!,
                new CompleteTransactionRequest { Confirmation = "CONFIRMED" }, "verifier1"));
        }
        /// <summary>
        /// Performs complete async_different verifier_is forbidden operation.
        /// </summary>

        [Fact]
        public async Task CompleteAsync_DifferentVerifier_IsForbidden()
        {
            // Execute complete async_different verifier_is forbidden operations
            var transaction = Transaction(status: "Verified");
            transaction.VerifiedBy = "verifier1";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.CompleteAsync(transaction.Id,
                new CompleteTransactionRequest { Confirmation = "CONFIRMED" }, "verifier2"));
            _transactions.Verify(x => x.UpdateAsync(It.IsAny<Transaction>()), Times.Never);
        }
        /// <summary>
        /// Updates the specified status async_workflow controlled targets_cannot be set generically record.
        /// </summary>

        [Theory]
        [InlineData("pending", "QRGenerated")]
        [InlineData("QRGenerated", "VerificationPending")]
        [InlineData("QRGenerated", "Verified")]
        [InlineData("VerificationPending", "Verified")]
        [InlineData("Verified", "EnergyTransferInProgress")]
        [InlineData("EnergyTransferInProgress", "Completed")]
        public async Task UpdateStatusAsync_WorkflowControlledTargets_CannotBeSetGenerically(string current, string requested)
        {
            // Execute update status async_workflow controlled targets_cannot be set generically operations
            var transaction = Transaction(status: current);
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateStatusAsync(transaction.Id, requested, "verifier1"));
        }
        /// <summary>
        /// Updates the specified status async_invalid transitions_are rejected record.
        /// </summary>

        [Theory]
        [InlineData("Completed", "Pending")]
        [InlineData("Rejected", "Pending")]
        [InlineData("Cancelled", "Pending")]
        [InlineData("Pending", "Completed")]
        [InlineData("Pending", "Verified")]
        [InlineData("QRGenerated", "Completed")]
        public async Task UpdateStatusAsync_InvalidTransitions_AreRejected(string current, string requested)
        {
            // Execute update status async_invalid transitions_are rejected operations
            var transaction = Transaction(status: current);
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateStatusAsync(transaction.Id, requested, "verifier1"));
        }
        /// <summary>
        /// Updates the specified status async_invalid status_is rejected record.
        /// </summary>

        [Fact]
        public async Task UpdateStatusAsync_InvalidStatus_IsRejected()
        {
            // Execute update status async_invalid status_is rejected operations
            var transaction = Transaction(status: "Pending");
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateStatusAsync(transaction.Id, "Unknown", "verifier1"));
        }
        /// <summary>
        /// Updates the specified status async_allowed terminal transition_succeeds record.
        /// </summary>

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
            // Execute update status async_allowed terminal transition_succeeds operations
            var transaction = Transaction(status: current);
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            _transactions.Setup(x => x.UpdateAsync(transaction)).ReturnsAsync(transaction);

            var result = await _service.UpdateStatusAsync(transaction.Id, requested, "verifier1");

            Assert.NotNull(result);
            Assert.Equal(requested, result!.Status);
        }
        /// <summary>
        /// Updates the specified status async_empty id or status_is rejected record.
        /// </summary>

        [Fact]
        public async Task UpdateStatusAsync_EmptyIdOrStatus_IsRejected()
        {
            // Execute update status async_empty id or status_is rejected operations
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateStatusAsync("", "Cancelled", "v"));
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateStatusAsync("tx1", "", "v"));
        }
        /// <summary>
        /// Retrieves by id async_prosumer cannot read another owners transaction details.
        /// </summary>

        [Fact]
        public async Task GetByIdAsync_ProsumerCannotReadAnotherOwnersTransaction()
        {
            // Execute get by id async_prosumer cannot read another owners transaction operations
            var transaction = Transaction(status: "Pending");
            transaction.ProsumerId = "owner";
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);
            _users.Setup(x => x.GetByIdAsync("other"))
                .ReturnsAsync(new User { Id = "other", Role = Role.Prosumer, Nic = "different" });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetByIdAsync(transaction.Id, "other", "Prosumer"));
        }
        /// <summary>
        /// Retrieves by id async_unknown role is forbidden details.
        /// </summary>

        [Fact]
        public async Task GetByIdAsync_UnknownRoleIsForbidden()
        {
            // Execute get by id async_unknown role is forbidden operations
            var transaction = Transaction(status: "Pending");
            _transactions.Setup(x => x.GetByIdAsync(transaction.Id)).ReturnsAsync(transaction);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.GetByIdAsync(transaction.Id, "user", "UnknownRole"));
        }
        /// <summary>
        /// Performs repository failure_is propagated operation.
        /// </summary>

        [Fact]
        public async Task RepositoryFailure_IsPropagated()
        {
            // Execute repository failure_is propagated operations
            _transactions.Setup(x => x.GetByIdAsync("tx1"))
                .ThrowsAsync(new InvalidOperationException("repository failure"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GetByIdAsync("tx1", "user", "Admin"));
        }
        /// <summary>
        /// Performs reservation operation.
        /// </summary>

        private static ReservationResponseDto Reservation(string status) => new()
        {
            Id = "r1",
            ProsumerId = "NIC123",
            MicrogridNodeId = "grid1",
            EnergySlotId = "slot1",
            EnergyAmount = 25,
            Status = status,
            StartTime = DateTime.UtcNow.AddMinutes(5),
            EndTime = DateTime.UtcNow.AddHours(1)
        };
        /// <summary>
        /// Performs transaction operation.
        /// </summary>

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
