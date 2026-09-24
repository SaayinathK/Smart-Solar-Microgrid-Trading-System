using System.Security.Cryptography;

using SmartMicrogrid.API.DTOs.Transactions;
using SmartMicrogrid.API.Models.Transactions;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services.Interfaces;

namespace SmartMicrogrid.API.Services.Implementation
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IReservationApiClient _reservationApiClient;

        public TransactionService(
            ITransactionRepository transactionRepository,
            IReservationApiClient reservationApiClient)
        {
            _transactionRepository = transactionRepository;
            _reservationApiClient = reservationApiClient;
        }


        // ============================================================
        // CREATE TRANSACTION
        // ============================================================
        public async Task<TransactionResponse> CreateAsync(
            CreateTransactionRequest request,
            string currentUserId)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.ReservationId))
            {
                throw new ArgumentException(
                    "Reservation ID is required.");
            }


            // --------------------------------------------------------
            // Get reservation from M2
            // --------------------------------------------------------
            var reservation =
                await _reservationApiClient
                    .GetReservationAsync(
                        request.ReservationId);

            if (reservation == null)
            {
                throw new KeyNotFoundException(
                    "Reservation was not found.");
            }


            // --------------------------------------------------------
            // Only approved reservations can create transactions
            // --------------------------------------------------------
            if (!string.Equals(
                    reservation.Status,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only approved reservations can initiate transaction processing.");
            }


            // --------------------------------------------------------
            // Prevent duplicate transaction for same reservation
            // --------------------------------------------------------
            var existing =
                await _transactionRepository
                    .GetByReservationIdAsync(
                        request.ReservationId);

            if (existing != null)
            {
                return Map(existing);
            }


            // --------------------------------------------------------
            // Create new transaction
            // --------------------------------------------------------
            var transaction = new Transaction
            {
                ReservationId = reservation.Id,
                ProsumerId = reservation.ProsumerId,
                MicrogridNodeId = reservation.MicrogridNodeId,
                EnergySlotId = reservation.EnergySlotId,
                EnergyAmount = reservation.EnergyAmount,

                TransactionCode = string.Empty,
                QrCodeData = string.Empty,

                VerifiedBy = null,
                VerificationTime = null,
                EnergyTransferTime = null,

                Status = "Pending",

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };


            transaction =
                await _transactionRepository
                    .CreateAsync(transaction);

            return Map(transaction);
        }


        // ============================================================
        // GET ALL TRANSACTIONS
        // ============================================================
        public async Task<List<TransactionResponse>> GetAllAsync(
            string currentUserId,
            string currentRole)
        {
            List<Transaction> transactions;


            // --------------------------------------------------------
            // PROSUMER
            // Only own transactions
            // --------------------------------------------------------
            if (currentRole.Equals(
                    "Prosumer",
                    StringComparison.OrdinalIgnoreCase))
            {
                transactions =
                    await _transactionRepository
                        .GetByProsumerIdAsync(
                            currentUserId);
            }


            // --------------------------------------------------------
            // TRANSACTION VERIFIER
            // Verifier needs access to transactions that require
            // operational verification, including pending ones.
            // --------------------------------------------------------
            else if (currentRole.Equals(
                         "TransactionVerifier",
                         StringComparison.OrdinalIgnoreCase))
            {
                transactions =
                    await _transactionRepository
                        .GetAllAsync();
            }


            // --------------------------------------------------------
            // MICROGRID OPERATOR
            // Relevant operational transaction information
            // --------------------------------------------------------
            else if (currentRole.Equals(
                         "MicrogridOperator",
                         StringComparison.OrdinalIgnoreCase))
            {
                transactions =
                    await _transactionRepository
                        .GetAllAsync();
            }


            // --------------------------------------------------------
            // SYSTEM ADMINISTRATOR
            // System-level monitoring
            // --------------------------------------------------------
            else if (currentRole.Equals(
                         "SystemAdministrator",
                         StringComparison.OrdinalIgnoreCase))
            {
                transactions =
                    await _transactionRepository
                        .GetAllAsync();
            }


            // --------------------------------------------------------
            // Unknown role
            // --------------------------------------------------------
            else
            {
                throw new UnauthorizedAccessException(
                    "You are not authorized to view transactions.");
            }


            return transactions
                .Select(Map)
                .ToList();
        }


        // ============================================================
        // GET TRANSACTION BY ID
        // ============================================================
        public async Task<TransactionResponse?> GetByIdAsync(
            string transactionId,
            string currentUserId,
            string currentRole)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new ArgumentException(
                    "Transaction ID is required.");
            }


            var transaction =
                await _transactionRepository
                    .GetByIdAsync(transactionId);

            if (transaction == null)
            {
                return null;
            }


            // --------------------------------------------------------
            // Authorization
            // --------------------------------------------------------
            var isProsumer =
                currentRole.Equals(
                    "Prosumer",
                    StringComparison.OrdinalIgnoreCase);

            var isVerifier =
                currentRole.Equals(
                    "TransactionVerifier",
                    StringComparison.OrdinalIgnoreCase);

            var isOperator =
                currentRole.Equals(
                    "MicrogridOperator",
                    StringComparison.OrdinalIgnoreCase);

            var isAdministrator =
                currentRole.Equals(
                    "SystemAdministrator",
                    StringComparison.OrdinalIgnoreCase);


            var isTransactionOwner =
                transaction.ProsumerId == currentUserId;


            // Prosumer can only see own transaction
            if (isProsumer && !isTransactionOwner)
            {
                throw new UnauthorizedAccessException(
                    "You are not allowed to view this transaction.");
            }


            // Other authorized operational roles can view
            if (!isProsumer &&
                !isVerifier &&
                !isOperator &&
                !isAdministrator)
            {
                throw new UnauthorizedAccessException(
                    "You are not authorized to view this transaction.");
            }


            return Map(transaction);
        }


        // ============================================================
        // GENERATE QR
        // ============================================================
        public async Task<GenerateQrResponse?> GenerateQrAsync(
            string transactionId,
            string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new ArgumentException(
                    "Transaction ID is required.");
            }


            var transaction =
                await _transactionRepository
                    .GetByIdAsync(transactionId);

            if (transaction == null)
            {
                return null;
            }


            // --------------------------------------------------------
            // QR can only be generated once for Pending transaction
            // --------------------------------------------------------
            if (!transaction.Status.Equals(
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "QR can only be generated for a pending transaction.");
            }


            // --------------------------------------------------------
            // Generate unique transaction code
            // --------------------------------------------------------
            var transactionCode =
                GenerateTransactionCode();


            transaction.TransactionCode =
                transactionCode;


            // --------------------------------------------------------
            // QR payload
            // --------------------------------------------------------
            transaction.QrCodeData =
                $"SMART-MICROGRID|TRANSACTION|{transaction.Id}|{transactionCode}";


            transaction.Status =
                "QRGenerated";

            transaction.UpdatedAt =
                DateTime.UtcNow;


            await _transactionRepository
                .UpdateAsync(transaction);


            return new GenerateQrResponse
            {
                TransactionId = transaction.Id,
                ReservationId = transaction.ReservationId,
                TransactionCode = transaction.TransactionCode,
                QrCodeData = transaction.QrCodeData,
                Status = transaction.Status
            };
        }


        // ============================================================
        // VERIFY TRANSACTION
        // ============================================================
        public async Task<TransactionResponse?> VerifyAsync(
            string transactionId,
            VerifyTransactionRequest request,
            string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new ArgumentException(
                    "Transaction ID is required.");
            }


            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }


            if (string.IsNullOrWhiteSpace(request.QrCodeData))
            {
                throw new ArgumentException(
                    "QR code data is required.");
            }


            // --------------------------------------------------------
            // Get transaction using URL transaction ID
            // --------------------------------------------------------
            var transaction =
                await _transactionRepository
                    .GetByIdAsync(transactionId);

            if (transaction == null)
            {
                throw new KeyNotFoundException(
                    "Transaction was not found.");
            }


            // --------------------------------------------------------
            // Check QR exists
            // --------------------------------------------------------
            if (string.IsNullOrWhiteSpace(
                    transaction.QrCodeData))
            {
                throw new InvalidOperationException(
                    "QR code has not been generated for this transaction.");
            }


            // --------------------------------------------------------
            // Make sure scanned QR belongs to this transaction
            // --------------------------------------------------------
            if (!string.Equals(
                    transaction.QrCodeData,
                    request.QrCodeData,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The scanned QR code does not belong to this transaction.");
            }


            // --------------------------------------------------------
            // Transaction must be ready for verification
            // --------------------------------------------------------
            if (!transaction.Status.Equals(
                    "QRGenerated",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !transaction.Status.Equals(
                    "VerificationPending",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Transaction cannot be verified from status '{transaction.Status}'.");
            }


            // --------------------------------------------------------
            // Get reservation from M2
            // --------------------------------------------------------
            var reservation =
                await _reservationApiClient
                    .GetReservationAsync(
                        transaction.ReservationId);


            if (reservation == null)
            {
                transaction.Status =
                    "Rejected";

                transaction.UpdatedAt =
                    DateTime.UtcNow;

                await _transactionRepository
                    .UpdateAsync(transaction);

                throw new InvalidOperationException(
                    "The associated reservation could not be found.");
            }


            // --------------------------------------------------------
            // Reservation must still be approved
            // --------------------------------------------------------
            if (!string.Equals(
                    reservation.Status,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                transaction.Status =
                    "Rejected";

                transaction.UpdatedAt =
                    DateTime.UtcNow;

                await _transactionRepository
                    .UpdateAsync(transaction);

                throw new InvalidOperationException(
                    "The reservation is not approved for transaction processing.");
            }


            // --------------------------------------------------------
            // Validate reservation against transaction
            // --------------------------------------------------------

            // Prosumer validation
            if (!string.Equals(
                    transaction.ProsumerId,
                    reservation.ProsumerId,
                    StringComparison.Ordinal))
            {
                transaction.Status =
                    "Rejected";

                transaction.UpdatedAt =
                    DateTime.UtcNow;

                await _transactionRepository
                    .UpdateAsync(transaction);

                throw new InvalidOperationException(
                    "Transaction prosumer does not match the reservation.");
            }


            // Microgrid validation
            if (!string.Equals(
                    transaction.MicrogridNodeId,
                    reservation.MicrogridNodeId,
                    StringComparison.Ordinal))
            {
                transaction.Status =
                    "Rejected";

                transaction.UpdatedAt =
                    DateTime.UtcNow;

                await _transactionRepository
                    .UpdateAsync(transaction);

                throw new InvalidOperationException(
                    "Transaction microgrid does not match the reservation.");
            }


            // Energy slot validation
            if (!string.Equals(
                    transaction.EnergySlotId,
                    reservation.EnergySlotId,
                    StringComparison.Ordinal))
            {
                transaction.Status =
                    "Rejected";

                transaction.UpdatedAt =
                    DateTime.UtcNow;

                await _transactionRepository
                    .UpdateAsync(transaction);

                throw new InvalidOperationException(
                    "Transaction energy slot does not match the reservation.");
            }


            // Energy amount validation
            if (transaction.EnergyAmount !=
                reservation.EnergyAmount)
            {
                transaction.Status =
                    "Rejected";

                transaction.UpdatedAt =
                    DateTime.UtcNow;

                await _transactionRepository
                    .UpdateAsync(transaction);

                throw new InvalidOperationException(
                    "Transaction energy amount does not match the reservation.");
            }


            // --------------------------------------------------------
            // Verification successful
            // --------------------------------------------------------
            transaction.VerifiedBy =
                currentUserId;

            transaction.VerificationTime =
                DateTime.UtcNow;

            transaction.Status =
                "Verified";

            transaction.UpdatedAt =
                DateTime.UtcNow;


            await _transactionRepository
                .UpdateAsync(transaction);


            return Map(transaction);
        }


        // ============================================================
        // COMPLETE TRANSACTION
        // ============================================================
        public async Task<TransactionResponse?> CompleteAsync(
            string transactionId,
            CompleteTransactionRequest request,
            string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new ArgumentException(
                    "Transaction ID is required.");
            }


            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }


            // --------------------------------------------------------
            // Confirmation validation
            // --------------------------------------------------------
            if (!string.Equals(
                    request.Confirmation,
                    "CONFIRMED",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Confirmation must be 'CONFIRMED'.");
            }


            var transaction =
                await _transactionRepository
                    .GetByIdAsync(transactionId);

            if (transaction == null)
            {
                return null;
            }


            // --------------------------------------------------------
            // Transaction must be verified first
            // --------------------------------------------------------
            if (!transaction.Status.Equals(
                    "Verified",
                    StringComparison.OrdinalIgnoreCase)
                &&
                !transaction.Status.Equals(
                    "EnergyTransferInProgress",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only a verified transaction can be completed.");
            }


            // --------------------------------------------------------
            // Only the verifier who verified the transaction
            // can complete it
            // --------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(
                    transaction.VerifiedBy)
                &&
                transaction.VerifiedBy != currentUserId)
            {
                throw new UnauthorizedAccessException(
                    "Only the verifying transaction verifier can complete this transaction.");
            }


            // --------------------------------------------------------
            // Energy transfer begins
            // --------------------------------------------------------
            if (!transaction.Status.Equals(
                    "EnergyTransferInProgress",
                    StringComparison.OrdinalIgnoreCase))
            {
                transaction.Status =
                    "EnergyTransferInProgress";

                transaction.EnergyTransferTime =
                    DateTime.UtcNow;

                transaction.UpdatedAt =
                    DateTime.UtcNow;


                await _transactionRepository
                    .UpdateAsync(transaction);
            }


            // --------------------------------------------------------
            // Complete transaction
            // --------------------------------------------------------
            transaction.Status =
                "Completed";

            transaction.UpdatedAt =
                DateTime.UtcNow;


            await _transactionRepository
                .UpdateAsync(transaction);


            return Map(transaction);
        }


        // ============================================================
        // UPDATE STATUS
        // ============================================================
        public async Task<TransactionResponse?> UpdateStatusAsync(
            string transactionId,
            string status,
            string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new ArgumentException(
                    "Transaction ID is required.");
            }


            if (string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentException(
                    "Transaction status is required.");
            }


            var transaction =
                await _transactionRepository
                    .GetByIdAsync(transactionId);

            if (transaction == null)
            {
                return null;
            }


            // --------------------------------------------------------
            // Normalize requested status
            // --------------------------------------------------------
            var requestedStatus =
                status.Trim();

            var workflowControlledStatuses = new[]
            {
                "QRGenerated",
                "VerificationPending",
                "Verified",
                "EnergyTransferInProgress",
                "Completed"
            };

            if (workflowControlledStatuses.Contains(
                    requestedStatus,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Status '{requestedStatus}' must be changed through the appropriate transaction workflow.");
            }


            // --------------------------------------------------------
            // Validate status transition
            // --------------------------------------------------------
            var validTransition =
                IsValidStatusTransition(
                    transaction.Status,
                    requestedStatus);


            if (!validTransition)
            {
                throw new InvalidOperationException(
                    $"Invalid transaction status transition: '{transaction.Status}' -> '{requestedStatus}'.");
            }


            transaction.Status =
                requestedStatus;

            transaction.UpdatedAt =
                DateTime.UtcNow;


            await _transactionRepository
                .UpdateAsync(transaction);


            return Map(transaction);
        }


        // ============================================================
        // VALIDATE TRANSACTION STATUS TRANSITION
        // ============================================================
        private static bool IsValidStatusTransition(
            string currentStatus,
            string newStatus)
        {
            if (string.IsNullOrWhiteSpace(
                    currentStatus)
                ||
                string.IsNullOrWhiteSpace(
                    newStatus))
            {
                return false;
            }


            // Same status does not require an update
            if (currentStatus.Equals(
                    newStatus,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }


            switch (currentStatus.ToLowerInvariant())
            {
                case "pending":

                    return newStatus.Equals(
                               "QRGenerated",
                               StringComparison.OrdinalIgnoreCase)
                           ||
                           newStatus.Equals(
                               "Cancelled",
                               StringComparison.OrdinalIgnoreCase);


                case "qrgenerated":

                    return newStatus.Equals(
                               "VerificationPending",
                               StringComparison.OrdinalIgnoreCase)
                           ||
                           newStatus.Equals(
                               "Verified",
                               StringComparison.OrdinalIgnoreCase)
                           ||
                           newStatus.Equals(
                               "Rejected",
                               StringComparison.OrdinalIgnoreCase)
                           ||
                           newStatus.Equals(
                               "Cancelled",
                               StringComparison.OrdinalIgnoreCase);


                case "verificationpending":

                    return newStatus.Equals(
                               "Verified",
                               StringComparison.OrdinalIgnoreCase)
                           ||
                           newStatus.Equals(
                               "Rejected",
                               StringComparison.OrdinalIgnoreCase)
                           ||
                           newStatus.Equals(
                               "Cancelled",
                               StringComparison.OrdinalIgnoreCase);


                case "verified":

                    return newStatus.Equals(
                               "EnergyTransferInProgress",
                               StringComparison.OrdinalIgnoreCase)
                           ||
                           newStatus.Equals(
                               "Rejected",
                               StringComparison.OrdinalIgnoreCase)
                           ||
                           newStatus.Equals(
                               "Cancelled",
                               StringComparison.OrdinalIgnoreCase);


                case "energytransferinprogress":

                    return newStatus.Equals(
                               "Completed",
                               StringComparison.OrdinalIgnoreCase)
                           ||
                           newStatus.Equals(
                               "Rejected",
                               StringComparison.OrdinalIgnoreCase);


                case "completed":

                    // Completed is final
                    return false;


                case "rejected":

                    // Rejected is final
                    return false;


                case "cancelled":

                    // Cancelled is final
                    return false;


                default:

                    return false;
            }
        }


        // ============================================================
        // GENERATE TRANSACTION CODE
        // ============================================================
        private static string GenerateTransactionCode()
        {
            return
                $"TX-{DateTime.UtcNow:yyyyMMddHHmmss}-" +
                $"{RandomNumberGenerator.GetInt32(100000, 999999)}";
        }


        // ============================================================
        // MAP MODEL -> RESPONSE DTO
        // ============================================================
        private static TransactionResponse Map(
            Transaction transaction)
        {
            return new TransactionResponse
            {
                Id = transaction.Id,

                ReservationId =
                    transaction.ReservationId,

                ProsumerId =
                    transaction.ProsumerId,

                MicrogridNodeId =
                    transaction.MicrogridNodeId,

                EnergySlotId =
                    transaction.EnergySlotId,

                EnergyAmount =
                    transaction.EnergyAmount,

                TransactionCode =
                    transaction.TransactionCode,

                QrCodeData =
                    transaction.QrCodeData,

                VerifiedBy =
                    transaction.VerifiedBy,

                VerificationTime =
                    transaction.VerificationTime,

                EnergyTransferTime =
                    transaction.EnergyTransferTime,

                Status =
                    transaction.Status,

                CreatedAt =
                    transaction.CreatedAt,

                UpdatedAt =
                    transaction.UpdatedAt
            };
        }
    }
}