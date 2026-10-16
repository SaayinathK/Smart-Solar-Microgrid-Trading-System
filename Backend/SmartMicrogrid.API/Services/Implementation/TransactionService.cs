// ===========================================================================================================
// File: TransactionService.cs
// Project: Smart Solar Microgrid Trading System
// Module: M3 – Transaction & Verification Management
// Section Owned: M3 – Transaction & Verification Management
// Author: J. Jathusan (IT23297418)
// Description: Business logic service implementing TransactionService operations, rules, and workflows.
// ===========================================================================================================
using System.Security.Cryptography;

using SmartMicrogrid.API.DTOs.Transactions;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Models.Transactions;
using SmartMicrogrid.API.Repositories.Interfaces;
using SmartMicrogrid.API.Services.Interfaces;
using SmartMicrogrid.API.Services;

namespace SmartMicrogrid.API.Services.Implementation
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IReservationService _reservationService;
        private readonly IUserRepository _userRepository;
        /// <summary>
        /// Initializes a new instance of the TransactionService class.
        /// </summary>

        public TransactionService(
            ITransactionRepository transactionRepository,
            IReservationService reservationService,
            IUserRepository userRepository)
        {
            // Initialize dependencies and state
            _transactionRepository = transactionRepository;
            _reservationService = reservationService;
            _userRepository = userRepository;
        }
        /// <summary>
        /// Creates or registers a new async record.
        /// </summary>


        // Create transaction
        public async Task<TransactionResponse> CreateAsync(
            CreateTransactionRequest request,
            string currentUserId,
            string? operatorId = null)
        {
            // Execute create async operations
            if (string.IsNullOrWhiteSpace(operatorId))
            {
                throw new UnauthorizedAccessException(
                    "A valid authenticated MicrogridOperator is required.");
            }

            await EnsureMicrogridOperatorIdentityAsync(currentUserId, operatorId);

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.ReservationId))
            {
                throw new ArgumentException(
                    "Reservation ID is required.");
            }


            var reservation =
                await _reservationService.GetByIdAsync(
                    request.ReservationId,
                    operatorId);

            if (reservation == null)
            {
                throw new KeyNotFoundException(
                    "Reservation was not found.");
            }


            // Only approved reservations can create transactions
            if (!string.Equals(
                    reservation.Status,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only approved reservations can initiate transaction processing.");
            }


            // Prevent duplicate transaction for same reservation
            var existing =
                await _transactionRepository
                    .GetByReservationIdAsync(
                        request.ReservationId);

            if (existing != null)
            {
                throw new TransactionConflictException(
                    "A transaction already exists for this reservation.");
            }


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
        /// <summary>
        /// Retrieves all async details.
        /// </summary>


        // Get all transactions
        public async Task<List<TransactionResponse>> GetAllAsync(
            string currentUserId,
            string currentRole)
        {
            // Execute get all async operations
            List<Transaction> transactions;


            // Prosumer can only access their own transactions
            if (currentRole.Equals(
                    "Prosumer",
                    StringComparison.OrdinalIgnoreCase))
            {
                var user = await _userRepository.GetByIdAsync(currentUserId);
                var prosumerNic = user?.Nic;

                transactions = string.IsNullOrWhiteSpace(prosumerNic)
                    ? new List<Transaction>()
                    : await _transactionRepository.GetByProsumerIdAsync(prosumerNic);
            }


            // Operator sees transactions relevant to their operations
            else if (currentRole.Equals(
                         "MicrogridOperator",
                         StringComparison.OrdinalIgnoreCase))
            {
                var assignedMicrogridIds =
                    await _reservationService.GetAssignedMicrogridIdsAsync(currentUserId);
                var allTransactions =
                    await _transactionRepository.GetAllAsync();

                transactions = allTransactions
                    .Where(transaction => assignedMicrogridIds.Contains(transaction.MicrogridNodeId))
                    .ToList();
            }


            // Admin has system-level access
            else if (currentRole.Equals(
                         "Admin",
                         StringComparison.OrdinalIgnoreCase))
            {
                transactions =
                    await _transactionRepository
                        .GetAllAsync();
            }


            else
            {
                throw new UnauthorizedAccessException(
                    "You are not authorized to view transactions.");
            }


            return transactions
                .Select(Map)
                .ToList();
        }
        /// <summary>
        /// Retrieves by id async details.
        /// </summary>


        // Get transaction by ID
        public async Task<TransactionResponse?> GetByIdAsync(
            string transactionId,
            string currentUserId,
            string currentRole)
        {
            // Execute get by id async operations
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

            if (currentRole.Equals(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Map(transaction);
            }

            if (currentRole.Equals(
                    "MicrogridOperator",
                    StringComparison.OrdinalIgnoreCase))
            {
                var assignedMicrogridIds =
                    await _reservationService.GetAssignedMicrogridIdsAsync(currentUserId);

                if (!assignedMicrogridIds.Contains(transaction.MicrogridNodeId))
                {
                    throw new UnauthorizedAccessException(
                        "You are not authorized to view transactions for this microgrid.");
                }

                return Map(transaction);
            }

            if (currentRole.Equals(
                    "Prosumer",
                    StringComparison.OrdinalIgnoreCase))
            {
                var user = await _userRepository.GetByIdAsync(currentUserId);
                var prosumerNic = user?.Nic;

                if (string.IsNullOrWhiteSpace(prosumerNic) ||
                    transaction.ProsumerId != prosumerNic)
                {
                    throw new UnauthorizedAccessException(
                        "You are not allowed to view this transaction.");
                }

                return Map(transaction);
            }

            throw new UnauthorizedAccessException(
                "You are not authorized to view this transaction.");


        }
        /// <summary>
        /// Performs generate qr async operation.
        /// </summary>


        // Generate transaction QR
        public async Task<GenerateQrResponse?> GenerateQrAsync(
            string transactionId,
            string currentUserId)
        {
            // Execute generate qr async operations
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

            await EnsureMicrogridOperatorAccessAsync(
                currentUserId,
                transaction.MicrogridNodeId);


            // QR can only be generated once for a pending transaction
            if (!transaction.Status.Equals(
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "QR can only be generated for a pending transaction.");
            }


            var transactionCode =
                GenerateTransactionCode();


            transaction.TransactionCode =
                transactionCode;


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
        /// <summary>
        /// Verifies and validates async criteria.
        /// </summary>


        // Verify transaction
        public async Task<TransactionResponse?> VerifyAsync(
            string transactionId,
            VerifyTransactionRequest request,
            string currentUserId,
            string? operatorId = null)
        {
            // Execute verify async operations
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

            if (string.IsNullOrWhiteSpace(operatorId))
            {
                throw new UnauthorizedAccessException(
                    "A valid authenticated MicrogridOperator is required.");
            }

            await EnsureMicrogridOperatorIdentityAsync(currentUserId, operatorId);


            var transaction =
                await _transactionRepository
                    .GetByIdAsync(transactionId);

            if (transaction == null)
            {
                throw new KeyNotFoundException(
                    "Transaction was not found.");
            }


            if (string.IsNullOrWhiteSpace(
                    transaction.QrCodeData))
            {
                throw new InvalidOperationException(
                    "QR code has not been generated for this transaction.");
            }


            // Make sure scanned QR belongs to this transaction
            if (!string.Equals(
                    transaction.QrCodeData,
                    request.QrCodeData,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The scanned QR code does not belong to this transaction.");
            }


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


            var reservation =
                await _reservationService.GetByIdAsync(
                    transaction.ReservationId,
                    operatorId);


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


            if (reservation.EndTime <= DateTime.UtcNow)
            {
                throw new InvalidOperationException(
                    "The reservation has expired and cannot be verified.");
            }



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
        /// <summary>
        /// Performs complete async operation.
        /// </summary>


        // Complete transaction
        public async Task<TransactionResponse?> CompleteAsync(
            string transactionId,
            CompleteTransactionRequest request,
            string currentUserId)
        {
            // Execute complete async operations
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new ArgumentException(
                    "Transaction ID is required.");
            }


            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }


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

            await EnsureMicrogridOperatorAccessAsync(
                currentUserId,
                transaction.MicrogridNodeId);


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


            // Only the verifier who verified the transaction can complete it
            if (!string.IsNullOrWhiteSpace(
                    transaction.VerifiedBy)
                &&
                transaction.VerifiedBy != currentUserId)
            {
                throw new UnauthorizedAccessException(
                    "Only the MicrogridOperator who verified this transaction can complete it.");
            }


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


            transaction.Status =
                "Completed";

            transaction.UpdatedAt =
                DateTime.UtcNow;


            await _transactionRepository
                .UpdateAsync(transaction);


            return Map(transaction);
        }
        /// <summary>
        /// Updates the specified status async record.
        /// </summary>


        // Update transaction status
        public async Task<TransactionResponse?> UpdateStatusAsync(
            string transactionId,
            string status,
            string currentUserId)
        {
            // Execute update status async operations
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

            await EnsureMicrogridOperatorAccessAsync(
                currentUserId,
                transaction.MicrogridNodeId);


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
        /// <summary>
        /// Seeds and configures default/sample re microgrid operator identity async data.
        /// </summary>


        private async Task EnsureMicrogridOperatorIdentityAsync(
            string currentUserId,
            string? operatorId = null)
        {
            // Execute ensure microgrid operator identity async operations
            if (string.IsNullOrWhiteSpace(currentUserId) ||
                (!string.IsNullOrWhiteSpace(operatorId) &&
                 !string.Equals(operatorId, currentUserId, StringComparison.Ordinal)))
            {
                throw new UnauthorizedAccessException(
                    "A valid authenticated MicrogridOperator is required.");
            }

            var user = await _userRepository.GetByIdAsync(currentUserId);
            if (user == null || user.Role != Role.MicrogridOperator)
            {
                throw new UnauthorizedAccessException(
                    "A valid authenticated MicrogridOperator is required.");
            }
        }
        /// <summary>
        /// Seeds and configures default/sample re microgrid operator access async data.
        /// </summary>

        private async Task EnsureMicrogridOperatorAccessAsync(
            string currentUserId,
            string microgridNodeId)
        {
            // Execute ensure microgrid operator access async operations
            await EnsureMicrogridOperatorIdentityAsync(currentUserId);

            var assignedMicrogridIds =
                await _reservationService.GetAssignedMicrogridIdsAsync(currentUserId);

            if (!assignedMicrogridIds.Contains(microgridNodeId))
            {
                throw new UnauthorizedAccessException(
                    "You are not authorized to access transactions for this microgrid.");
            }
        }
        /// <summary>
        /// Performs is valid status transition operation.
        /// </summary>


        private static bool IsValidStatusTransition(
            string currentStatus,
            string newStatus)
        {
            // Execute is valid status transition operations
            if (string.IsNullOrWhiteSpace(
                    currentStatus)
                ||
                string.IsNullOrWhiteSpace(
                    newStatus))
            {
                return false;
            }


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
        /// <summary>
        /// Performs generate transaction code operation.
        /// </summary>


        private static string GenerateTransactionCode()
        {
            // Execute generate transaction code operations
            return
                $"TX-{DateTime.UtcNow:yyyyMMddHHmmss}-" +
                $"{RandomNumberGenerator.GetInt32(100000, 999999)}";
        }
        /// <summary>
        /// Performs map operation.
        /// </summary>


        private static TransactionResponse Map(
            Transaction transaction)
        {
            // Execute map operations
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
