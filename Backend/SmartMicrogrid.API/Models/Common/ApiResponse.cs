// ===========================================================================================================
// File: ApiResponse.cs
// Project: Smart Solar Microgrid Trading System
// Module: M1 – Microgrid & Energy Resource Management
// Section Owned: M1 – Microgrid & Energy Resource Management
// Author: K. Saayinath (IT23304338)
// Description: Data transfer object (DTO) representing ApiResponse communication payload.
// ===========================================================================================================
namespace SmartMicrogrid.API.Models.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        /// <summary>
        /// Performs success response operation.
        /// </summary>

        public static ApiResponse<T> SuccessResponse(T data, string message = "Request processed successfully.")
        {
            // Execute success response operations
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Errors = null
            };
        }
        /// <summary>
        /// Performs failure response operation.
        /// </summary>

        public static ApiResponse<T> FailureResponse(string message, List<string>? errors = null)
        {
            // Execute failure response operations
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default,
                Errors = errors
            };
        }
    }
}
