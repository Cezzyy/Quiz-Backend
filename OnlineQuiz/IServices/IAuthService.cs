using OnlineQuiz.DTOs;
using System.Security.Claims;

namespace OnlineQuiz.IServices
{
    public interface IAuthService
    {
        /// <summary>
        /// Authenticate user and generate JWT token
        /// </summary>
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest);

        /// <summary>
        /// Validate JWT token and return claims
        /// </summary>
        ClaimsPrincipal? VerifyToken(string token);

        /// <summary>
        /// Get current user information from token
        /// </summary>
        Task<UserResponseDto?> GetCurrentUserAsync(int userId);

        /// <summary>
        /// Change user password
        /// </summary>
        Task ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
    }
}
