using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.Controllers;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using System.Security.Claims;
using Xunit;

namespace OnlineQuiz.Tests.Controllers
{
    internal class FakeActivityLogService3 : IActivityLogService
    {
        public Task<ActivityLogDto> LogActivityAsync(CreateActivityLogDto dto)
            => Task.FromResult(new ActivityLogDto { ActivityLogId = 3, UserId = dto.UserId, Action = dto.Action, Entity = dto.Entity, CreatedAt = DateTime.UtcNow });
        public Task<List<ActivityLogDto>> GetActivityLogsAsync(ActivityLogFilterDto filter) => Task.FromResult(new List<ActivityLogDto>());
        public Task<List<ActivityLogDto>> GetUserActivityLogsAsync(int userId, int? days = null) => Task.FromResult(new List<ActivityLogDto>());
        public Task<ActivityLogStatisticsDto> GetActivityStatisticsAsync(int? userId = null, int? days = 30) => Task.FromResult(new ActivityLogStatisticsDto());
        public Task<ActivityLogDto?> GetActivityLogByIdAsync(long activityLogId) => Task.FromResult<ActivityLogDto?>(null);
    }

    internal class FakeUserService : IUserService
    {
        public bool ThrowArgumentOnCreate { get; set; }
        public bool ThrowInvalidOpOnUpdate { get; set; }
        public bool ThrowInvalidOpOnDelete { get; set; }
        public bool ReturnEmptyUsers { get; set; }

        public Task<UserResponseDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            if (ThrowArgumentOnCreate) throw new ArgumentException("Invalid user payload");
            return Task.FromResult(new UserResponseDto
            {
                UserId = 1000,
                Email = createUserDto.Email,
                FullName = createUserDto.FullName,
                RoleId = createUserDto.RoleId,
                RoleName = createUserDto.RoleId == 1 ? "Admin" : createUserDto.RoleId == 2 ? "Teacher" : "Student",
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        public Task<UserResponseDto?> GetUserByIdAsync(int userId)
            => Task.FromResult<UserResponseDto?>(new UserResponseDto { UserId = userId, Email = "user@example.com", FullName = "Test User", RoleId = 3, RoleName = "Student", Status = "Active" });

        public Task<List<UserResponseDto>> GetAllUsersAsync()
            => Task.FromResult(ReturnEmptyUsers ? new List<UserResponseDto>() : new List<UserResponseDto> { new UserResponseDto { UserId = 1, Email = "a@a.com", FullName = "A" } });

        public Task<PagedResult<UserResponseDto>> GetAllUsersPagedAsync(PaginationParams paginationParams)
            => Task.FromResult(new PagedResult<UserResponseDto>
            {
                Items = new List<UserResponseDto> { new UserResponseDto { UserId = 2, Email = "b@b.com", FullName = "B" } },
                TotalCount = 1,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize
            });

        public Task<UserResponseDto> UpdateUserAsync(int userId, UpdateUserDto updateUserDto)
        {
            if (ThrowInvalidOpOnUpdate) throw new InvalidOperationException("User not found");
            return Task.FromResult(new UserResponseDto
            {
                UserId = userId,
                Email = updateUserDto.Email ?? "updated@example.com",
                FullName = updateUserDto.FullName ?? "Updated Name",
                RoleId = 3,
                RoleName = "Student",
                Status = updateUserDto.Status ?? "Active",
                UpdatedAt = DateTime.UtcNow
            });
        }

        public Task<bool> DeleteUserAsync(int userId)
        {
            if (ThrowInvalidOpOnDelete) throw new InvalidOperationException("User not found");
            return Task.FromResult(true);
        }

        public Task<int> BulkDeleteAsync(List<int> userIds) => Task.FromResult(userIds.Count);

        public Task ResetPasswordAsync(int userId, string newPassword)
        {
            if (ThrowInvalidOpOnUpdate) throw new InvalidOperationException("User not found");
            return Task.CompletedTask;
        }

        public Task<BulkUserImportResultDto> BulkCreateUsersFromExcelAsync(Stream fileStream, string fileName, int createdByUserId)
            => Task.FromResult(new BulkUserImportResultDto
            {
                SuccessCount = 1,
                FailureCount = 0,
                TotalRows = 1,
                LogId = 10,
                CreatedUsers = new List<UserResponseDto> { new UserResponseDto { UserId = 25, Email = "new@user.com", FullName = "New User", RoleId = 3, RoleName = "Student" } }
            });

        // Archive stubs
        public Task<UserResponseDto> ArchiveUserAsync(int userId, int archivedBy)
            => Task.FromResult(new UserResponseDto { UserId = userId, Email = "user@example.com", FullName = "User", Status = "Archived" });
        public Task<UserResponseDto> UnarchiveUserAsync(int userId)
            => Task.FromResult(new UserResponseDto { UserId = userId, Email = "user@example.com", FullName = "User", Status = "Active" });
        public Task<BulkArchiveResponseDto> BulkArchiveUsersAsync(List<int> userIds, int archivedBy)
            => Task.FromResult(new BulkArchiveResponseDto { TotalRequested = userIds.Count, SuccessCount = userIds.Count });
        public Task<BulkArchiveResponseDto> BulkUnarchiveUsersAsync(List<int> userIds)
            => Task.FromResult(new BulkArchiveResponseDto { TotalRequested = userIds.Count, SuccessCount = userIds.Count });
        public Task<List<UserResponseDto>> GetArchivedUsersAsync()
            => Task.FromResult(new List<UserResponseDto>());
        public Task<PagedResult<UserResponseDto>> GetArchivedUsersPagedAsync(PaginationParams paginationParams)
            => Task.FromResult(new PagedResult<UserResponseDto> { Items = new List<UserResponseDto>(), TotalCount = 0, PageNumber = paginationParams.PageNumber, PageSize = paginationParams.PageSize });
        public Task<ArchiveStatisticsDto> GetUserArchiveStatisticsAsync()
            => Task.FromResult(new ArchiveStatisticsDto());
    }

    [Collection("MapsterWarmup")]
    public class UserControllerTests
    {
        private static UserController CreateController(IUserService userService, ClaimsIdentity identity)
        {
            var controller = new UserController(userService, new FakeActivityLogService3());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
            return controller;
        }

        private static ClaimsIdentity AdminIdentity()
            => new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "100"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("RoleId", "1")
            }, "TestAuth");

        private static ClaimsIdentity StudentIdentity()
            => new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "101"),
                new Claim(ClaimTypes.Role, "Student"),
                new Claim("RoleId", "3")
            }, "TestAuth");

        [Fact]
        public async Task CreateUser_ReturnsCreated_WhenValid()
        {
            var service = new FakeUserService();
            var controller = CreateController(service, AdminIdentity());
            var dto = new CreateUserDto { Email = "new@user.com", Password = "secret123", FullName = "New User", RoleId = 3 };

            var result = await controller.CreateUser(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var user = Assert.IsType<UserResponseDto>(created.Value);
            Assert.Equal("new@user.com", user.Email);
            Assert.Equal("New User", user.FullName);
        }

        [Fact]
        public async Task CreateUser_ReturnsBadRequest_OnArgumentException()
        {
            var service = new FakeUserService { ThrowArgumentOnCreate = true };
            var controller = CreateController(service, AdminIdentity());
            var dto = new CreateUserDto { Email = "bad@user.com", Password = "x", FullName = "Bad", RoleId = 3 };

            var result = await controller.CreateUser(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task UpdateUser_ReturnsOk_WhenValid()
        {
            var service = new FakeUserService();
            var controller = CreateController(service, AdminIdentity());
            var dto = new UpdateUserDto { Email = "updated@user.com", FullName = "Updated" };

            var result = await controller.UpdateUser(25, dto);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var user = Assert.IsType<UserResponseDto>(ok.Value);
            Assert.Equal("updated@user.com", user.Email);
            Assert.Equal("Updated", user.FullName);
        }

        [Fact]
        public async Task UpdateUser_ReturnsNotFound_OnInvalidOperation()
        {
            var service = new FakeUserService { ThrowInvalidOpOnUpdate = true };
            var controller = CreateController(service, AdminIdentity());
            var dto = new UpdateUserDto { Email = "missing@user.com" };

            var result = await controller.UpdateUser(9999, dto);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFound.Value);
        }

        [Fact]
        public async Task GetAllUsers_ReturnsNotFound_WhenEmpty()
        {
            var service = new FakeUserService { ReturnEmptyUsers = true };
            var controller = CreateController(service, AdminIdentity());

            var result = await controller.GetAllUsers();

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFound.Value);
        }

        [Fact]
        public async Task GetAllUsersPaged_ReturnsOk_WithOneItem()
        {
            var service = new FakeUserService();
            var controller = CreateController(service, AdminIdentity());

            var result = await controller.GetAllUsersPaged(pageNumber: 1, pageSize: 5);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var paged = Assert.IsType<PagedResult<UserResponseDto>>(ok.Value);
            Assert.Equal(1, paged.TotalCount);
            Assert.Single(paged.Items);
        }

        [Fact]
        public async Task BulkDeleteUsers_ReturnsNoContent_WhenAdmin()
        {
            var service = new FakeUserService();
            var controller = CreateController(service, AdminIdentity());
            var dto = new BulkDeleteUsersDto { UserIds = new List<int> { 1, 2, 3 } };

            var result = await controller.BulkDeleteUsers(dto);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task BulkImportUsers_ReturnsUnauthorized_WhenNoUserIdClaim()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "TestAuth");
            var controller = CreateController(new FakeUserService(), identity);

            // Provide a valid Excel file to pass file validation and hit auth check
            var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            var file = new FormFile(stream, 0, stream.Length, name: "file", fileName: "users.xlsx");

            var result = await controller.BulkImportUsers(file);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.NotNull(unauthorized.Value);
        }

        [Fact]
        public async Task BulkImportUsers_ReturnsBadRequest_WhenInvalidExtension()
        {
            var controller = CreateController(new FakeUserService(), AdminIdentity());
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var file = new FormFile(stream, 0, stream.Length, name: "file", fileName: "users.txt");

            var result = await controller.BulkImportUsers(file);

            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task BulkImportUsers_ReturnsOk_WhenAdminWithValidFile()
        {
            var controller = CreateController(new FakeUserService(), AdminIdentity());
            var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            var file = new FormFile(stream, 0, stream.Length, name: "file", fileName: "users.xlsx");

            var result = await controller.BulkImportUsers(file);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var payload = Assert.IsType<BulkUserImportResultDto>(ok.Value);
            Assert.Equal(1, payload.SuccessCount);
            Assert.Equal(0, payload.FailureCount);
        }

        [Fact]
        public async Task ResetPassword_ReturnsOk_WhenAdmin()
        {
            var controller = CreateController(new FakeUserService(), AdminIdentity());
            var dto = new ResetPasswordDto { NewPassword = "newSecret123" };

            var result = await controller.ResetPassword(25, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task ResetPassword_ReturnsNotFound_OnInvalidOperation()
        {
            var controller = CreateController(new FakeUserService { ThrowInvalidOpOnUpdate = true }, AdminIdentity());
            var dto = new ResetPasswordDto { NewPassword = "newSecret123" };

            var result = await controller.ResetPassword(9999, dto);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFound.Value);
        }
    }
}