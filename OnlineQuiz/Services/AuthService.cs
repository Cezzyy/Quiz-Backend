using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.IServices;
using OnlineQuiz.Utilities;
using System.Security.Claims;

namespace OnlineQuiz.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IUserService _userService;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _jwtExpirationHours;

        public AuthService(IAuthRepository authRepository, IUserService userService)
        {
            _authRepository = authRepository;
            _userService = userService;

            // Load JWT configuration from environment variables
            _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? throw new InvalidOperationException("JWT_SECRET is not configured");
            _jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                ?? "OnlineQuizAPI";
            _jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                ?? "OnlineQuizClient";
            
            var expirationHoursStr = Environment.GetEnvironmentVariable("JWT_EXPIRATION_HOURS");
            _jwtExpirationHours = int.TryParse(expirationHoursStr, out int hours) ? hours : 24;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest)
        {
            // Verify credentials using Supabase RPC function
            var user = await _authRepository.VerifyUserCredentialsAsync(loginRequest.Email, loginRequest.Password);
            
            if (user == null)
            {
                return null; // Invalid credentials
            }

            // Check if user is active
            if (user.Status != "Active")
            {
                throw new InvalidOperationException("User account is inactive");
            }

            // Get complete user data with roles
            var userWithRoles = await _authRepository.GetUserWithRolesAsync(user.UserId);
            if (userWithRoles == null)
            {
                throw new InvalidOperationException("Failed to retrieve user roles");
            }

            var (userData, userRole, student, teacher) = userWithRoles.Value;

            // Build UserResponseDto
            var userResponse = new UserResponseDto
            {
                UserId = userData.UserId,
                Email = userData.Email,
                FullName = userData.FullName,
                Status = userData.Status,
                ContactNumber = userData.ContactNumber,
                EmergencyContactNumber = userData.EmergencyContactNumber,
                CreatedAt = userData.CreatedAt,
                UpdatedAt = userData.UpdatedAt,
                CreatedBy = userData.CreatedBy,
                RoleId = userRole.RoleId,
                RoleName = GetRoleName(userRole.RoleId)
            };

            // Add student/teacher data if applicable
            if (student != null)
            {
                userResponse.Student = new StudentData
                {
                    StudentId = student.StudentId,
                    YearLevel = student.YearLevel,
                    Section = student.Section,
                    Course = student.Course
                };
            }

            if (teacher != null)
            {
                userResponse.Teacher = new TeacherData
                {
                    Department = teacher.Department
                };
            }

            // Generate JWT token
            var token = JwtTokenGenerator.GenerateToken(
                userData.UserId,
                userData.Email,
                userRole.RoleId,
                userResponse.RoleName,
                _jwtSecret,
                _jwtIssuer,
                _jwtAudience,
                _jwtExpirationHours
            );

            var tokenExpiration = DateTime.UtcNow.AddHours(_jwtExpirationHours);

            return new LoginResponseDto
            {
                User = userResponse,
                Token = token,
                TokenExpiration = tokenExpiration
            };
        }

        public ClaimsPrincipal? VerifyToken(string token)
        {
            return JwtTokenGenerator.ValidateToken(token, _jwtSecret, _jwtIssuer, _jwtAudience);
        }

        public async Task<UserResponseDto?> GetCurrentUserAsync(int userId)
        {
            return await _userService.GetUserByIdAsync(userId);
        }

        private string GetRoleName(int roleId)
        {
            return roleId switch
            {
                RoleConstants.Admin => "Admin",
                RoleConstants.Teacher => "Teacher",
                RoleConstants.Student => "Student",
                _ => "Unknown"
            };
        }
    }
}
