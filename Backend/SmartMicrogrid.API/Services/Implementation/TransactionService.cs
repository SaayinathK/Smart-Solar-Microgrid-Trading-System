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

        public async Task<TransactionResponse> CreateAsync(
            CreateTransactionRequest request,
            string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(request.ReservationId))
            {
                throw new ArgumentException(
                    "Reservation ID is required.");
            }

            var reservation =
                await _reservationApiClient
                    .GetReservationAsync(request.ReservationId);

            if (reservation == null)
            {
                throw new KeyNotFoundException(
                    "Reservation was not found.");
            }

            if (!string.Equals(
                    reservation.Status,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only approved reservations can initiate transaction processing.");
            }

            var existing =
                await _transactionRepository
                    .GetByReservationIdAsync(
                        request.ReservationId);

            if (existing != null)
            {
                return Map(existing);
            }

            var transaction = new Transaction
            {
                ReservationId = reservation.Id,
                ProsumerId = reservation.ProsumerId,
                MicrogridNodeId = reservation.MicrogridNodeId,
                EnergySlotId = reservation.EnergySlotId,
                EnergyAmount = reservation.EnergyAmount,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            transaction =
                await _transactionRepository
                    .CreateAsync(transaction);

            return Map(transaction);
        }

        public async Task<List<TransactionResponse>> GetAllAsync(
            string currentUserId,
            string currentRole)
        {
            List<Transaction> transactions;

            if (currentRole.Equals(
                    "Prosumer",
                    StringComparison.OrdinalIgnoreCase))
            {
                transactions =
                    await _transactionRepository
                        .GetByProsumerIdAsync(currentUserId);
            }
            else if (currentRole.Equals(
                         "TransactionVerifier",
                         StringComparison.OrdinalIgnoreCase))
            {
                transactions =
                    await _transactionRepository
                        .GetByVerifierIdAsync(currentUserId);
            }
            else
            {
                transactions =
                    await _transactionRepository
                        .GetAllAsync();
            }

            return transactions
                .Select(Map)
                .ToList();
        }

        public async Task<TransactionResponse?> GetByIdAsync(
            string transactionId,
            string currentUserId,
            string currentRole)
        {
            var transaction =
                await _transactionRepository
                    .GetByIdAsync(transactionId);

            if (transaction == null)
            {
                return null;
            }

            var allowed =
                currentRole.Equals(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase)
                || currentRole.Equals(
                    "TransactionVerifier",
                    StringComparison.OrdinalIgnoreCase)
                || transaction.ProsumerId == currentUserId;

            if (!allowed)
            {
                throw new UnauthorizedAccessException(
                    "You are not allowed to view this transaction.");
            }

            return Map(transaction);
        }

        public async Task<GenerateQrResponse?> GenerateQrAsync(
            string transactionId,
            string currentUserId)
        {
            var transaction =
                await _transactionRepository
                    .GetByIdAsync(transactionId);

            if (transaction == null)
            {
                return null;
            }

            if (transaction.Status != "Pending")
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

            transaction.Status = "QRGenerated";
            transaction.UpdatedAt = DateTime.UtcNow;

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

        public async Task<TransactionResponse?> VerifyAsync(
            VerifyTransactionRequest request,
            string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(request.QrCodeData))
            {
                throw new ArgumentException(
                    "QR code data is required.");
            }

            var transaction =
                await _transactionRepository
                    .GetByQrCodeDataAsync(
                        request.QrCodeData);

            if (transaction == null)
            {
                throw new KeyNotFoundException(
                    "Transaction associated with the QR code was not found.");
            }

            if (transaction.Status != "QRGenerated"
                && transaction.Status != "VerificationPending")
            {
                throw new InvalidOperationException(
                    $"Transaction cannot be verified from status '{transaction.Status}'.");
            }

            var reservation =
                await _reservationApiClient
                    .GetReservationAsync(
                        transaction.ReservationId);

            if (reservation == null)
            {
                transaction.Status = "Rejected";
                transaction.UpdatedAt = DateTime.UtcNow;

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
                transaction.Status = "Rejected";
                transaction.UpdatedAt = DateTime.UtcNow;

                await _transactionRepository
                    .UpdateAsync(transaction);

                throw new InvalidOperationException(
                    "The reservation is not approved for transaction processing.");
            }

            transaction.VerifiedBy = currentUserId;
            transaction.VerificationTime = DateTime.UtcNow;
            transaction.Status = "Verified";
            transaction.UpdatedAt = DateTime.UtcNow;

            await _transactionRepository
                .UpdateAsync(transaction);

            return Map(transaction);
        }

        public async Task<TransactionResponse?> CompleteAsync(
            string transactionId,
            CompleteTransactionRequest request,
            string currentUserId)
        {
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

            if (transaction.Status != "Verified"
                && transaction.Status != "EnergyTransferInProgress")
            {
                throw new InvalidOperationException(
                    "Only a verified transaction can be completed.");
            }

            if (transaction.VerifiedBy != currentUserId
                && !string.IsNullOrWhiteSpace(transaction.VerifiedBy))
            {
                throw new UnauthorizedAccessException(
                    "Only the verifying transaction verifier can complete this transaction.");
            }

            transaction.Status =
                "EnergyTransferInProgress";

            transaction.EnergyTransferTime =
                DateTime.UtcNow;

            transaction.UpdatedAt =
                DateTime.UtcNow;

            await _transactionRepository
                .UpdateAsync(transaction);

            transaction.Status =
                "Completed";

            transaction.UpdatedAt =
                DateTime.UtcNow;

            await _transactionRepository
                .UpdateAsync(transaction);

            return Map(transaction);
        }

        public async Task<TransactionResponse?> UpdateStatusAsync(
            string transactionId,
            string status,
            string currentUserId)
        {
            var allowedStatuses = new[]
            {
                "Pending",
                "QRGenerated",
                "VerificationPending",
                "Verified",
                "EnergyTransferInProgress",
                "Completed",
                "Rejected",
                "Cancelled"
            };

            if (!allowedStatuses.Contains(
                    status,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Invalid transaction status.");
            }

            var transaction =
                await _transactionRepository
                    .GetByIdAsync(transactionId);

            if (transaction == null)
            {
                return null;
            }

            transaction.Status = status;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _transactionRepository
                .UpdateAsync(transaction);

            return Map(transaction);
        }

        private static string GenerateTransactionCode()
        {
            return $"TX-{DateTime.UtcNow:yyyyMMddHHmmss}-{RandomNumberGenerator.GetInt32(100000, 999999)}";
        }

        private static TransactionResponse Map(
            Transaction transaction)
        {
            return new TransactionResponse
            {
                Id = transaction.Id,
                ReservationId = transaction.ReservationId,
                ProsumerId = transaction.ProsumerId,
                MicrogridNodeId = transaction.MicrogridNodeId,
                EnergySlotId = transaction.EnergySlotId,
                EnergyAmount = transaction.EnergyAmount,
                TransactionCode = transaction.TransactionCode,
                QrCodeData = transaction.QrCodeData,
                VerifiedBy = transaction.VerifiedBy,
                VerificationTime = transaction.VerificationTime,
                EnergyTransferTime = transaction.EnergyTransferTime,
                Status = transaction.Status,
                CreatedAt = transaction.CreatedAt,
                UpdatedAt = transaction.UpdatedAt
            };
        }
    }
}