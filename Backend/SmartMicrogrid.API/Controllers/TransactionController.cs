// ===========================================================================================================
// File: TransactionController.cs
// Project: Smart Solar Microgrid Trading System
// Module: M3 – Transaction & Verification Management
// Section Owned: M3 – Transaction & Verification Management
// Author: J. Jathusan (IT23297418)
// Description: API Controller exposing REST endpoints for Transaction management.
// ===========================================================================================================
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MongoDB.Driver;
using SmartMicrogrid.API.DTOs.Transactions;
using SmartMicrogrid.API.Models.Common;
using SmartMicrogrid.API.Services.Interfaces;
using SmartMicrogrid.API.Services;
using System.Security.Claims;

namespace SmartMicrogrid.API.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    [Authorize]
    [M3ExceptionFilter]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        /// <summary>
        /// Initializes a new instance of the TransactionController class.
        /// </summary>

        public TransactionController(
            ITransactionService transactionService)
        {
            // Initialize dependencies and state
            _transactionService = transactionService;
        }
        /// <summary>
        /// Retrieves all details.
        /// </summary>


        // Get transactions
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // Execute get all operations
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            var transactions =
                await _transactionService.GetAllAsync(
                    userId,
                    role);

            return Ok(ApiResponse<object>.SuccessResponse(
                transactions,
                "Transactions retrieved successfully."));
        }
        /// <summary>
        /// Retrieves by id details.
        /// </summary>


        // Get transaction by ID
        [HttpGet("{transactionId}")]
        public async Task<IActionResult> GetById(
            string transactionId)
        {
            // Execute get by id operations
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return BadRequest(ApiResponse<object>.FailureResponse(
                    "Transaction ID is required."));
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
                return NotFound(ApiResponse<object>.FailureResponse(
                    "Transaction not found."));
            }

            return Ok(ApiResponse<object>.SuccessResponse(
                transaction,
                "Transaction retrieved successfully."));
        }
        /// <summary>
        /// Creates or registers a new  record.
        /// </summary>


        // Create a transaction
        [HttpPost]
        [Authorize(Roles = "MicrogridOperator")]
        public async Task<IActionResult> Create(
            [FromBody] CreateTransactionRequest request)
        {
            // Execute create operations
            if (request == null ||
                string.IsNullOrWhiteSpace(request.ReservationId))
            {
                return BadRequest(ApiResponse<object>.FailureResponse(
                    "Reservation ID is required."));
            }

            var userId = GetCurrentUserId();
            var operatorId = User.IsInRole("MicrogridOperator") ? userId : null;

            var transaction =
                await _transactionService.CreateAsync(
                    request,
                    userId,
                    operatorId);

            if (transaction == null)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(
                    "Unable to create transaction."));
            }

            return Ok(ApiResponse<object>.SuccessResponse(
                transaction,
                "Transaction created successfully."));
        }
        /// <summary>
        /// Performs generate qr operation.
        /// </summary>


        // Generate transaction QR
        [HttpPost("{transactionId}/generate-qr")]
        [Authorize(Roles = "MicrogridOperator")]
        public async Task<IActionResult> GenerateQr(
            string transactionId)
        {
            // Execute generate qr operations
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return BadRequest(ApiResponse<object>.FailureResponse(
                    "Transaction ID is required."));
            }

            var userId = GetCurrentUserId();

            var result =
                await _transactionService.GenerateQrAsync(
                    transactionId,
                    userId);

            if (result == null)
            {
                return NotFound(ApiResponse<object>.FailureResponse(
                    "Transaction not found or QR cannot be generated."));
            }

            return Ok(ApiResponse<object>.SuccessResponse(
                result,
                "QR data generated successfully."));
        }
        /// <summary>
        /// Verifies and validates  criteria.
        /// </summary>


        // Verify transaction
        [HttpPost("{transactionId}/verify")]
        [Authorize(Roles = "MicrogridOperator")]
        public async Task<IActionResult> Verify(
            string transactionId,
            [FromBody] VerifyTransactionRequest request)
        {
            // Execute verify operations
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return BadRequest(ApiResponse<object>.FailureResponse(
                    "Transaction ID is required."));
            }

            if (request == null ||
                string.IsNullOrWhiteSpace(request.QrCodeData))
            {
                return BadRequest(ApiResponse<object>.FailureResponse(
                    "QR code data is required."));
            }

            var userId = GetCurrentUserId();
            var operatorId = User.IsInRole("MicrogridOperator") ? userId : null;

            // The transaction ID from the URL is passed directly
            // to the service together with the scanned QR data.
            var transaction =
                await _transactionService.VerifyAsync(
                    transactionId,
                    request,
                    userId,
                    operatorId);

            if (transaction == null)
            {
                return NotFound(ApiResponse<object>.FailureResponse(
                    "Transaction not found."));
            }

            return Ok(ApiResponse<object>.SuccessResponse(
                transaction,
                "Transaction verified successfully."));
        }
        /// <summary>
        /// Performs complete operation.
        /// </summary>


        // Complete transaction
        [HttpPost("{transactionId}/complete")]
        [Authorize(Roles = "MicrogridOperator")]
        public async Task<IActionResult> Complete(
            string transactionId,
            [FromBody] CompleteTransactionRequest request)
        {
            // Execute complete operations
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return BadRequest(ApiResponse<object>.FailureResponse(
                    "Transaction ID is required."));
            }

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Confirmation))
            {
                return BadRequest(ApiResponse<object>.FailureResponse(
                    "Confirmation is required."));
            }

            var userId = GetCurrentUserId();

            var transaction =
                await _transactionService.CompleteAsync(
                    transactionId,
                    request,
                    userId);

            if (transaction == null)
            {
                return NotFound(ApiResponse<object>.FailureResponse(
                    "Transaction not found."));
            }

            return Ok(ApiResponse<object>.SuccessResponse(
                transaction,
                "Energy transaction completed successfully."));
        }
        /// <summary>
        /// Updates the specified status record.
        /// </summary>


        // Update transaction status
        [HttpPatch("{transactionId}/status")]
        [Authorize(Roles = "MicrogridOperator")]
        public async Task<IActionResult> UpdateStatus(
            string transactionId,
            [FromQuery] string status)
        {
            // Execute update status operations
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return BadRequest(ApiResponse<object>.FailureResponse(
                    "Transaction ID is required."));
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                return BadRequest(ApiResponse<object>.FailureResponse(
                    "Transaction status is required."));
            }

            var userId = GetCurrentUserId();

            var transaction =
                await _transactionService.UpdateStatusAsync(
                    transactionId,
                    status,
                    userId);

            if (transaction == null)
            {
                return NotFound(ApiResponse<object>.FailureResponse(
                    "Transaction not found."));
            }

            return Ok(ApiResponse<object>.SuccessResponse(
                transaction,
                "Transaction status updated successfully."));
        }
        /// <summary>
        /// Retrieves current user id details.
        /// </summary>


        private string GetCurrentUserId()
        {
            // Execute get current user id operations
            return User.FindFirstValue(
                       ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub")
                   ?? throw new UnauthorizedAccessException(
                       "User ID was not found in the token.");
        }
        /// <summary>
        /// Retrieves current role details.
        /// </summary>


        private string GetCurrentRole()
        {
            // Execute get current role operations
            return User.FindFirstValue(
                       ClaimTypes.Role)
                   ?? User.FindFirstValue("role")
                   ?? string.Empty;
        }

        private sealed class M3ExceptionFilterAttribute : Attribute, IAsyncExceptionFilter
        {
            /// <summary>
            /// Performs on exception async operation.
            /// </summary>
            public Task OnExceptionAsync(ExceptionContext context)
            {
                // Execute on exception async operations
                var (statusCode, message) = GetError(context.Exception);

                context.Result = new ObjectResult(
                    ApiResponse<object>.FailureResponse(message))
                {
                    StatusCode = statusCode
                };
                context.ExceptionHandled = true;

                return Task.CompletedTask;
            }
            /// <summary>
            /// Retrieves error details.
            /// </summary>

            private static (int StatusCode, string Message) GetError(
                Exception exception)
            {
                // Execute get error operations
                if (exception is MongoWriteException mongoException &&
                    mongoException.WriteError?.Code == 11000)
                {
                    return (
                        (int)HttpStatusCode.Conflict,
                        "A transaction already exists for this reservation.");
                }

                if (exception is TransactionConflictException)
                {
                    return (
                        (int)HttpStatusCode.Conflict,
                        "A transaction already exists for this reservation.");
                }

                return exception switch
                {
                    ArgumentException => (
                        (int)HttpStatusCode.BadRequest,
                        "Invalid transaction request."),
                    KeyNotFoundException => (
                        (int)HttpStatusCode.NotFound,
                        "Transaction or reservation not found."),
                    UnauthorizedAccessException => (
                        (int)HttpStatusCode.Forbidden,
                        "You are not authorized to perform this transaction operation."),
                    HttpRequestException => (
                        (int)HttpStatusCode.BadGateway,
                        "The reservation service is currently unavailable."),
                    InvalidOperationException => (
                        (int)HttpStatusCode.BadRequest,
                        "The transaction operation is invalid."),
                    _ => (
                        (int)HttpStatusCode.InternalServerError,
                        "An unexpected transaction error occurred.")
                };
            }
        }
    }
}
