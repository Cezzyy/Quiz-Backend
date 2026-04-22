using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;
using OnlineQuiz.Utilities;
using Xunit;

namespace OnlineQuiz.Tests.Services
{
    [Collection("MapsterWarmup")]
    public class ManualGradingServiceTests
    {
        // ── Shared in-memory fakes ────────────────────────────────────────

        private class InMemoryAnswerRepository : IAttemptAnswerRepository
        {
            private readonly Dictionary<int, AttemptAnswer> _store = new();
            private int _nextId = 1;

            public AttemptAnswer Seed(AttemptAnswer a) { a.AttemptAnswerId = _nextId++; _store[a.AttemptAnswerId] = Clone(a); return Clone(a); }
            public Task<AttemptAnswer> CreateAsync(AttemptAnswer a) { a.AttemptAnswerId = _nextId++; _store[a.AttemptAnswerId] = Clone(a); return Task.FromResult(Clone(a)); }
            public Task<AttemptAnswer?> GetByIdAsync(int id) { _store.TryGetValue(id, out var a); return Task.FromResult(a != null ? Clone(a) : null); }
            public Task<List<AttemptAnswer>> GetByAttemptIdAsync(int attemptId) => Task.FromResult(_store.Values.Where(a => a.AttemptId == attemptId).Select(Clone).ToList());
            public Task<List<AttemptAnswer>> GetByAttemptIdsAsync(List<int> ids) => Task.FromResult(_store.Values.Where(a => ids.Contains(a.AttemptId)).Select(Clone).ToList());
            public Task<List<AttemptAnswer>> GetByIdsAsync(List<int> ids) => Task.FromResult(_store.Values.Where(a => ids.Contains(a.AttemptAnswerId)).Select(Clone).ToList());
            public Task<AttemptAnswer> UpdateAsync(AttemptAnswer a) { _store[a.AttemptAnswerId] = Clone(a); return Task.FromResult(Clone(a)); }
            public Task<bool> DeleteAsync(int id) => Task.FromResult(_store.Remove(id));
            private static AttemptAnswer Clone(AttemptAnswer a) => new AttemptAnswer { AttemptAnswerId = a.AttemptAnswerId, AttemptId = a.AttemptId, QuestionId = a.QuestionId, ChoiceId = a.ChoiceId, FreeText = a.FreeText, IsCorrect = a.IsCorrect, PointsAwarded = a.PointsAwarded, Feedback = a.Feedback };
        }

        private class InMemoryAttemptRepository : IAttemptRepository
        {
            private readonly Dictionary<int, Attempt> _store = new();
            private int _nextId = 1;
            public Attempt Seed(Attempt a) { a.AttemptId = _nextId++; _store[a.AttemptId] = Clone(a); return Clone(a); }
            public Task<Attempt> CreateAsync(Attempt a) { a.AttemptId = _nextId++; _store[a.AttemptId] = Clone(a); return Task.FromResult(Clone(a)); }
            public Task<Attempt?> GetByIdAsync(int id) { _store.TryGetValue(id, out var a); return Task.FromResult(a != null ? Clone(a) : null); }
            public Task<List<Attempt>> GetByQuizIdAsync(int quizId) => Task.FromResult(_store.Values.Where(a => a.QuizId == quizId).Select(Clone).ToList());
            public Task<List<Attempt>> GetByQuizIdsAsync(List<int> ids) => Task.FromResult(_store.Values.Where(a => ids.Contains(a.QuizId)).Select(Clone).ToList());
            public Task<List<Attempt>> GetByStudentIdAsync(int studentId) => Task.FromResult(_store.Values.Where(a => a.UserId == studentId).Select(Clone).ToList());
            public Task<List<Attempt>> GetByQuizIdAndUserIdAsync(int quizId, int userId) => Task.FromResult(_store.Values.Where(a => a.QuizId == quizId && a.UserId == userId).Select(Clone).ToList());
            public Task<Attempt> UpdateAsync(Attempt a) { _store[a.AttemptId] = Clone(a); return Task.FromResult(Clone(a)); }
            public Task<bool> DeleteAsync(int id) => Task.FromResult(_store.Remove(id));
            public Task<double> GetAverageScoreByCourseAsync(int courseId) => Task.FromResult(0.0);
            public Task<List<Attempt>> GetRecentAttemptsByStudentAsync(int studentId, int count) => Task.FromResult(new List<Attempt>());
            public Task<Dictionary<int, double>> GetAverageScoresByCourseIdsAsync(List<int> ids) => Task.FromResult(new Dictionary<int, double>());
            public Task<List<Attempt>> GetByQuizIdsAndStudentIdAsync(List<int> quizIds, int studentId) => Task.FromResult(new List<Attempt>());
            public Task<int> BulkDeleteAsync(List<int> ids) => Task.FromResult(0);
            public Task<List<Attempt>> GetAllAttemptsForExportAsync(int? quizId, int? courseId) => Task.FromResult(new List<Attempt>());
            private static Attempt Clone(Attempt a) => new Attempt { AttemptId = a.AttemptId, QuizId = a.QuizId, UserId = a.UserId, StartedAt = a.StartedAt, SubmittedAt = a.SubmittedAt, Score = a.Score, TimeSpentSeconds = a.TimeSpentSeconds };
        }

        private class InMemoryQuizRepository : IQuizRepository
        {
            private readonly Dictionary<int, Quiz> _quizzes = new();
            private readonly Dictionary<int, List<Question>> _questions = new();
            private int _nextQuizId = 1, _nextQId = 1;
            public Quiz SeedQuiz(Quiz q) { q.QuizId = _nextQuizId++; _quizzes[q.QuizId] = q; return q; }
            public Question SeedQuestion(int quizId, Question q) { q.QuestionId = _nextQId++; q.QuizId = quizId; if (!_questions.ContainsKey(quizId)) _questions[quizId] = new(); _questions[quizId].Add(q); return q; }
            public Task<Quiz?> GetByIdAsync(int id) { _quizzes.TryGetValue(id, out var q); return Task.FromResult(q); }
            public Task<List<Question>> GetQuestionsByQuizIdAsync(int quizId) => Task.FromResult(_questions.TryGetValue(quizId, out var qs) ? qs.ToList() : new List<Question>());
            public Task<List<Question>> GetQuestionsByQuizIdsAsync(List<int> ids) { var r = new List<Question>(); foreach (var id in ids) if (_questions.TryGetValue(id, out var qs)) r.AddRange(qs); return Task.FromResult(r); }
            public Task<List<Quiz>> GetByCourseIdsAsync(List<int> ids) => Task.FromResult(_quizzes.Values.Where(q => ids.Contains(q.CourseId)).ToList());
            public Task<Quiz> CreateAsync(Quiz q) { q.QuizId = _nextQuizId++; _quizzes[q.QuizId] = q; return Task.FromResult(q); }
            public Task<List<Quiz>> GetByCourseIdAsync(int courseId) => Task.FromResult(_quizzes.Values.Where(q => q.CourseId == courseId).ToList());
            public Task<Quiz> UpdateAsync(Quiz q) { _quizzes[q.QuizId] = q; return Task.FromResult(q); }
            public Task<bool> DeleteAsync(int id) => Task.FromResult(_quizzes.Remove(id));
            public Task<Question> CreateQuestionAsync(Question q) { q.QuestionId = _nextQId++; if (!_questions.ContainsKey(q.QuizId)) _questions[q.QuizId] = new(); _questions[q.QuizId].Add(q); return Task.FromResult(q); }
            public Task<Choice> CreateChoiceAsync(Choice c) => Task.FromResult(c);
            public Task<List<Choice>> GetChoicesByQuestionIdAsync(int qId) => Task.FromResult(new List<Choice>());
            public Task<List<Choice>> GetChoicesByQuestionIdsAsync(List<int> ids) => Task.FromResult(new List<Choice>());
            public Task<List<Quiz>> GetByIdsAsync(List<int> ids) => Task.FromResult(_quizzes.Values.Where(q => ids.Contains(q.QuizId)).ToList());
            public Task<List<Quiz>> GetUpcomingDeadlinesAsync(DateTime t) => Task.FromResult(new List<Quiz>());
            public Task<int> CountAsync() => Task.FromResult(_quizzes.Count);
            public Task<int> CountByCourseAsync(int courseId) => Task.FromResult(_quizzes.Values.Count(q => q.CourseId == courseId));
            public Task<int> CountByCourseIdsAsync(List<int> ids) => Task.FromResult(0);
            public Task<int> BulkDeleteAsync(List<int> ids) => Task.FromResult(0);
            public Task<Quiz?> ArchiveAsync(int id, int by) => Task.FromResult<Quiz?>(null);
            public Task<Quiz?> UnarchiveAsync(int id) => Task.FromResult<Quiz?>(null);
            public Task<int> BulkArchiveAsync(List<int> ids, int by) => Task.FromResult(0);
            public Task<int> BulkUnarchiveAsync(List<int> ids) => Task.FromResult(0);
            public Task<List<Quiz>> GetArchivedAsync() => Task.FromResult(new List<Quiz>());
            public Task<List<Quiz>> GetArchivedByCourseIdAsync(int id) => Task.FromResult(new List<Quiz>());
            public Task<List<Quiz>> GetAllIncludingArchivedAsync() => Task.FromResult(_quizzes.Values.ToList());
            public Task<int> CountArchivedAsync() => Task.FromResult(0);
        }

        private class InMemoryCourseRepository : ICourseRepository
        {
            private readonly Dictionary<int, Course> _store = new();
            private int _nextId = 1;
            public Course Seed(Course c) { c.CourseId = _nextId++; _store[c.CourseId] = c; return c; }
            public Task<Course?> GetByIdAsync(int id) { _store.TryGetValue(id, out var c); return Task.FromResult(c); }
            public Task<List<Course>> GetByInstructorIdAsync(int id) => Task.FromResult(_store.Values.Where(c => c.InstructorUserId == id).ToList());
            public Task<Course> CreateAsync(Course c) { c.CourseId = _nextId++; _store[c.CourseId] = c; return Task.FromResult(c); }
            public Task<List<Course>> GetAllAsync() => Task.FromResult(_store.Values.ToList());
            public Task<Course> UpdateAsync(Course c) { _store[c.CourseId] = c; return Task.FromResult(c); }
            public Task<bool> DeleteAsync(int id) => Task.FromResult(_store.Remove(id));
            public Task<List<Course>> GetByStudentIdAsync(int id) => Task.FromResult(new List<Course>());
            public Task<int> CountAsync() => Task.FromResult(_store.Count);
            public Task<int> CountByInstructorAsync(int id) => Task.FromResult(0);
            public Task<int> BulkDeleteAsync(List<int> ids) => Task.FromResult(0);
            public Task<Course?> ArchiveAsync(int id, int by) => Task.FromResult<Course?>(null);
            public Task<Course?> UnarchiveAsync(int id) => Task.FromResult<Course?>(null);
            public Task<int> BulkArchiveAsync(List<int> ids, int by) => Task.FromResult(0);
            public Task<int> BulkUnarchiveAsync(List<int> ids) => Task.FromResult(0);
            public Task<List<Course>> GetArchivedAsync() => Task.FromResult(new List<Course>());
            public Task<List<Course>> GetAllIncludingArchivedAsync() => Task.FromResult(_store.Values.ToList());
            public Task<int> CountArchivedAsync() => Task.FromResult(0);
        }

        private class InMemoryUserRepository : IUserRepository
        {
            private readonly Dictionary<int, User> _store = new();
            public void Seed(User u) => _store[u.UserId] = u;
            public Task<User?> GetByIdAsync(int id) { _store.TryGetValue(id, out var u); return Task.FromResult(u); }
            public Task<User?> GetByEmailAsync(string e) => Task.FromResult<User?>(null);
            public Task<List<User>> GetAllAsync() => Task.FromResult(_store.Values.ToList());
            public Task<List<User>> GetByIdsAsync(List<int> ids) => Task.FromResult(_store.Values.Where(u => ids.Contains(u.UserId)).ToList());
            public Task<User> CreateAsync(User u) { _store[u.UserId] = u; return Task.FromResult(u); }
            public Task<User> UpdateAsync(User u) { _store[u.UserId] = u; return Task.FromResult(u); }
            public Task<bool> DeleteAsync(int id) => Task.FromResult(_store.Remove(id));
            public Task<int> BulkDeleteAsync(List<int> ids) => Task.FromResult(0);
            public Task<int> CountAsync() => Task.FromResult(_store.Count);
            public Task<int> CountByRoleAsync(int roleId) => Task.FromResult(0);
            public Task<List<User>> GetRecentRegistrationsAsync(int days) => Task.FromResult(new List<User>());
            public Task<User?> ArchiveAsync(int id, int by) => Task.FromResult<User?>(null);
            public Task<User?> UnarchiveAsync(int id) => Task.FromResult<User?>(null);
            public Task<int> BulkArchiveAsync(List<int> ids, int by) => Task.FromResult(0);
            public Task<int> BulkUnarchiveAsync(List<int> ids) => Task.FromResult(0);
            public Task<List<User>> GetArchivedAsync() => Task.FromResult(new List<User>());
            public Task<List<User>> GetAllIncludingArchivedAsync() => Task.FromResult(_store.Values.ToList());
            public Task<int> CountArchivedAsync() => Task.FromResult(0);
        }

        private ManualGradingService CreateService(
            InMemoryAnswerRepository answers,
            InMemoryAttemptRepository attempts,
            InMemoryQuizRepository quizzes,
            InMemoryCourseRepository courses,
            InMemoryUserRepository? users = null)
            => new ManualGradingService(answers, attempts, quizzes, courses, users ?? new InMemoryUserRepository());

        // ── Helpers to build a standard test scenario ─────────────────────

        private (InMemoryAnswerRepository answers, InMemoryAttemptRepository attempts,
                 InMemoryQuizRepository quizzes, InMemoryCourseRepository courses,
                 InMemoryUserRepository users,
                 Course course, Quiz quiz, Question essayQ, Attempt attempt, AttemptAnswer answer)
            BuildScenario(int teacherId = 1, bool submitted = true)
        {
            var answers  = new InMemoryAnswerRepository();
            var attempts = new InMemoryAttemptRepository();
            var quizzes  = new InMemoryQuizRepository();
            var courses  = new InMemoryCourseRepository();
            var users    = new InMemoryUserRepository();

            var course = courses.Seed(new Course { InstructorUserId = teacherId, Name = "Math", Code = "M101", Status = "Active", CreatedBy = teacherId });
            var quiz   = quizzes.SeedQuiz(new Quiz { CourseId = course.CourseId, Title = "Quiz 1", IsPublished = true, Status = "Active", CreatedBy = teacherId });
            var essayQ = quizzes.SeedQuestion(quiz.QuizId, new Question { Body = "Explain X", Type = QuestionTypeConstants.Text, Points = 10 });

            users.Seed(new User { UserId = 42, FullName = "Alice", Email = "a@a.com", PasswordHash = "x", Status = "Active" });

            var attempt = attempts.Seed(new Attempt
            {
                QuizId = quiz.QuizId, UserId = 42,
                StartedAt = DateTime.UtcNow.AddMinutes(-30),
                SubmittedAt = submitted ? DateTime.UtcNow : null
            });

            var answer = answers.Seed(new AttemptAnswer
            {
                AttemptId = attempt.AttemptId,
                QuestionId = essayQ.QuestionId,
                FreeText = "My essay answer",
                IsCorrect = null
            });

            return (answers, attempts, quizzes, courses, users, course, quiz, essayQ, attempt, answer);
        }

        // ── GradeEssayAnswerAsync ─────────────────────────────────────────

        [Fact]
        public async Task GradeEssayAnswer_Succeeds_ForAuthorizedTeacher()
        {
            var (answers, attempts, quizzes, courses, users, _, _, essayQ, _, answer) = BuildScenario(teacherId: 1);
            var svc = CreateService(answers, attempts, quizzes, courses, users);

            var gradeDto = new GradeEssayAnswerDto { AttemptAnswerId = answer.AttemptAnswerId, IsCorrect = true, PointsAwarded = 8, Feedback = "Good" };
            var result = await svc.GradeEssayAnswerAsync(answer.AttemptAnswerId, gradeDto, teacherId: 1);

            Assert.True(result.IsCorrect);
        }

        [Fact]
        public async Task GradeEssayAnswer_Throws_WhenAnswerNotFound()
        {
            var (answers, attempts, quizzes, courses, users, _, _, _, _, _) = BuildScenario();
            var svc = CreateService(answers, attempts, quizzes, courses, users);

            var gradeDto = new GradeEssayAnswerDto { AttemptAnswerId = 999, IsCorrect = true };
            await Assert.ThrowsAsync<ArgumentException>(() => svc.GradeEssayAnswerAsync(999, gradeDto, teacherId: 1));
        }

        [Fact]
        public async Task GradeEssayAnswer_Throws_WhenAttemptNotSubmitted()
        {
            var (answers, attempts, quizzes, courses, users, _, _, _, _, answer) = BuildScenario(submitted: false);
            var svc = CreateService(answers, attempts, quizzes, courses, users);

            var gradeDto = new GradeEssayAnswerDto { AttemptAnswerId = answer.AttemptAnswerId, IsCorrect = true };
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.GradeEssayAnswerAsync(answer.AttemptAnswerId, gradeDto, teacherId: 1));
        }

        [Fact]
        public async Task GradeEssayAnswer_Throws_WhenWrongTeacher()
        {
            var (answers, attempts, quizzes, courses, users, _, _, _, _, answer) = BuildScenario(teacherId: 1);
            var svc = CreateService(answers, attempts, quizzes, courses, users);

            var gradeDto = new GradeEssayAnswerDto { AttemptAnswerId = answer.AttemptAnswerId, IsCorrect = true };
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.GradeEssayAnswerAsync(answer.AttemptAnswerId, gradeDto, teacherId: 99));
        }

        [Fact]
        public async Task GradeEssayAnswer_Throws_WhenPointsExceedMaximum()
        {
            var (answers, attempts, quizzes, courses, users, _, _, essayQ, _, answer) = BuildScenario(teacherId: 1);
            var svc = CreateService(answers, attempts, quizzes, courses, users);

            // essayQ.Points = 10, awarding 15 should fail
            var gradeDto = new GradeEssayAnswerDto { AttemptAnswerId = answer.AttemptAnswerId, IsCorrect = true, PointsAwarded = 15 };
            await Assert.ThrowsAsync<ArgumentException>(() => svc.GradeEssayAnswerAsync(answer.AttemptAnswerId, gradeDto, teacherId: 1));
        }

        [Fact]
        public async Task GradeEssayAnswer_Throws_WhenQuestionIsNotEssayType()
        {
            var answers  = new InMemoryAnswerRepository();
            var attempts = new InMemoryAttemptRepository();
            var quizzes  = new InMemoryQuizRepository();
            var courses  = new InMemoryCourseRepository();

            var course  = courses.Seed(new Course { InstructorUserId = 1, Name = "C", Code = "C1", Status = "Active", CreatedBy = 1 });
            var quiz    = quizzes.SeedQuiz(new Quiz { CourseId = course.CourseId, Title = "Q", IsPublished = true, Status = "Active", CreatedBy = 1 });
            var mcQ     = quizzes.SeedQuestion(quiz.QuizId, new Question { Body = "Pick one", Type = QuestionTypeConstants.Single, Points = 5 });
            var attempt = attempts.Seed(new Attempt { QuizId = quiz.QuizId, UserId = 1, StartedAt = DateTime.UtcNow, SubmittedAt = DateTime.UtcNow });
            var answer  = answers.Seed(new AttemptAnswer { AttemptId = attempt.AttemptId, QuestionId = mcQ.QuestionId, ChoiceId = 1 });

            var svc = CreateService(answers, attempts, quizzes, courses);
            var gradeDto = new GradeEssayAnswerDto { AttemptAnswerId = answer.AttemptAnswerId, IsCorrect = true };
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.GradeEssayAnswerAsync(answer.AttemptAnswerId, gradeDto, teacherId: 1));
        }

        // ── RecalculateAttemptScoreAsync ──────────────────────────────────

        [Fact]
        public async Task RecalculateAttemptScore_Throws_WhenAttemptNotSubmitted()
        {
            var (answers, attempts, quizzes, courses, users, _, _, _, attempt, _) = BuildScenario(submitted: false);
            var svc = CreateService(answers, attempts, quizzes, courses, users);
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.RecalculateAttemptScoreAsync(attempt.AttemptId, teacherId: 1));
        }

        [Fact]
        public async Task RecalculateAttemptScore_Throws_WhenWrongTeacher()
        {
            var (answers, attempts, quizzes, courses, users, _, _, _, attempt, _) = BuildScenario(teacherId: 1);
            var svc = CreateService(answers, attempts, quizzes, courses, users);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.RecalculateAttemptScoreAsync(attempt.AttemptId, teacherId: 99));
        }

        [Fact]
        public async Task RecalculateAttemptScore_CalculatesCorrectly_WithPartialCredit()
        {
            var (answers, attempts, quizzes, courses, users, _, _, essayQ, attempt, answer) = BuildScenario(teacherId: 1);
            var svc = CreateService(answers, attempts, quizzes, courses, users);

            // Award 5 out of 10 points
            var gradeDto = new GradeEssayAnswerDto { AttemptAnswerId = answer.AttemptAnswerId, IsCorrect = false, PointsAwarded = 5 };
            await svc.GradeEssayAnswerAsync(answer.AttemptAnswerId, gradeDto, teacherId: 1);

            var result = await svc.RecalculateAttemptScoreAsync(attempt.AttemptId, teacherId: 1);

            Assert.Equal(50m, result.Score); // 5/10 = 50%
        }

        // ── GetPendingEssayAnswersForQuizAsync ────────────────────────────

        [Fact]
        public async Task GetPendingEssayAnswers_ReturnsEmpty_WhenNoSubmittedAttempts()
        {
            var (answers, attempts, quizzes, courses, users, _, quiz, _, _, _) = BuildScenario(submitted: false);
            var svc = CreateService(answers, attempts, quizzes, courses, users);
            var result = await svc.GetPendingEssayAnswersForQuizAsync(quiz.QuizId, teacherId: 1);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPendingEssayAnswers_Throws_WhenWrongTeacher()
        {
            var (answers, attempts, quizzes, courses, users, _, quiz, _, _, _) = BuildScenario(teacherId: 1);
            var svc = CreateService(answers, attempts, quizzes, courses, users);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.GetPendingEssayAnswersForQuizAsync(quiz.QuizId, teacherId: 99));
        }

        [Fact]
        public async Task GetPendingEssayAnswers_ReturnsPendingAnswers()
        {
            var (answers, attempts, quizzes, courses, users, _, quiz, _, _, _) = BuildScenario(teacherId: 1);
            var svc = CreateService(answers, attempts, quizzes, courses, users);
            var result = await svc.GetPendingEssayAnswersForQuizAsync(quiz.QuizId, teacherId: 1);
            Assert.Single(result);
            Assert.Null(result[0].IsCorrect); // not yet graded
        }
    }
}
