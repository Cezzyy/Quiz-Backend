using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace OnlineQuiz.Utilities
{
    /// <summary>
    /// Extension methods for ASP.NET Core controllers
    /// </summary>
    public static class ControllerExtensions
    {
        /// <summary>
        /// Extracts the authenticated user ID from JWT token claims.
        /// Tries multiple claim types in order: NameIdentifier, "id", "UserId"
        /// </summary>
        /// <param name="controller">The controller instance</param>
        /// <param name="userId">The extracted user ID if successful</param>
        /// <returns>True if user ID was successfully extracted and parsed, false otherwise</returns>
        public static bool TryGetAuthenticatedUserId(this ControllerBase controller, out int userId)
        {
            userId = 0;

            var userIdClaim = controller.User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? controller.User.FindFirst("id")
                           ?? controller.User.FindFirst("UserId");

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out userId))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the authenticated user ID from JWT token claims.
        /// Throws UnauthorizedAccessException if user ID cannot be extracted.
        /// </summary>
        /// <param name="controller">The controller instance</param>
        /// <returns>The authenticated user ID</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user identity cannot be verified</exception>
        public static int GetAuthenticatedUserId(this ControllerBase controller)
        {
            if (controller.TryGetAuthenticatedUserId(out int userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("User identity could not be verified");
        }
    }
}
