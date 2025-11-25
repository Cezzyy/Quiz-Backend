using System.ComponentModel.DataAnnotations;

namespace OnlineQuiz.DTOs
{
    /// <summary>
    /// DTO for login request containing user credentials
    /// </summary>
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for login response containing user data and JWT token
    /// </summary>
    public class LoginResponseDto
    {
        public UserResponseDto User { get; set; } = new();
        public string Token { get; set; } = string.Empty;
        public DateTime TokenExpiration { get; set; }
    }

    /// <summary>
    /// DTO for token information
    /// </summary>
    public class TokenResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public string TokenType { get; set; } = "Bearer";
    }
}
