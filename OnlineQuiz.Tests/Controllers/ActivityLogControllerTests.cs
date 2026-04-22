using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.Controllers;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using Xunit;

namespace OnlineQuiz.Tests.Controllers
{
    internal class FakeActivityLogServiceForActivityLog : IActivityLogService
    {
        public bool ReturnNullOnGetById { get; set; }

        public Task<ActivityLogDto> LogActivityAsync(CreateActivityLogDto dto)
            => Task.FromResult(new ActivityLogDto { ActivityLogId = 1, UserId = dto.UserId, Action = dto.Action, Entity = dto.Entity, CreatedAt = DateTime.UtcNow });

        public Task<List<ActivityLogDto>> GetActivityLogsAsync(ActivityLogFilterDto filter)
            => Task.FromResult(new List<ActivityLogDto>
            {
                new ActivityLogDto { ActivityLogId = 1, UserId = 10, Action = "CREATE", Entity = "User", CreatedAt = DateTime.UtcNow }
            });

        public Task<List<ActivityLogDto>> GetUserActivityLogsAsync(int userId, int? days = null)
            => Task.FromResult(new List<ActivityLogDto>
            {
                new ActivityLogDto { ActivityLogId = 2, UserId = userId, Action = "LOGIN", Entity = "Auth", CreatedAt = DateTime.UtcNow }
            });

        public Task<ActivityLogStatisticsDto> GetActivityStatisticsAsync(int? userId = null, int? days = 30)
            => Task.FromResult(new ActivityLogStatisticsDto { TotalActions = 5, UniqueUsers = 2 });

        public Task<ActivityLogDto?> GetActivityLogByIdAsync(long activityLogId)
        {
            if (ReturnNullOnGetById) return Task.FromResult<ActivityLogDto?>(null);
            return Task.FromResult<ActivityLogDto?>(new ActivityLogDto { ActivityLogId = activityLogId, UserId = 1, Action = "CREATE", Entity = "User", CreatedAt = DateTime.UtcNow });
        }
    }

    [Collection("MapsterWarmup")]
    public class ActivityLogControllerTests
    {
        private static ActivityLogController CreateController(
            FakeActivityLogServiceForActivityLog? svc = null,
            string role = "Admin",
            int userId = 1)
        {
            var controller = new ActivityLogController(svc ?? new FakeActivityLogServiceForActivityLog());
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };
            return controller;
        }

        // ── GetActivityLogs ───────────────────────────────────────────────

        [Fact]
        public async Task GetActivityLogs_ReturnsOk_WhenAdmin()
        {
            var controller = CreateController(role: "Admin");
            var result = await controller.GetActivityLogs(new ActivityLogFilterDto());
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<ActivityLogDto>>(ok.Value);
            Assert.NotEmpty(list);
        }

        [Fact]
        public async Task GetActivityLogs_ReturnsForbid_WhenNotAdmin()
        {
            var controller = CreateController(role: "Teacher");
            var result = await controller.GetActivityLogs(new ActivityLogFilterDto());
            Assert.IsType<ForbidResult>(result.Result);
        }

        // ── GetActivityLogById ────────────────────────────────────────────

        [Fact]
        public async Task GetActivityLogById_ReturnsOk_WhenAdmin()
        {
            var controller = CreateController(role: "Admin");
            var result = await controller.GetActivityLogById(1);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var log = Assert.IsType<ActivityLogDto>(ok.Value);
            Assert.Equal(1, log.ActivityLogId);
        }

        [Fact]
        public async Task GetActivityLogById_ReturnsNotFound_WhenMissing()
        {
            var controller = CreateController(new FakeActivityLogServiceForActivityLog { ReturnNullOnGetById = true }, role: "Admin");
            var result = await controller.GetActivityLogById(999);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetActivityLogById_ReturnsForbid_WhenNotAdmin()
        {
            var controller = CreateController(role: "Student");
            var result = await controller.GetActivityLogById(1);
            Assert.IsType<ForbidResult>(result.Result);
        }

        // ── GetUserActivityLogs ───────────────────────────────────────────

        [Fact]
        public async Task GetUserActivityLogs_ReturnsOk_WhenAdminViewsOther()
        {
            var controller = CreateController(role: "Admin", userId: 1);
            var result = await controller.GetUserActivityLogs(userId: 99);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetUserActivityLogs_ReturnsOk_WhenUserViewsOwn()
        {
            var controller = CreateController(role: "Student", userId: 42);
            var result = await controller.GetUserActivityLogs(userId: 42);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetUserActivityLogs_ReturnsForbid_WhenStudentViewsOther()
        {
            var controller = CreateController(role: "Student", userId: 42);
            var result = await controller.GetUserActivityLogs(userId: 99);
            Assert.IsType<ForbidResult>(result.Result);
        }

        // ── GetMyActivityLogs ─────────────────────────────────────────────

        [Fact]
        public async Task GetMyActivityLogs_ReturnsOk_WithValidClaim()
        {
            var controller = CreateController(userId: 10);
            var result = await controller.GetMyActivityLogs();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetMyActivityLogs_ReturnsUnauthorized_WhenNoUserIdClaim()
        {
            var controller = new ActivityLogController(new FakeActivityLogService());
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var result = await controller.GetMyActivityLogs();
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        // ── GetActivityStatistics ─────────────────────────────────────────

        [Fact]
        public async Task GetActivityStatistics_ReturnsOk_WhenAdmin()
        {
            var controller = CreateController(role: "Admin");
            var result = await controller.GetActivityStatistics();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var stats = Assert.IsType<ActivityLogStatisticsDto>(ok.Value);
            Assert.Equal(5, stats.TotalActions);
        }

        [Fact]
        public async Task GetActivityStatistics_ReturnsForbid_WhenNotAdmin()
        {
            var controller = CreateController(role: "Teacher");
            var result = await controller.GetActivityStatistics();
            Assert.IsType<ForbidResult>(result.Result);
        }

        // ── CreateActivityLog ─────────────────────────────────────────────

        [Fact]
        public async Task CreateActivityLog_ReturnsCreated_WithValidDto()
        {
            var controller = CreateController();
            var dto = new CreateActivityLogDto
            {
                UserId = 1,
                Action = "CREATE",
                Entity = "User",
                Description = "Test log"
            };
            var result = await controller.CreateActivityLog(dto);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.NotNull(created.Value);
        }
    }
}
