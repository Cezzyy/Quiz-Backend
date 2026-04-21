using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OnlineQuiz.Attributes
{
    /// <summary>
    /// Custom authorization attribute for API key authentication
    /// Used for hardware devices (ESP32) that cannot use JWT tokens
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
    {
        private const string ApiKeyHeaderName = "X-API-Key";

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Get configuration and logger
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ApiKeyAuthAttribute>>();

            // Check if API key exists in header
            if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
            {
                logger.LogWarning("API key missing from request. IP: {IP}", context.HttpContext.Connection.RemoteIpAddress);
                context.Result = new UnauthorizedObjectResult(new { message = "API key is missing" });
                return;
            }

            // Get expected API key from configuration
            var expectedApiKey = configuration["ESP32:ApiKey"];
            
            if (string.IsNullOrEmpty(expectedApiKey))
            {
                logger.LogError("ESP32:ApiKey is not configured in appsettings.json or environment variables");
                context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                return;
            }

            // Validate API key
            if (!expectedApiKey.Equals(extractedApiKey))
            {
                logger.LogWarning("Invalid API key attempt. IP: {IP}", context.HttpContext.Connection.RemoteIpAddress);
                context.Result = new UnauthorizedObjectResult(new { message = "Invalid API key" });
                return;
            }

            // API key is valid, proceed
            logger.LogDebug("API key validated successfully for IP: {IP}", context.HttpContext.Connection.RemoteIpAddress);
            await next();
        }
    }
}
