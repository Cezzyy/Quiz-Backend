using System.Text.Json;

namespace OnlineQuiz.Utilities
{
    public static class ActivityLogHelper
    {
        /// <summary>
        /// Extracts the IP address from the HTTP context
        /// </summary>
        public static string? GetIpAddress(HttpContext context)
        {
            if (context == null)
                return null;

            // Try to get the IP from X-Forwarded-For header (if behind a proxy)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // Take the first IP if multiple are present
                return forwardedFor.Split(',').FirstOrDefault()?.Trim();
            }

            // Fall back to the remote IP address
            return context.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// Extracts the User-Agent from the HTTP context
        /// </summary>
        public static string? GetUserAgent(HttpContext context)
        {
            if (context == null)
                return null;

            var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
            
            // Truncate if too long (max 255 characters)
            if (!string.IsNullOrEmpty(userAgent) && userAgent.Length > 255)
            {
                return userAgent.Substring(0, 255);
            }

            return userAgent;
        }

        /// <summary>
        /// Serializes an object to JSON string
        /// </summary>
        public static string? SerializeToJson(object? data)
        {
            if (data == null)
                return null;

            try
            {
                return JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Validates if the action is a valid activity log action
        /// </summary>
        public static bool IsValidAction(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
                return false;

            return ActivityLogConstants.Actions.All.Contains(action);
        }

        /// <summary>
        /// Validates if the entity is a valid activity log entity
        /// </summary>
        public static bool IsValidEntity(string entity)
        {
            if (string.IsNullOrWhiteSpace(entity))
                return false;

            return ActivityLogConstants.Entities.All.Contains(entity);
        }
    }
}
