using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.Controllers;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using OnlineQuiz.Tests.Fakes;
using OnlineQuiz.Utilities;
using Xunit;

namespace OnlineQuiz.Tests.Controllers
{
    internal class FakeQuizService : IQuizService
    {
        public bool ThrowCreateUnauthorized { get; set; }
        public bool ThrowCreateValidation { get; set; }
        public bool ThrowUpdateArgument { get; set; }
        public bool ThrowUpdateUnauthorized { get; set; }
        public bool ThrowDeleteUnauthorized { get; set; }
        public bool ThrowBulkDeleteUnauthorized { get; set; }

        public Task<QuizResponseDto> CreateQuizAsync(CreateQuizDto createQuizDto)
        {
            if (ThrowCreateUnauthorized) throw new UnauthorizedAccessException("Not allowed to create quiz");
            if (ThrowCreateValidation) throw new InvalidOperationException("Invalid quiz data");
            return Task.FromResult(new QuizResponseDto
            {
                QuizId = 100,
                CourseId = createQuizDto.CourseId,
                Title = createQuizDto.Title,
                DueAt = createQuizDto.DueAt,
                TimeLimitMinutes = createQuizDto.TimeLimitMinutes,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow,
                Questions = new()
            });
        }

        public Task<List<QuizResponseDto>> GetQuizzesForCourseAsync(int courseId, int userId, bool isStudent)
        {
            if (courseId == 1)
                return Task.FromResult(new List<QuizResponseDto>
                {
                    new QuizResponseDto { QuizId = 1, CourseId = 1, Title = "Quiz A", CreatedAt = DateTime.UtcNow, IsPublished = true },
                    new QuizResponseDto { QuizId = 2, CourseId = 1, Title = "Quiz B", CreatedAt = DateTime.UtcNow, IsPublished = false },
                });
            return Task.FromResult(new List<QuizResponseDto>());
        }

        public Task<PagedResult<QuizResponseDto>> GetQuizzesForCoursePagedAsync(int courseId, int userId, bool isStudent, PaginationParams paginationParams)
        {
            var items = new List<QuizResponseDto>
            {
                new QuizResponseDto { QuizId = 1, CourseId = courseId, Title = "Paged Quiz", CreatedAt = DateTime.UtcNow }
            };
            return Task.FromResult(new PagedResult<QuizResponseDto>
            {
                Items = items, TotalCount = items.Count,
                PageNumber = paginationParams.PageNumber, PageSize = paginationParams.PageSize
            });
        }

        public Task<QuizResponseDto?> GetQuizByIdAsync(int quizId)
        {
            if (quizId == 404) return Task.FromResult<QuizResponseDto?>(null);
            return Task.FromResult<QuizResponseDto?>(new QuizResponseDto { QuizId = quizId, CourseId = 1, Title = "Found Quiz", CreatedAt = DateTime.UtcNow });
        }

        public Task<QuizResponseDto> UpdateQuizAsync(int quizId, UpdateQuizDto updateQuizDto, int userId)
        {
            if (ThrowUpdateArgument) throw new ArgumentException("Quiz not found");
            if (ThrowUpdateUnauthorized) throw new UnauthorizedAccessException("Not authorized to update");
            return Task.FromResult(new QuizResponseDto
            {
                QuizId = quizId, CourseId = 1,
                Title = updateQuizDto.Title ?? "Updated Quiz",
                DueAt = updateQuizDto.DueAt,
                TimeLimitMinutes = updateQuizDto.TimeLimitMinutes,
                IsPublished = updateQuizDto.IsPublished ?? false,
                CreatedAt = DateTime.UtcNow
            });
        }

        public Task<bool> DeleteQuizAsync(int quizId, int userId)
        {
            if (ThrowDeleteUnauthorized) throw new UnauthorizedAccessException("Not authorized to delete");
            if (quizId == 404) return Task.FromResult(false);
            return Task.FromResult(true);
        }

        public Task<int> BulkDeleteQuizzesAsync(List<int> quizIds, int userId)
        {
            if (ThrowBulkDeleteUnauthorized) throw new UnauthorizedAccessException("Not authorized to bulk delete");
            return Task.FromResult(quizIds.Count);
        }

        // Archive stubs
        public Task<QuizResponseDto> ArchiveQuizAsync(int quizId, int userId, int archivedBy)
            => Task.FromResult(new QuizResponseDto { QuizId = quizId, CourseId = 1, Title = "Archived Quiz", Status = "Archived", CreatedAt = DateTime.UtcNow });
        public Task<QuizResponseDto> UnarchiveQuizAsync(int quizId, int userId)
            => Task.FromResult(new QuizResponseDto { QuizId = quizId, CourseId = 1, Title = "Restored Quiz", Status = "Active", CreatedAt = DateTime.UtcNow });
        public Task<BulkArchiveResponseDto> BulkArchiveQuizzesAsync(List<int> quizIds, int userId, int archivedBy)
            => Task.FromResult(new BulkArchiveResponseDto { TotalRequested = quizIds.Count, SuccessCount = quizIds.Count });
        public Task<BulkArchiveResponseDto> BulkUnarchiveQuizzesAsync(List<int> quizIds, int userId)
            => Task.FromResult(new BulkArchiveResponseDto { TotalRequested = quizIds.Count, SuccessCount = quizIds.Count });
        public Task<List<QuizResponseDto>> GetArchivedQuizzesAsync(int courseId, int userId)
            => Task.FromResult(new List<QuizResponseDto>());
        public Task<PagedResult<QuizResponseDto>> GetArchivedQuizzesPagedAsync(int courseId, int userId, PaginationParams paginationParams)
            => Task.FromResult(new PagedResult<QuizResponseDto> { Items = new List<QuizResponseDto>(), TotalCount = 0, PageNumber = paginationParams.PageNumber, PageSize = paginationParams.PageSize });
        public Task<ArchiveStatisticsDto> GetQuizArchiveStatisticsAsync(int? courseId = null)
            => Task.FromResult(new ArchiveStatisticsDto());
    }

    [Collection("MapsterWarmup")]
    public class QuizControllerTests
    {
        // Controller now reads userId from JWT claims — helper sets up the identity
        private static QuizController CreateController(
            FakeQuizService? quizService = null,
            int userId = 10,
            string role = "Teacher")
        {
            var controller = new QuizController(
                quizService ?? new FakeQuizService(),
                new FakeActivityLogService());

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("RoleName", role)
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

        // ── CreateQuiz ────────────────────────────────────────────────────

        [Fact]
        public async Task CreateQuiz_ReturnsCreatedAt_WithQuiz()
        {
            var controller = CreateController();
            var dto = new CreateQuizDto { CourseId = 1, Title = "New Quiz", CreatedBy = 10, TimeLimitMinutes = 30 };

            var result = await controller.CreateQuiz(dto);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var quiz = Assert.IsType<QuizResponseDto>(created.Value);
            Assert.Equal("New Quiz", quiz.Title);
            Assert.Equal(1, quiz.CourseId);
        }

        [Fact]
        public async Task CreateQuiz_ReturnsUnauthorized_OnUnauthorizedAccess()
        {
            var controller = CreateController(new FakeQuizService { ThrowCreateUnauthorized = true });
            var result = await controller.CreateQuiz(new CreateQuizDto { CourseId = 1, Title = "Quiz", CreatedBy = 10 });
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.NotNull(unauthorized.Value);
        }

        [Fact]
        public async Task CreateQuiz_ReturnsBadRequest_OnValidationError()
        {
            var controller = CreateController(new FakeQuizService { ThrowCreateValidation = true });
            var result = await controller.CreateQuiz(new CreateQuizDto { CourseId = 1, Title = "Bad Quiz", CreatedBy = 10 });
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        // ── GetQuizzesForCourse ───────────────────────────────────────────
        // Controller now extracts userId + role from JWT — no parameters

        [Fact]
        public async Task GetQuizzesForCourse_ReturnsEmptyList_WhenNone()
        {
            var controller = CreateController(role: "Teacher");
            var result = await controller.GetQuizzesForCourse(courseId: 999);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var quizzes = Assert.IsType<List<QuizResponseDto>>(ok.Value);
            Assert.Empty(quizzes);
        }

        [Fact]
        public async Task GetQuizzesForCourse_ReturnsOk_WithItems()
        {
            var controller = CreateController(role: "Teacher");
            var result = await controller.GetQuizzesForCourse(courseId: 1);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var quizzes = Assert.IsType<List<QuizResponseDto>>(ok.Value);
            Assert.NotEmpty(quizzes);
        }

        [Fact]
        public async Task GetQuizzesForCourse_ReturnsUnauthorized_WhenNoUserIdClaim()
        {
            var controller = new QuizController(new FakeQuizService(), new FakeActivityLogService());
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var result = await controller.GetQuizzesForCourse(courseId: 1);
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetQuizzesForCoursePaged_ReturnsOk_WithPagedResult()
        {
            var controller = CreateController(role: "Teacher");
            var result = await controller.GetQuizzesForCoursePaged(courseId: 5, pageNumber: 1, pageSize: 10);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var paged = Assert.IsType<PagedResult<QuizResponseDto>>(ok.Value);
            Assert.Equal(1, paged.PageNumber);
            Assert.True(paged.Items.Count > 0);
        }

        // ── GetQuizById ───────────────────────────────────────────────────

        [Fact]
        public async Task GetQuizById_ReturnsNotFound_WhenMissing()
        {
            var controller = CreateController();
            var result = await controller.GetQuizById(404);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetQuizById_ReturnsOk_WhenFound()
        {
            var controller = CreateController();
            var result = await controller.GetQuizById(7);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var quiz = Assert.IsType<QuizResponseDto>(ok.Value);
            Assert.Equal(7, quiz.QuizId);
        }

        // ── UpdateQuiz ────────────────────────────────────────────────────
        // Controller now extracts userId from JWT — no userId parameter

        [Fact]
        public async Task UpdateQuiz_ReturnsOk_WithUpdatedFields()
        {
            var controller = CreateController(userId: 99);
            var updateDto = new UpdateQuizDto { Title = "Updated", IsPublished = true, TimeLimitMinutes = 60 };
            var result = await controller.UpdateQuiz(quizId: 3, updateQuizDto: updateDto);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var quiz = Assert.IsType<QuizResponseDto>(ok.Value);
            Assert.Equal("Updated", quiz.Title);
            Assert.True(quiz.IsPublished);
            Assert.Equal(60, quiz.TimeLimitMinutes);
        }

        [Fact]
        public async Task UpdateQuiz_ReturnsNotFound_OnArgumentException()
        {
            var controller = CreateController(new FakeQuizService { ThrowUpdateArgument = true });
            var result = await controller.UpdateQuiz(quizId: 999, updateQuizDto: new UpdateQuizDto { Title = "Title" });
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFound.Value);
        }

        [Fact]
        public async Task UpdateQuiz_ReturnsUnauthorized_OnUnauthorizedAccessException()
        {
            var controller = CreateController(new FakeQuizService { ThrowUpdateUnauthorized = true });
            var result = await controller.UpdateQuiz(quizId: 3, updateQuizDto: new UpdateQuizDto { Title = "Title" });
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.NotNull(unauthorized.Value);
        }

        [Fact]
        public async Task UpdateQuiz_ReturnsUnauthorized_WhenNoUserIdClaim()
        {
            var controller = new QuizController(new FakeQuizService(), new FakeActivityLogService());
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var result = await controller.UpdateQuiz(quizId: 1, updateQuizDto: new UpdateQuizDto { Title = "T" });
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        // ── DeleteQuiz ────────────────────────────────────────────────────
        // Controller now extracts userId from JWT — no userId parameter

        [Fact]
        public async Task DeleteQuiz_ReturnsNoContent_WhenDeleted()
        {
            var controller = CreateController(userId: 1);
            var result = await controller.DeleteQuiz(quizId: 10);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteQuiz_ReturnsNotFound_WhenMissing()
        {
            var controller = CreateController(userId: 1);
            var result = await controller.DeleteQuiz(quizId: 404);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteQuiz_ReturnsUnauthorized_OnUnauthorizedAccess()
        {
            var controller = CreateController(new FakeQuizService { ThrowDeleteUnauthorized = true }, userId: 1);
            var result = await controller.DeleteQuiz(quizId: 10);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(unauthorized.Value);
        }

        [Fact]
        public async Task DeleteQuiz_ReturnsUnauthorized_WhenNoUserIdClaim()
        {
            var controller = new QuizController(new FakeQuizService(), new FakeActivityLogService());
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var result = await controller.DeleteQuiz(quizId: 1);
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── BulkDeleteQuizzes ─────────────────────────────────────────────
        // BulkDeleteQuizzesDto no longer has UserId — controller reads from JWT

        [Fact]
        public async Task BulkDeleteQuizzes_ReturnsNoContent()
        {
            var controller = CreateController(userId: 1);
            var dto = new BulkDeleteQuizzesDto { QuizIds = new List<int> { 1, 2, 3 } };
            var result = await controller.BulkDeleteQuizzes(dto);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task BulkDeleteQuizzes_ReturnsUnauthorized_OnUnauthorizedAccess()
        {
            var controller = CreateController(new FakeQuizService { ThrowBulkDeleteUnauthorized = true }, userId: 1);
            var dto = new BulkDeleteQuizzesDto { QuizIds = new List<int> { 1, 2 } };
            var result = await controller.BulkDeleteQuizzes(dto);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(unauthorized.Value);
        }

        [Fact]
        public async Task BulkDeleteQuizzes_ReturnsUnauthorized_WhenNoUserIdClaim()
        {
            var controller = new QuizController(new FakeQuizService(), new FakeActivityLogService());
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var dto = new BulkDeleteQuizzesDto { QuizIds = new List<int> { 1 } };
            var result = await controller.BulkDeleteQuizzes(dto);
            Assert.IsType<UnauthorizedObjectResult>(result);
        }
    }
}
