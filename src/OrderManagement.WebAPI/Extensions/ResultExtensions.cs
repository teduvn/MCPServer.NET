using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Domain.Common;

namespace OrderManagement.WebAPI.Extensions
{
    public static class ResultExtensions
    {
        /// <summary>
        /// Map Result.Failure sang IActionResult theo chuẩn RFC 7807 Problem Details.
        /// Được dùng trong mọi Controller — không cần viết if/else lặp lại.
        /// </summary>
        public static IActionResult ToProblemDetails<T>(this Result<T> result)
        {
            if (result.IsSuccess)
                throw new InvalidOperationException("Cannot convert success result to problem details");


            return result.Error.Code switch
            {
                // Business rule violation → 400 Bad Request
                "Error.BusinessRule" => CreateProblemDetailsResult(
                    StatusCodes.Status400BadRequest,
                    "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                    "Business Rule Violation",
                    result.Error.Description),


                // Entity không tồn tại → 404 Not Found
                // Match both "Error.NotFound" and "Entity.NotFound" patterns
                var code when code.EndsWith(".NotFound") => CreateProblemDetailsResult(
                    StatusCodes.Status404NotFound,
                    "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
                    "Resource Not Found",
                    result.Error.Description),


                // Validation thất bại → 422 Unprocessable Entity
                "Error.Validation" => CreateProblemDetailsResult(
                    StatusCodes.Status422UnprocessableEntity,
                    "https://datatracker.ietf.org/doc/html/rfc4918#section-11.2",
                    "Validation Failed",
                    result.Error.Description),


                // Fallback
                _ => CreateProblemDetailsResult(
                    StatusCodes.Status400BadRequest,
                    "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                    "Bad Request",
                    result.Error.Description)
            };
        }

        private static IActionResult CreateProblemDetailsResult(int statusCode, string type, string title, string detail)
        {
            var problemDetails = new ProblemDetails
            {
                Type = type,
                Title = title,
                Detail = detail,
                Status = statusCode
            };

            return new ObjectResult(problemDetails)
            {
                StatusCode = statusCode,
                ContentTypes = new Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection { "application/problem+json" }
            };
        }
    }

}
