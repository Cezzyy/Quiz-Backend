using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.Controllers;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using OnlineQuiz.Tests.Fakes;
using Xunit;

namespace OnlineQuiz.Tests.Controllers
{
    internal class FakeAttemptService : IAttemptService
    {
        public bool ThrowSubmitUnauthorized { get; set; }
        public bool ThrowSubmitClosedQuiz { get; set; }
        public bool ThrowSubmitTimeout { get; set; }
        public bool ThrowSubmitInvalidPayload { get; set; }
        public Task<AttemptResponseDto> StartAttemptAsync(StartAttemptDto startAttemptDto)
        {
            return Task.FromResult(new AttemptResponseDto
            {
                AttemptId = 123,
                UserId = startAttemptDto.StudentId,
                StudentName = "Ada Lovelace",
                QuizId = startAttemptDto.QuizId,
                QuizTitle = "Algorithms",
                StartedAt = DateTime.UtcNow
            });
        }

        public Task<AttemptResponseDto?> GetAttemptByIdAsync(int attemptId, int userId)
        {
            if (attemptId == 404) return Task.FromResult<AttemptResponseDto?>(null);
            return Task.FromResult<AttemptResponseDto?>(new AttemptResponseDto
            {
                AttemptId = attemptId,
                UserId = userId,
                StudentName = "Grace Hopper",
                QuizId = 5,
                QuizTitle = "Compilers",
                StartedAt = DateTime.UtcNow
            });
        }

        public Task<List<AttemptResponseDto>> GetAttemptsForQuizAsync(int quizId, int teacherId)
        {
            return Task.FromResult(new List<AttemptResponseDto>
            {
                new AttemptResponseDto { AttemptId = 1, UserId = 10, QuizId = quizId, StartedAt = DateTime.UtcNow },
                new AttemptResponseDto { AttemptId = 2, UserId = 11, QuizId = quizId, StartedAt = DateTime.UtcNow },
            });
        }

        public Task<PagedResult<AttemptResponseDto>> GetAttemptsForQuizPagedAsync(int quizId, int teacherId, PaginationParams paginationParams)
        {
            var items = new List<AttemptResponseDto>
            {
                new AttemptResponseDto { AttemptId = 1, UserId = 10, QuizId = quizId, StartedAt = DateTime.UtcNow }
            };
            return Task.FromResult(new PagedResult<AttemptResponseDto>
            {
                Items = items,
                TotalCount = items.Count,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize
            });
        }

        public Task<List<AttemptResponseDto>> GetAttemptsForStudentAsync(int studentId)
        {
            return Task.FromResult(new List<AttemptResponseDto>
            {
                new AttemptResponseDto { AttemptId = 1, UserId = studentId, QuizId = 7, StartedAt = DateTime.UtcNow }
            });
        }

        public Task<PagedResult<AttemptResponseDto>> GetAttemptsForStudentPagedAsync(int studentId, PaginationParams paginationParams)
        {
            var items = new List<AttemptResponseDto>
            {
                new AttemptResponseDto { AttemptId = 1, UserId = studentId, QuizId = 8, StartedAt = DateTime.UtcNow }
            };
            return Task.FromResult(new PagedResult<AttemptResponseDto>
            {
                Items = items,
                TotalCount = items.Count,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize
            });
        }

        public Task<AttemptResponseDto> SubmitAttemptAsync(int attemptId, SubmitAttemptDto submitAttemptDto, int studentId)
        {
            if (ThrowSubmitUnauthorized)
                throw new UnauthorizedAccessException("Not authorized to submit attempt");
            if (ThrowSubmitClosedQuiz)
                throw new InvalidOperationException("Quiz is closed");
            if (ThrowSubmitTimeout)
                throw new InvalidOperationException("Attempt timed out");
            if (ThrowSubmitInvalidPayload)
                throw new InvalidOperationException("Invalid submit payload");

            return Task.FromResult(new AttemptResponseDto
            {
                AttemptId = attemptId,
                UserId = studentId,
                QuizId = 9,
                SubmittedAt = DateTime.UtcNow,
                Score = 95.5m,
                TimeSpentSeconds = submitAttemptDto.TimeSpentSeconds
            });
        }

        public Task<bool> DeleteAttemptAsync(int attemptId, int userId)
        {
            if (attemptId == 404) throw new ArgumentException("Attempt not found");
            return Task.FromResult(true);
        }

        public Task<int> BulkDeleteAttemptsAsync(List<int> attemptIds, int userId)
        {
            return Task.FromResult(attemptIds.Count);
        }

        public Task<(byte[] FileContent, string FileName)> ExportQuizScoresToExcelAsync(int userId, int? quizId, int? courseId)
        {
            var content = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // PK.. zip header (xlsx)
            var fileName = "scores.xlsx";
            return Task.FromResult((content, fileName));
        }
    }

    [Collection("MapsterWarmup")]
    public class AttemptControllerTests
    {
        private static AttemptController CreateController(FakeAttemptService? attemptService = null, FakeActivityLogService? activityLogService = null)
        {
            var controller = new AttemptController(attemptService ?? new FakeAttemptService(), activityLogService ?? new FakeActivityLogService());
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            return controller;
        }

        [Fact]
        public async Task StartAttempt_ReturnsCreatedAt()
        {
            var controller = CreateController();
            var dto = new StartAttemptDto { QuizId = 7, StudentId = 42 };
            var result = await controller.StartAttempt(dto);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var attempt = Assert.IsType<AttemptResponseDto>(created.Value);
            Assert.Equal(42, attempt.UserId);
            Assert.Equal(7, attempt.QuizId);
        }

        [Fact]
        public async Task GetAttemptById_ReturnsOk_WhenFound()
        {
            var controller = CreateController();
            var result = await controller.GetAttemptById(attemptId: 5, userId: 42);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var attempt = Assert.IsType<AttemptResponseDto>(ok.Value);
            Assert.Equal(5, attempt.AttemptId);
        }

        [Fact]
        public async Task GetAttemptById_ReturnsNotFound_WhenMissing()
        {
            var controller = CreateController();
            var result = await controller.GetAttemptById(attemptId: 404, userId: 42);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetAttemptsForQuiz_ReturnsOk_List()
        {
            var controller = CreateController();
            var result = await controller.GetAttemptsForQuiz(quizId: 3, teacherId: 1);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var attempts = Assert.IsType<List<AttemptResponseDto>>(ok.Value);
            Assert.NotEmpty(attempts);
        }

        [Fact]
        public async Task GetAttemptsForStudent_ReturnsOk_List()
        {
            var controller = CreateController();
            var result = await controller.GetAttemptsForStudent(studentId: 42);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var attempts = Assert.IsType<List<AttemptResponseDto>>(ok.Value);
            Assert.NotEmpty(attempts);
        }

        [Fact]
        public async Task GetAttemptsForQuizPaged_ReturnsOk_Paged()
        {
            var controller = CreateController();
            var result = await controller.GetAttemptsForQuizPaged(quizId: 3, teacherId: 1, pageNumber: 1, pageSize: 10);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var paged = Assert.IsType<PagedResult<AttemptResponseDto>>(ok.Value);
            Assert.Equal(1, paged.PageNumber);
            Assert.True(paged.Items.Count > 0);
        }

        [Fact]
        public async Task GetAttemptsForStudentPaged_ReturnsOk_Paged()
        {
            var controller = CreateController();
            var result = await controller.GetAttemptsForStudentPaged(studentId: 42, pageNumber: 1, pageSize: 10);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var paged = Assert.IsType<PagedResult<AttemptResponseDto>>(ok.Value);
            Assert.Equal(1, paged.PageNumber);
            Assert.True(paged.Items.Count > 0);
        }

        [Fact]
        public async Task SubmitAttempt_ReturnsOk_WithScore()
        {
            var controller = CreateController();
            var dto = new SubmitAttemptDto { TimeSpentSeconds = 120 };
            var result = await controller.SubmitAttempt(attemptId: 9, submitAttemptDto: dto, studentId: 42);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var attempt = Assert.IsType<AttemptResponseDto>(ok.Value);
            Assert.Equal(95.5m, attempt.Score);
            Assert.Equal(120, attempt.TimeSpentSeconds);
        }

        [Fact]
        public async Task DeleteAttempt_ReturnsNoContent_OnSuccess()
        {
            var controller = CreateController();
            var result = await controller.DeleteAttempt(attemptId: 10, userId: 1);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteAttempt_ReturnsNotFound_OnArgumentException()
        {
            var controller = CreateController();
            var result = await controller.DeleteAttempt(attemptId: 404, userId: 1);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task BulkDeleteAttempts_ReturnsNoContent()
        {
            var controller = CreateController();
            var dto = new BulkDeleteAttemptsDto { AttemptIds = new List<int> { 1, 2 }, UserId = 1 };
            var result = await controller.BulkDeleteAttempts(dto);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task ExportScores_ReturnsFile_WithContentType()
        {
            var controller = CreateController();
            var result = await controller.ExportScores(userId: 1, quizId: 2, courseId: null);
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
            Assert.NotNull(fileResult.FileContents);
            Assert.True(fileResult.FileContents.Length > 0);
        }

        [Fact]
        public async Task SubmitAttempt_ReturnsUnauthorized_OnUnauthorizedAccess()
        {
            var fakeService = new FakeAttemptService { ThrowSubmitUnauthorized = true };
            var controller = CreateController(fakeService);
            var dto = new SubmitAttemptDto { TimeSpentSeconds = 120 };

            var result = await controller.SubmitAttempt(1, dto, studentId: 42);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.NotNull(unauthorized.Value);
        }

        [Fact]
        public async Task SubmitAttempt_ReturnsBadRequest_WhenQuizClosed()
        {
            var fakeService = new FakeAttemptService { ThrowSubmitClosedQuiz = true };
            var controller = CreateController(fakeService);
            var dto = new SubmitAttemptDto { TimeSpentSeconds = 120 };

            var result = await controller.SubmitAttempt(1, dto, studentId: 42);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task SubmitAttempt_ReturnsBadRequest_WhenAttemptTimedOut()
        {
            var fakeService = new FakeAttemptService { ThrowSubmitTimeout = true };
            var controller = CreateController(fakeService);
            var dto = new SubmitAttemptDto { TimeSpentSeconds = 120 };

            var result = await controller.SubmitAttempt(2, dto, studentId: 99);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task SubmitAttempt_ReturnsBadRequest_OnInvalidPayload()
        {
            var fakeService = new FakeAttemptService { ThrowSubmitInvalidPayload = true };
            var controller = CreateController(fakeService);
            var dto = new SubmitAttemptDto { TimeSpentSeconds = 120 };

            var result = await controller.SubmitAttempt(3, dto, studentId: 10);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }
    }
}