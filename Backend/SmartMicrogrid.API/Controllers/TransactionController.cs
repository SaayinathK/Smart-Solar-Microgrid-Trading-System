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


        // ============================================================
        // GET: /api/transactions
        // Get transactions based on the logged-in user's role
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            var transactions =
                await _transactionService.GetAllAsync(
                    userId,
                    role);

            return Ok(new
            {
                success = true,
                message = "Transactions retrieved successfully.",
                data = transactions
            });
        }


        // ============================================================
        // GET: /api/transactions/{transactionId}
        // Get a single transaction
        // ============================================================
        [HttpGet("{transactionId}")]
        public async Task<IActionResult> GetById(
            string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Transaction ID is required.",
                    data = (object?)null
                });
            }

            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            var transaction =
                await _transactionService.GetByIdAsync(
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


        // ============================================================
        // POST: /api/transactions
        // Create a transaction from an approved reservation
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "TransactionVerifier")]
        public async Task<IActionResult> Create(
            [FromBody] CreateTransactionRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.ReservationId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Reservation ID is required.",
                    data = (object?)null
                });
            }

            var userId = GetCurrentUserId();

            var transaction =
                await _transactionService.CreateAsync(
                    request,
                    userId);

            if (transaction == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unable to create transaction.",
                    data = (object?)null
                });
            }

            return Ok(new
            {
                success = true,
                message = "Transaction created successfully.",
                data = transaction
            });
        }


        // ============================================================
        // POST: /api/transactions/{transactionId}/generate-qr
        // Generate QR data for a transaction
        // ============================================================
        [HttpPost("{transactionId}/generate-qr")]
        [Authorize(Roles = "TransactionVerifier")]
        public async Task<IActionResult> GenerateQr(
            string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Transaction ID is required.",
                    data = (object?)null
                });
            }

            var userId = GetCurrentUserId();

            var result =
                await _transactionService.GenerateQrAsync(
                    transactionId,
                    userId);

            if (result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Transaction not found or QR cannot be generated.",
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


        // ============================================================
        // POST: /api/transactions/{transactionId}/verify
        // Verify QR and reservation information
        // ============================================================
        [HttpPost("{transactionId}/verify")]
        [Authorize(Roles = "TransactionVerifier")]
        public async Task<IActionResult> Verify(
            string transactionId,
            [FromBody] VerifyTransactionRequest request)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Transaction ID is required.",
                    data = (object?)null
                });
            }

            if (request == null ||
                string.IsNullOrWhiteSpace(request.QrCodeData))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "QR code data is required.",
                    data = (object?)null
                });
            }

            var userId = GetCurrentUserId();

            // --------------------------------------------------------
            // The transaction ID from the URL is now passed directly
            // to the service together with the scanned QR data.
            // --------------------------------------------------------
            var transaction =
                await _transactionService.VerifyAsync(
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
                message = "Transaction verified successfully.",
                data = transaction
            });
        }


        // ============================================================
        // POST: /api/transactions/{transactionId}/complete
        // Complete the energy transaction
        // ============================================================
        [HttpPost("{transactionId}/complete")]
        [Authorize(Roles = "TransactionVerifier")]
        public async Task<IActionResult> Complete(
            string transactionId,
            [FromBody] CompleteTransactionRequest request)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Transaction ID is required.",
                    data = (object?)null
                });
            }

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Confirmation))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Confirmation is required.",
                    data = (object?)null
                });
            }

            var userId = GetCurrentUserId();

            var transaction =
                await _transactionService.CompleteAsync(
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


        // ============================================================
        // PATCH: /api/transactions/{transactionId}/status
        // Update transaction status
        // ============================================================
        [HttpPatch("{transactionId}/status")]
        [Authorize(Roles = "TransactionVerifier")]
        public async Task<IActionResult> UpdateStatus(
            string transactionId,
            [FromQuery] string status)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Transaction ID is required.",
                    data = (object?)null
                });
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Transaction status is required.",
                    data = (object?)null
                });
            }

            var userId = GetCurrentUserId();

            var transaction =
                await _transactionService.UpdateStatusAsync(
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


        // ============================================================
        // GET CURRENT USER ID FROM JWT
        // ============================================================
        private string GetCurrentUserId()
        {
            return User.FindFirstValue(
                       ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub")
                   ?? throw new UnauthorizedAccessException(
                       "User ID was not found in the token.");
        }


        // ============================================================
        // GET CURRENT USER ROLE FROM JWT
        // ============================================================
        private string GetCurrentRole()
        {
            return User.FindFirstValue(
                       ClaimTypes.Role)
                   ?? User.FindFirstValue("role")
                   ?? string.Empty;
        }
    }
}