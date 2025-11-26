using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.Controllers;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using OnlineQuiz.Utilities;
using Xunit;

namespace OnlineQuiz.Tests.Controllers
{
    // Simple fake implementation of IAuthService for unit testing
    internal class FakeAuthService : IAuthService
    {
        public bool ChangePasswordCalled { get; private set; }
        public int? ChangePasswordUserId { get; private set; }
        public ChangePasswordDto? LastChangePasswordDto { get; private set; }

        public Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest)
        {
            if (loginRequest.Email == "student@example.com" && loginRequest.Password == "password123")
            {
                return Task.FromResult<LoginResponseDto?>(new LoginResponseDto
                {
                    Token = "test-jwt-token",
                    TokenExpiration = DateTime.UtcNow.AddHours(1),
                    User = new UserResponseDto
                    {
                        UserId = 42,
                        Email = loginRequest.Email,
                        FullName = "Test Student",
                        RoleId = 7,
                        RoleName = "Student",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                });
            }

            return Task.FromResult<LoginResponseDto?>(null);
        }

        public ClaimsPrincipal? VerifyToken(string token)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(ClaimTypes.Email, "student@example.com"),
                new Claim(ClaimTypes.Role, "Student"),
            }, "FakeAuth");
            return new ClaimsPrincipal(identity);
        }

        public Task<UserResponseDto?> GetCurrentUserAsync(int userId)
        {
            if (userId == 42)
            {
                return Task.FromResult<UserResponseDto?>(new UserResponseDto
                {
                    UserId = 42,
                    Email = "student@example.com",
                    FullName = "Test Student",
                    RoleId = 7,
                    RoleName = "Student",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            return Task.FromResult<UserResponseDto?>(null);
        }

        public bool ThrowChangePasswordArgumentException { get; set; }
        public bool ThrowChangePasswordUnexpectedException { get; set; }

        public Task ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            if (ThrowChangePasswordArgumentException)
            {
                throw new ArgumentException("Invalid password format");
            }
            if (ThrowChangePasswordUnexpectedException)
            {
                throw new Exception("Service failure");
            }
            ChangePasswordCalled = true;
            ChangePasswordUserId = userId;
            LastChangePasswordDto = changePasswordDto;
            return Task.CompletedTask;
        }
    }

    // Simple fake implementation of IActivityLogService to avoid external side effects
    internal class FakeActivityLogService : IActivityLogService
    {
        public List<CreateActivityLogDto> LoggedActivities { get; } = new();

        public Task<ActivityLogDto> LogActivityAsync(CreateActivityLogDto dto)
        {
            LoggedActivities.Add(dto);
            return Task.FromResult(new ActivityLogDto
            {
                ActivityLogId = DateTime.UtcNow.Ticks,
                UserId = dto.UserId,
                Action = dto.Action,
                Entity = dto.Entity,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            });
        }

        public Task<List<ActivityLogDto>> GetActivityLogsAsync(ActivityLogFilterDto filter) => Task.FromResult(new List<ActivityLogDto>());
        public Task<List<ActivityLogDto>> GetUserActivityLogsAsync(int userId, int? days = null) => Task.FromResult(new List<ActivityLogDto>());
        public Task<ActivityLogStatisticsDto> GetActivityStatisticsAsync(int? userId = null, int? days = 30) => Task.FromResult(new ActivityLogStatisticsDto());
        public Task<ActivityLogDto?> GetActivityLogByIdAsync(long activityLogId) => Task.FromResult<ActivityLogDto?>(null);
    }

    public class AuthControllerTests
    {
        private static AuthController CreateController(FakeAuthService? authService = null, FakeActivityLogService? activityLogService = null, ClaimsPrincipal? user = null)
        {
            var controller = new AuthController(authService ?? new FakeAuthService(), activityLogService ?? new FakeActivityLogService());
            var httpContext = new DefaultHttpContext();
            if (user != null)
            {
                httpContext.User = user;
            }
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            return controller;
        }

        [Fact]
        public async Task Login_ReturnsOk_And_SetsJwtCookie()
        {
            var controller = CreateController();

            var loginRequest = new LoginRequestDto
            {
                Email = "student@example.com",
                Password = "password123"
            };

            var actionResult = await controller.Login(loginRequest);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var response = Assert.IsType<LoginResponseDto>(okResult.Value);

            Assert.Equal("student@example.com", response.User.Email);
            Assert.Equal("Student", response.User.RoleName);
            Assert.False(string.IsNullOrWhiteSpace(response.Token));

            var setCookieHeader = controller.Response.Headers["Set-Cookie"].ToString();
            Assert.Contains("jwt=", setCookieHeader);
            Assert.Contains(response.Token, setCookieHeader);
        }

        [Fact]
        public async Task Login_ReturnsBadRequest_WhenModelStateInvalid()
        {
            var controller = CreateController();
            controller.ModelState.AddModelError("Email", "Email is required");

            var loginRequest = new LoginRequestDto
            {
                Email = "",
                Password = ""
            };

            var actionResult = await controller.Login(loginRequest);
            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_OnInvalidCredentials()
        {
            var controller = CreateController();

            var loginRequest = new LoginRequestDto
            {
                Email = "wrong@example.com",
                Password = "badpass"
            };

            var actionResult = await controller.Login(loginRequest);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);
            Assert.NotNull(unauthorized.Value);
        }

        [Fact]
        public async Task VerifyMe_ReturnsOk_WithUser()
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim(ClaimTypes.Email, "student@example.com"),
                new Claim(ClaimTypes.Role, "Student"),
            }, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var controller = CreateController(user: principal);

            var actionResult = await controller.VerifyMe();
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var user = Assert.IsType<UserResponseDto>(okResult.Value);

            Assert.Equal(42, user.UserId);
            Assert.Equal("student@example.com", user.Email);
            Assert.Equal("Student", user.RoleName);
        }

        [Fact]
        public async Task VerifyMe_ReturnsUnauthorized_WhenMissingUserIdClaim()
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Email, "student@example.com"),
                new Claim(ClaimTypes.Role, "Student"),
            }, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var controller = CreateController(user: principal);

            var actionResult = await controller.VerifyMe();
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);
            Assert.NotNull(unauthorized.Value);
        }

        [Fact]
        public async Task VerifyMe_ReturnsNotFound_WhenUserDoesNotExist()
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "99"),
                new Claim(ClaimTypes.Email, "missing@example.com"),
                new Claim(ClaimTypes.Role, "Student"),
            }, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var controller = CreateController(user: principal);

            var actionResult = await controller.VerifyMe();
            var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            Assert.NotNull(notFound.Value);
        }

        [Fact]
        public async Task Logout_DeletesJwtCookie_AndReturnsOk()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42") }, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var controller = CreateController(user: principal);

            var actionResult = await controller.Logout();
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.NotNull(okResult.Value);

            var setCookieHeader = controller.Response.Headers["Set-Cookie"].ToString();
            Assert.Contains("jwt=", setCookieHeader);
        }

        [Fact]
        public async Task ChangePassword_ReturnsOk_AndInvokesService()
        {
            var fakeAuth = new FakeAuthService();
            var controller = CreateController(authService: fakeAuth, activityLogService: new FakeActivityLogService());

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42") }, "TestAuth");
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

            var dto = new ChangePasswordDto { OldPassword = "oldpass", NewPassword = "newpass123" };

            var actionResult = await controller.ChangePassword(dto);
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.NotNull(okResult.Value);

            Assert.True(fakeAuth.ChangePasswordCalled);
            Assert.Equal(42, fakeAuth.ChangePasswordUserId);
            Assert.Equal("newpass123", fakeAuth.LastChangePasswordDto!.NewPassword);
        }

        [Fact]
        public async Task ChangePassword_ReturnsUnauthorized_WhenMissingUserIdClaim()
        {
            var controller = CreateController(authService: new FakeAuthService(), activityLogService: new FakeActivityLogService());
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            var dto = new ChangePasswordDto { OldPassword = "old", NewPassword = "new" };

            var actionResult = await controller.ChangePassword(dto);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(actionResult);
            Assert.NotNull(unauthorized.Value);
        }

        [Fact]
        public async Task ChangePassword_ReturnsBadRequest_OnArgumentException()
        {
            var fakeAuth = new FakeAuthService { ThrowChangePasswordArgumentException = true };
            var controller = CreateController(authService: fakeAuth, activityLogService: new FakeActivityLogService());

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42") }, "TestAuth");
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

            var dto = new ChangePasswordDto { OldPassword = "old", NewPassword = "new" };

            var actionResult = await controller.ChangePassword(dto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.NotNull(badRequest.Value);
        }
    }
}