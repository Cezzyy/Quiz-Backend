using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OnlineQuiz.Controllers;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using Xunit;

namespace OnlineQuiz.Tests.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // Fake IBiometricService
    // ─────────────────────────────────────────────────────────────────────────
    internal class FakeBiometricService : IBiometricService
    {
        // Control flags
        public bool EnrollSuccess { get; set; } = true;
        public bool UnenrollSuccess { get; set; } = true;
        public bool VerifySuccess { get; set; } = true;
        public bool IsEnrolled { get; set; } = true;
        public int? FingerprintSlot { get; set; } = 5;
        public bool CancelSuccess { get; set; } = true;

        public Task<BiometricEnrollResponseDto> StartEnrollmentAsync(int userId, int requestedByUserId)
            => Task.FromResult(new BiometricEnrollResponseDto
            {
                Success = EnrollSuccess,
                SlotId = EnrollSuccess ? 3 : null,
                Message = EnrollSuccess ? "Enrollment started" : "Device not connected"
            });

        public Task<BiometricEnrollResponseDto> CompleteEnrollmentAsync(int userId, int slotId, bool success, string? errorMessage = null)
            => Task.FromResult(new BiometricEnrollResponseDto { Success = success, SlotId = slotId, Message = success ? "Enrolled" : errorMessage ?? "Failed" });

        public Task<BiometricVerifyResponseDto> StartVerificationAsync(int userId, int? quizId = null)
            => Task.FromResult(new BiometricVerifyResponseDto
            {
                Success = VerifySuccess,
                Matched = false,
                Message = VerifySuccess ? "Verification started" : "User not enrolled"
            });

        public Task<BiometricVerifyResponseDto> CompleteVerificationAsync(int userId, bool matched, string? errorMessage = null)
            => Task.FromResult(new BiometricVerifyResponseDto { Success = matched, Matched = matched, Message = matched ? "Verified" : errorMessage ?? "Failed" });

        public Task<BiometricEnrollResponseDto> UnenrollFingerprintAsync(int userId, int requestedByUserId)
            => Task.FromResult(new BiometricEnrollResponseDto
            {
                Success = UnenrollSuccess,
                SlotId = UnenrollSuccess ? 3 : null,
                Message = UnenrollSuccess ? "Unenrolled" : "User has no fingerprint enrolled"
            });

        public Task<BiometricStatusDto> GetDeviceStatusAsync()
            => Task.FromResult(new BiometricStatusDto { IsConnected = true, CurrentMode = "Idle" });

        public Task<AvailableSlotsResponseDto> GetAvailableSlotsAsync()
            => Task.FromResult(new AvailableSlotsResponseDto
            {
                AvailableSlots = new List<int> { 1, 2, 3 },
                TotalSlots = 127,
                UsedSlots = 124
            });

        public Task<bool> IsUserEnrolledAsync(int userId) => Task.FromResult(IsEnrolled);

        public Task<int?> GetUserFingerprintSlotAsync(int userId) => Task.FromResult(FingerprintSlot);

        public Task<List<BiometricLogDto>> GetBiometricLogsAsync(BiometricLogFilterDto filter)
            => Task.FromResult(new List<BiometricLogDto>
            {
                new BiometricLogDto { Id = 1, UserId = 10, UserName = "Alice", ActionType = "enrollment_started", Success = true, CreatedAt = DateTime.UtcNow }
            });

        public Task<int> GetBiometricLogsCountAsync(BiometricLogFilterDto filter) => Task.FromResult(1);

        public Task<bool> CancelCurrentOperationAsync() => Task.FromResult(CancelSuccess);
    }

    internal class FakeActivityLogServiceForBiometric : IActivityLogService
    {
        public Task<ActivityLogDto> LogActivityAsync(CreateActivityLogDto dto)
            => Task.FromResult(new ActivityLogDto { ActivityLogId = 1, UserId = dto.UserId, Action = dto.Action, Entity = dto.Entity, CreatedAt = DateTime.UtcNow });
        public Task<List<ActivityLogDto>> GetActivityLogsAsync(ActivityLogFilterDto filter) => Task.FromResult(new List<ActivityLogDto>());
        public Task<List<ActivityLogDto>> GetUserActivityLogsAsync(int userId, int? days = null) => Task.FromResult(new List<ActivityLogDto>());
        public Task<ActivityLogStatisticsDto> GetActivityStatisticsAsync(int? userId = null, int? days = 30) => Task.FromResult(new ActivityLogStatisticsDto());
        public Task<ActivityLogDto?> GetActivityLogByIdAsync(long activityLogId) => Task.FromResult<ActivityLogDto?>(null);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests
    // ─────────────────────────────────────────────────────────────────────────
    [Collection("MapsterWarmup")]
    public class BiometricControllerTests
    {
        private static BiometricController CreateController(
            FakeBiometricService? biometric = null,
            string role = "Teacher",
            int userId = 42)
        {
            var controller = new BiometricController(
                biometric ?? new FakeBiometricService(),
                new FakeActivityLogServiceForBiometric(),
                NullLogger<BiometricController>.Instance);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("RoleName", role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
            return controller;
        }

        // ── EnrollFingerprint ─────────────────────────────────────────────

        [Fact]
        public async Task EnrollFingerprint_ReturnsOk_WhenTeacherAndSuccess()
        {
            var controller = CreateController(role: "Teacher");
            var result = await controller.EnrollFingerprint(userId: 10);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<BiometricEnrollResponseDto>(ok.Value);
            Assert.True(dto.Success);
        }

        [Fact]
        public async Task EnrollFingerprint_ReturnsOk_WhenAdminAndSuccess()
        {
            var controller = CreateController(role: "Admin");
            var result = await controller.EnrollFingerprint(userId: 10);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<BiometricEnrollResponseDto>(ok.Value);
            Assert.True(dto.Success);
        }

        [Fact]
        public async Task EnrollFingerprint_ReturnsForbid_WhenStudent()
        {
            var controller = CreateController(role: "Student");
            var result = await controller.EnrollFingerprint(userId: 10);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task EnrollFingerprint_ReturnsUnauthorized_WhenNoUserIdClaim()
        {
            var controller = new BiometricController(
                new FakeBiometricService(),
                new FakeActivityLogServiceForBiometric(),
                NullLogger<BiometricController>.Instance);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };
            var result = await controller.EnrollFingerprint(userId: 10);
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task EnrollFingerprint_ReturnsBadRequest_WhenServiceFails()
        {
            var fake = new FakeBiometricService { EnrollSuccess = false };
            var controller = CreateController(fake, role: "Teacher");
            var result = await controller.EnrollFingerprint(userId: 10);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            var dto = Assert.IsType<BiometricEnrollResponseDto>(bad.Value);
            Assert.False(dto.Success);
        }

        // ── UnenrollFingerprint ───────────────────────────────────────────

        [Fact]
        public async Task UnenrollFingerprint_ReturnsOk_WhenTeacherAndSuccess()
        {
            var controller = CreateController(role: "Teacher");
            var result = await controller.UnenrollFingerprint(userId: 10);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<BiometricEnrollResponseDto>(ok.Value);
            Assert.True(dto.Success);
        }

        [Fact]
        public async Task UnenrollFingerprint_ReturnsForbid_WhenStudent()
        {
            var controller = CreateController(role: "Student");
            var result = await controller.UnenrollFingerprint(userId: 10);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task UnenrollFingerprint_ReturnsBadRequest_WhenServiceFails()
        {
            var fake = new FakeBiometricService { UnenrollSuccess = false };
            var controller = CreateController(fake, role: "Admin");
            var result = await controller.UnenrollFingerprint(userId: 10);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            var dto = Assert.IsType<BiometricEnrollResponseDto>(bad.Value);
            Assert.False(dto.Success);
        }

        // ── VerifyFingerprint ─────────────────────────────────────────────

        [Fact]
        public async Task VerifyFingerprint_ReturnsOk_WhenUserVerifiesOwn()
        {
            // userId 42 verifying their own fingerprint (currentUserId = 42)
            var controller = CreateController(role: "Student", userId: 42);
            var result = await controller.VerifyFingerprint(userId: 42);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<BiometricVerifyResponseDto>(ok.Value);
            Assert.True(dto.Success);
        }

        [Fact]
        public async Task VerifyFingerprint_ReturnsForbid_WhenStudentVerifiesOther()
        {
            // Student 42 trying to verify user 99
            var controller = CreateController(role: "Student", userId: 42);
            var result = await controller.VerifyFingerprint(userId: 99);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task VerifyFingerprint_ReturnsOk_WhenTeacherVerifiesOther()
        {
            var controller = CreateController(role: "Teacher", userId: 42);
            var result = await controller.VerifyFingerprint(userId: 99);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<BiometricVerifyResponseDto>(ok.Value);
            Assert.True(dto.Success);
        }

        [Fact]
        public async Task VerifyFingerprint_ReturnsBadRequest_WhenServiceFails()
        {
            var fake = new FakeBiometricService { VerifySuccess = false };
            var controller = CreateController(fake, role: "Teacher");
            var result = await controller.VerifyFingerprint(userId: 10);
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            var dto = Assert.IsType<BiometricVerifyResponseDto>(bad.Value);
            Assert.False(dto.Success);
        }

        // ── GetDeviceStatus ───────────────────────────────────────────────

        [Fact]
        public async Task GetDeviceStatus_ReturnsOk_WithStatus()
        {
            var controller = CreateController();
            var result = await controller.GetDeviceStatus();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<BiometricStatusDto>(ok.Value);
            Assert.True(dto.IsConnected);
            Assert.Equal("Idle", dto.CurrentMode);
        }

        // ── GetAvailableSlots ─────────────────────────────────────────────

        [Fact]
        public async Task GetAvailableSlots_ReturnsOk_WhenTeacher()
        {
            var controller = CreateController(role: "Teacher");
            var result = await controller.GetAvailableSlots();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<AvailableSlotsResponseDto>(ok.Value);
            Assert.NotEmpty(dto.AvailableSlots);
        }

        [Fact]
        public async Task GetAvailableSlots_ReturnsForbid_WhenStudent()
        {
            var controller = CreateController(role: "Student");
            var result = await controller.GetAvailableSlots();
            Assert.IsType<ForbidResult>(result.Result);
        }

        // ── IsUserEnrolled ────────────────────────────────────────────────

        [Fact]
        public async Task IsUserEnrolled_ReturnsOk_WithEnrollmentStatus()
        {
            var controller = CreateController(role: "Teacher", userId: 42);
            var result = await controller.IsUserEnrolled(userId: 10);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task IsUserEnrolled_ReturnsForbid_WhenStudentChecksOther()
        {
            var controller = CreateController(role: "Student", userId: 42);
            var result = await controller.IsUserEnrolled(userId: 99);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task IsUserEnrolled_ReturnsOk_WhenStudentChecksOwn()
        {
            var controller = CreateController(role: "Student", userId: 42);
            var result = await controller.IsUserEnrolled(userId: 42);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        // ── GetBiometricLogs ──────────────────────────────────────────────

        [Fact]
        public async Task GetBiometricLogs_ReturnsOk_WithPaginatedResult()
        {
            var controller = CreateController(role: "Admin", userId: 1);
            var filter = new BiometricLogFilterDto { Page = 1, PageSize = 20 };
            var result = await controller.GetBiometricLogs(filter);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetBiometricLogs_ForcesOwnUserId_WhenStudent()
        {
            // Student should only see their own logs — filter.UserId gets overridden
            var controller = CreateController(role: "Student", userId: 42);
            var filter = new BiometricLogFilterDto { Page = 1, PageSize = 20 };
            var result = await controller.GetBiometricLogs(filter);
            Assert.IsType<OkObjectResult>(result);
            // The filter.UserId was set to 42 inside the controller
            Assert.Equal(42, filter.UserId);
        }

        // ── CancelOperation ───────────────────────────────────────────────

        [Fact]
        public async Task CancelOperation_ReturnsOk_WhenSuccess()
        {
            var controller = CreateController();
            var result = await controller.CancelOperation();
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task CancelOperation_ReturnsOk_WithFailedMessage_WhenServiceReturnsFalse()
        {
            var fake = new FakeBiometricService { CancelSuccess = false };
            var controller = CreateController(fake);
            var result = await controller.CancelOperation();
            // Controller always returns Ok — the success flag is in the body
            Assert.IsType<OkObjectResult>(result);
        }
    }
}
