using Mapster;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.IServices;
using OnlineQuiz.Models;

namespace OnlineQuiz.Services
{
    public class AttemptService : IAttemptService
    {
        private readonly IAttemptRepository _attemptRepository;
        private readonly IQuizRepository _quizRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IUserRepository _userRepository;

        public AttemptService(
            IAttemptRepository attemptRepository,
            IQuizRepository quizRepository,
            ICourseRepository courseRepository,
            IEnrollmentRepository enrollmentRepository,
            IUserRepository userRepository)
        {
            _attemptRepository = attemptRepository;
            _quizRepository = quizRepository;
            _courseRepository = courseRepository;
            _enrollmentRepository = enrollmentRepository;
            _userRepository = userRepository;
        }

        public async Task<AttemptResponseDto> StartAttemptAsync(StartAttemptDto startAttemptDto)
        {
            var quiz = await _quizRepository.GetByIdAsync(startAttemptDto.QuizId);
            if (quiz == null)
            {
                throw new ArgumentException($"Quiz with ID {startAttemptDto.QuizId} not found");
            }

            if (!quiz.IsPublished)
            {
                throw new InvalidOperationException("Cannot start attempt on unpublished quiz");
            }

            // Check if student is enrolled
            var isEnrolled = await _enrollmentRepository.ExistsAsync(startAttemptDto.StudentId, quiz.CourseId);
            if (!isEnrolled)
            {
                throw new UnauthorizedAccessException("Student is not enrolled in this course");
            }

            var attempt = new Attempt
            {
                QuizId = startAttemptDto.QuizId,
                UserId = startAttemptDto.StudentId,
                StartedAt = DateTime.UtcNow
            };

            var createdAttempt = await _attemptRepository.CreateAsync(attempt);
            var student = await _userRepository.GetByIdAsync(startAttemptDto.StudentId);

            return new AttemptResponseDto
            {
                AttemptId = createdAttempt.AttemptId,
                UserId = createdAttempt.UserId,
                StudentName = student?.FullName,
                QuizId = createdAttempt.QuizId,
                QuizTitle = quiz.Title,
                StartedAt = createdAttempt.StartedAt,
                SubmittedAt = createdAttempt.SubmittedAt,
                Score = createdAttempt.Score,
                TimeSpentSeconds = createdAttempt.TimeSpentSeconds
            };
        }

        public async Task<AttemptResponseDto?> GetAttemptByIdAsync(int attemptId, int userId)
        {
            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null)
            {
                return null;
            }

            var quiz = await _quizRepository.GetByIdAsync(attempt.QuizId);
            var student = await _userRepository.GetByIdAsync(attempt.UserId);

            return new AttemptResponseDto
            {
                AttemptId = attempt.AttemptId,
                UserId = attempt.UserId,
                StudentName = student?.FullName,
                QuizId = attempt.QuizId,
                QuizTitle = quiz?.Title,
                StartedAt = attempt.StartedAt,
                SubmittedAt = attempt.SubmittedAt,
                Score = attempt.Score,
                TimeSpentSeconds = attempt.TimeSpentSeconds
            };
        }

        public async Task<List<AttemptResponseDto>> GetAttemptsForQuizAsync(int quizId, int teacherId)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            if (quiz == null)
            {
                throw new ArgumentException($"Quiz with ID {quizId} not found");
            }

            var course = await _courseRepository.GetByIdAsync(quiz.CourseId);
            if (course == null || course.InstructorUserId != teacherId)
            {
                throw new UnauthorizedAccessException("Only the course instructor can view quiz attempts");
            }

            var attempts = await _attemptRepository.GetByQuizIdAsync(quizId);
            var response = new List<AttemptResponseDto>();

            // Collect user IDs
            var userIds = attempts.Select(a => a.UserId).Distinct().ToList();
            
            // Batch fetch students
            var students = await _userRepository.GetByIdsAsync(userIds);
            var studentMap = students.ToDictionary(u => u.UserId, u => u.FullName);

            foreach (var attempt in attempts)
            {
                string? studentName = null;
                if (studentMap.TryGetValue(attempt.UserId, out var name))
                {
                    studentName = name;
                }

                response.Add(new AttemptResponseDto
                {
                    AttemptId = attempt.AttemptId,
                    UserId = attempt.UserId,
                    StudentName = studentName,
                    QuizId = attempt.QuizId,
                    QuizTitle = quiz.Title,
                    StartedAt = attempt.StartedAt,
                    SubmittedAt = attempt.SubmittedAt,
                    Score = attempt.Score,
                    TimeSpentSeconds = attempt.TimeSpentSeconds
                });
            }

            return response;
        }

        public async Task<List<AttemptResponseDto>> GetAttemptsForStudentAsync(int studentId)
        {
            var attempts = await _attemptRepository.GetByStudentIdAsync(studentId);
            var response = new List<AttemptResponseDto>();

            if (!attempts.Any()) return response;

            // Collect IDs
            var quizIds = attempts.Select(a => a.QuizId).Distinct().ToList();
            var userIds = attempts.Select(a => a.UserId).Distinct().ToList();

            // Batch fetch
            var quizzes = await _quizRepository.GetByIdsAsync(quizIds);
            var students = await _userRepository.GetByIdsAsync(userIds);

            var quizMap = quizzes.ToDictionary(q => q.QuizId, q => q.Title);
            var studentMap = students.ToDictionary(u => u.UserId, u => u.FullName);

            foreach (var attempt in attempts)
            {
                string? quizTitle = null;
                if (quizMap.TryGetValue(attempt.QuizId, out var title))
                {
                    quizTitle = title;
                }

                string? studentName = null;
                if (studentMap.TryGetValue(attempt.UserId, out var name))
                {
                    studentName = name;
                }

                response.Add(new AttemptResponseDto
                {
                    AttemptId = attempt.AttemptId,
                    UserId = attempt.UserId,
                    StudentName = studentName,
                    QuizId = attempt.QuizId,
                    QuizTitle = quizTitle,
                    StartedAt = attempt.StartedAt,
                    SubmittedAt = attempt.SubmittedAt,
                    Score = attempt.Score,
                    TimeSpentSeconds = attempt.TimeSpentSeconds
                });
            }

            return response;
        }

        public async Task<AttemptResponseDto> SubmitAttemptAsync(int attemptId, SubmitAttemptDto submitAttemptDto, int studentId)
        {
            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null)
            {
                throw new ArgumentException($"Attempt with ID {attemptId} not found");
            }

            if (attempt.UserId != studentId)
            {
                throw new UnauthorizedAccessException("You can only submit your own attempts");
            }

            if (attempt.SubmittedAt != null)
            {
                throw new InvalidOperationException("Attempt has already been submitted");
            }

            attempt.SubmittedAt = DateTime.UtcNow;
            attempt.Score = submitAttemptDto.Score;
            attempt.TimeSpentSeconds = submitAttemptDto.TimeSpentSeconds;

            var updatedAttempt = await _attemptRepository.UpdateAsync(attempt);
            var quiz = await _quizRepository.GetByIdAsync(updatedAttempt.QuizId);
            var student = await _userRepository.GetByIdAsync(updatedAttempt.UserId);

            return new AttemptResponseDto
            {
                AttemptId = updatedAttempt.AttemptId,
                UserId = updatedAttempt.UserId,
                StudentName = student?.FullName,
                QuizId = updatedAttempt.QuizId,
                QuizTitle = quiz?.Title,
                StartedAt = updatedAttempt.StartedAt,
                SubmittedAt = updatedAttempt.SubmittedAt,
                Score = updatedAttempt.Score,
                TimeSpentSeconds = updatedAttempt.TimeSpentSeconds
            };
        }
    }
}
