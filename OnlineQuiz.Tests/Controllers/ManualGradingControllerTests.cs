using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.Controllers;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using OnlineQuiz.Tests.Fakes;
using Xunit;

namespace OnlineQuiz.Tests.Controllers
{
    internal class FakeManualGradingService : IManualGradingService
    {
        public bool ThrowUnauthorized { get; set; }
        public bool ThrowArgument { get; set; }
        public bool ThrowInvalidOp { get; set; }

        public Task<AnswerResponseDto> GradeEssayAnswerAsync(int attemptAnswerId, GradeEssayAnswerDto gradeDto, int teacherId)
        {
            if (ThrowUnauthorized) throw new UnauthorizedAccessException("Not the instructor");
            if (ThrowArgument) throw new ArgumentException("Answer not found");
            if (ThrowInvalidOp) throw new InvalidOperationException("Attempt not submitted");
            return Task.FromResult(new AnswerResponseDto { AnswerId = attemptAnswerId, AttemptId = 1, QuestionId = 1, IsCorrect = gradeDto.IsCorrect });
        }

        public Task<List<AnswerResponseDto>> BulkGradeEssayAnswersAsync(BulkGradeEssayDto bulkGradeDto, int teacherId)
        {
            if (ThrowUnauthorized) throw new UnauthorizedAccessException("Not the instructor");
            if (ThrowArgument) throw new ArgumentException("Attempt not found");
            if (ThrowInvalidOp) throw new InvalidOperationException("Attempt not submitted");
            var results = bulkGradeDto.Grades.Select(g => new AnswerResponseDto { AnswerId = g.AttemptAnswerId, AttemptId = bulkGradeDto.AttemptId, QuestionId = 1, IsCorrect = g.IsCorrect }).ToList();
            return Task.FromResult(results);
        }

        public Task<List<EssayAnswerForGradingDto>> GetPendingEssayAnswersForQuizAsync(int quizId, int teacherId)
        {
            if (ThrowUnauthorized) throw new UnauthorizedAccessException("Not the instructor");
            if (ThrowArgument) throw new ArgumentException("Quiz not found");
            return Task.FromResult(new List<EssayAnswerForGradingDto>
            {
                new EssayAnswerForGradingDto { AttemptAnswerId = 1, AttemptId = 1, QuestionBody = "Explain X", StudentAnswer = "My answer", StudentName = "Alice" }
            });
        }

        public Task<List<PendingEssayGradingDto>> GetAllPendingEssayAnswersForTeacherAsync(int teacherId)
        {
            return Task.FromResult(new List<PendingEssayGradingDto>
            {
                new PendingEssayGradingDto { QuizId = 1, QuizTitle = "Quiz 1", CourseId = 1, CourseName = "Math", PendingCount = 2 }
            });
        }

        public Task<AttemptResponseDto> RecalculateAttemptScoreAsync(int attemptId, int teacherId)
        {
            if (ThrowUnauthorized) throw new UnauthorizedAccessException("Not the instructor");
            if (ThrowArgument) throw new ArgumentException("Attempt not found");
            if (ThrowInvalidOp) throw new InvalidOperationException("Attempt not submitted");
            return Task.FromResult(new AttemptResponseDto { AttemptId = attemptId, Score = 75m });
        }
    }

    [Collection("MapsterWarmup")]
    public class ManualGradingControllerTests
    {
        private static ManualGradingController CreateController(
            FakeManualGradingService? svc = null,
            int userId = 10)
        {
            var controller = new ManualGradingController(
                svc ?? new FakeManualGradingService(),
                new FakeActivityLogService());

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };
            return controller;
        }

        private static ManualGradingController CreateControllerNoAuth()
        {
            var controller = new ManualGradingController(new FakeManualGradingService(), new FakeActivityLogService());
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            return controller;
        }

        // ── GradeEssayAnswer ──────────────────────────────────────────────

        [Fact]
        public async Task GradeEssayAnswer_ReturnsOk_WhenValid()
        {
            var controller = CreateController(userId: 10);
            var dto = new GradeEssayAnswerDto { AttemptAnswerId = 5, IsCorrect = true, PointsAwarded = 8 };
            var result = await controller.GradeEssayAnswer(5, dto);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GradeEssayAnswer_ReturnsUnauthorized_WhenNoUserIdClaim()
        {
            var controller = CreateControllerNoAuth();
            var result = await controller.GradeEssayAnswer(1, new GradeEssayAnswerDto { IsCorrect = true });
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GradeEssayAnswer_Returns403_WhenWrongTeacher()
        {
            var controller = CreateController(new FakeManualGradingService { ThrowUnauthorized = true });
            var result = await controller.GradeEssayAnswer(1, new GradeEssayAnswerDto { IsCorrect = true });
            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, status.StatusCode);
        }

        [Fact]
        public async Task GradeEssayAnswer_ReturnsNotFound_WhenAnswerMissing()
        {
            var controller = CreateController(new FakeManualGradingService { ThrowArgument = true });
            var result = await controller.GradeEssayAnswer(999, new GradeEssayAnswerDto { IsCorrect = true });
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFound.Value);
        }

        [Fact]
        public async Task GradeEssayAnswer_ReturnsBadRequest_WhenAttemptNotSubmitted()
        {
            var controller = CreateController(new FakeManualGradingService { ThrowInvalidOp = true });
            var result = await controller.GradeEssayAnswer(1, new GradeEssayAnswerDto { IsCorrect = true });
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(bad.Value);
        }

        // ── BulkGradeEssayAnswers ─────────────────────────────────────────

        [Fact]
        public async Task BulkGradeEssayAnswers_ReturnsOk_WhenValid()
        {
            var controller = CreateController(userId: 10);
            var dto = new BulkGradeEssayDto
            {
                AttemptId = 1,
                Grades = new List<GradeEssayAnswerDto>
                {
                    new GradeEssayAnswerDto { AttemptAnswerId = 1, IsCorrect = true },
                    new GradeEssayAnswerDto { AttemptAnswerId = 2, IsCorrect = false }
                }
            };
            var result = await controller.BulkGradeEssayAnswers(1, dto);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task BulkGradeEssayAnswers_ReturnsUnauthorized_WhenNoUserIdClaim()
        {
            var controller = CreateControllerNoAuth();
            var result = await controller.BulkGradeEssayAnswers(1, new BulkGradeEssayDto { Grades = new() });
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── GetPendingEssayAnswersForQuiz ─────────────────────────────────

        [Fact]
        public async Task GetPendingEssayAnswersForQuiz_ReturnsOk_WithPendingList()
        {
            var controller = CreateController(userId: 10);
            var result = await controller.GetPendingEssayAnswersForQuiz(quizId: 5);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetPendingEssayAnswersForQuiz_Returns403_WhenWrongTeacher()
        {
            var controller = CreateController(new FakeManualGradingService { ThrowUnauthorized = true });
            var result = await controller.GetPendingEssayAnswersForQuiz(quizId: 5);
            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, status.StatusCode);
        }

        [Fact]
        public async Task GetPendingEssayAnswersForQuiz_ReturnsNotFound_WhenQuizMissing()
        {
            var controller = CreateController(new FakeManualGradingService { ThrowArgument = true });
            var result = await controller.GetPendingEssayAnswersForQuiz(quizId: 999);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ── GetAllPendingEssayAnswers ──────────────────────────────────────

        [Fact]
        public async Task GetAllPendingEssayAnswers_ReturnsOk_WithSummary()
        {
            var controller = CreateController(userId: 10);
            var result = await controller.GetAllPendingEssayAnswers();
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetAllPendingEssayAnswers_ReturnsUnauthorized_WhenNoUserIdClaim()
        {
            var controller = CreateControllerNoAuth();
            var result = await controller.GetAllPendingEssayAnswers();
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── RecalculateAttemptScore ───────────────────────────────────────

        [Fact]
        public async Task RecalculateAttemptScore_ReturnsOk_WhenValid()
        {
            var controller = CreateController(userId: 10);
            var result = await controller.RecalculateAttemptScore(attemptId: 3);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task RecalculateAttemptScore_Returns403_WhenWrongTeacher()
        {
            var controller = CreateController(new FakeManualGradingService { ThrowUnauthorized = true });
            var result = await controller.RecalculateAttemptScore(attemptId: 3);
            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, status.StatusCode);
        }

        [Fact]
        public async Task RecalculateAttemptScore_ReturnsNotFound_WhenAttemptMissing()
        {
            var controller = CreateController(new FakeManualGradingService { ThrowArgument = true });
            var result = await controller.RecalculateAttemptScore(attemptId: 999);
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
