using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMicrogrid.API.DTOs.Transactions;
using SmartMicrogrid.API.Services.Interfaces;
using System.Security.Claims;

namespace SmartMicrogrid.API.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(
            ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        // GET /api/transactions
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            var transactions =
                await _transactionService
                    .GetAllAsync(userId, role);

            return Ok(new
            {
                success = true,
                message = "Transactions retrieved successfully.",
                data = transactions
            });
        }

        // GET /api/transactions/{id}
        [HttpGet("{transactionId}")]
        public async Task<IActionResult> GetById(
            string transactionId)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            var transaction =
                await _transactionService
                    .GetByIdAsync(
                        transactionId,
                        userId,
                        role);

            if (transaction == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Transaction not found.",
                    data = (object?)null
                });
            }

            return Ok(new
            {
                success = true,
                message = "Transaction retrieved successfully.",
                data = transaction
            });
        }

        // POST /api/transactions
        [HttpPost]
        [Authorize(Roles = "TransactionVerifier")]
        public async Task<IActionResult> Create(
            [FromBody] CreateTransactionRequest request)
        {
            var userId = GetCurrentUserId();

            var transaction =
                await _transactionService
                    .CreateAsync(
                        request,
                        userId);

            return Ok(new
            {
                success = true,
                message = "Transaction created successfully.",
                data = transaction
            });
        }

        // POST /api/transactions/{id}/generate-qr
        [HttpPost("{transactionId}/generate-qr")]
        [Authorize(Roles = "TransactionVerifier")]
        public async Task<IActionResult> GenerateQr(
            string transactionId)
        {
            var userId = GetCurrentUserId();

            var result =
                await _transactionService
                    .GenerateQrAsync(
                        transactionId,
                        userId);

            if (result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Transaction not found.",
                    data = (object?)null
                });
            }

            return Ok(new
            {
                success = true,
                message = "QR data generated successfully.",
                data = result
            });
        }

        // POST /api/transactions/{id}/verify
        [HttpPost("{transactionId}/verify")]
        [Authorize(Roles = "TransactionVerifier")]
        public async Task<IActionResult> Verify(
            string transactionId,
            [FromBody] VerifyTransactionRequest request)
        {
            var userId = GetCurrentUserId();

            var transaction =
                await _transactionService
                    .VerifyAsync(
                        request,
                        userId);

            if (transaction == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Transaction not found.",
                    data = (object?)null
                });
            }

            return Ok(new
            {
                success = true,
                message = "Transaction verified successfully.",
                data = transaction
            });
        }

        // POST /api/transactions/{id}/complete
        [HttpPost("{transactionId}/complete")]
        [Authorize(Roles = "TransactionVerifier")]
        public async Task<IActionResult> Complete(
            string transactionId,
            [FromBody] CompleteTransactionRequest request)
        {
            var userId = GetCurrentUserId();

            var transaction =
                await _transactionService
                    .CompleteAsync(
                        transactionId,
                        request,
                        userId);

            if (transaction == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Transaction not found.",
                    data = (object?)null
                });
            }

            return Ok(new
            {
                success = true,
                message = "Energy transaction completed successfully.",
                data = transaction
            });
        }

        // PATCH /api/transactions/{id}/status
        [HttpPatch("{transactionId}/status")]
        [Authorize(Roles = "TransactionVerifier")]
        public async Task<IActionResult> UpdateStatus(
            string transactionId,
            [FromQuery] string status)
        {
            var userId = GetCurrentUserId();

            var transaction =
                await _transactionService
                    .UpdateStatusAsync(
                        transactionId,
                        status,
                        userId);

            if (transaction == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Transaction not found.",
                    data = (object?)null
                });
            }

            return Ok(new
            {
                success = true,
                message = "Transaction status updated successfully.",
                data = transaction
            });
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(
                       ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub")
                   ?? throw new UnauthorizedAccessException(
                       "User ID was not found in the token.");
        }

        private string GetCurrentRole()
        {
            return User.FindFirstValue(
                       ClaimTypes.Role)
                   ?? User.FindFirstValue("role")
                   ?? string.Empty;
        }
    }
}