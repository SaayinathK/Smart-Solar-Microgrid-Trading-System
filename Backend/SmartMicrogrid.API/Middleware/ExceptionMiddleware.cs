// ===========================================================================================================
// File: ExceptionMiddleware.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: HTTP middleware for Exception processing.
// ===========================================================================================================
using System.Net;
using System.Text.Json;
using SmartMicrogrid.API.Models.Common;

namespace SmartMicrogrid.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        /// <summary>
        /// Initializes a new instance of the ExceptionMiddleware class.
        /// </summary>

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            // Initialize dependencies and state
            _next = next;
            _logger = logger;
        }
        /// <summary>
        /// Performs invoke async operation.
        /// </summary>

        public async Task InvokeAsync(HttpContext context)
        {
            // Execute invoke async operations
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }
        /// <summary>
        /// Handles execution of exception async.
        /// </summary>

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Execute handle exception async operations
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)GetStatusCode(exception);

            var response = ApiResponse<object>.FailureResponse(
                "An internal server error occurred while processing your request.",
                new List<string> { exception.Message }
            );

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(response, options);
            return context.Response.WriteAsync(json);
        }
        /// <summary>
        /// Retrieves status code details.
        /// </summary>

        private static HttpStatusCode GetStatusCode(Exception exception)
        {
            // Execute get status code operations
            return exception switch
            {
                ArgumentException => HttpStatusCode.BadRequest,
                KeyNotFoundException => HttpStatusCode.NotFound,
                UnauthorizedAccessException => HttpStatusCode.Forbidden,
                HttpRequestException => HttpStatusCode.BadGateway,
                InvalidOperationException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };
        }
    }
}
