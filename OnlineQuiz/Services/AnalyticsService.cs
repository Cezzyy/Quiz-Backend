using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.Models;

namespace OnlineQuiz.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IQuizRepository _quizRepository;
        private readonly IAttemptRepository _attemptRepository;
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;

        public AnalyticsService(
            IUserRepository userRepository,
            ICourseRepository courseRepository,
            IQuizRepository quizRepository,
            IAttemptRepository attemptRepository,
            IActivityLogRepository activityLogRepository,
            IEnrollmentRepository enrollmentRepository)
        {
            _userRepository = userRepository;
            _courseRepository = courseRepository;
            _quizRepository = quizRepository;
            _attemptRepository = attemptRepository;
            _activityLogRepository = activityLogRepository;
            _enrollmentRepository = enrollmentRepository;
        }

        public async Task<AdminDashboardDto> GetAdminDashboardAsync()
        {
            // Fetch all counts and recent data in parallel
            var totalUsersTask = _userRepository.CountAsync();
            var totalCoursesTask = _courseRepository.CountAsync();
            var totalQuizzesTask = _quizRepository.CountAsync();
            var recentRegistrationsTask = _userRepository.GetRecentRegistrationsAsync(7);
            var recentActivitiesTask = _activityLogRepository.GetRecentAsync(10);

            await Task.WhenAll(totalUsersTask, totalCoursesTask, totalQuizzesTask, recentRegistrationsTask, recentActivitiesTask);

            var totalUsers = await totalUsersTask;
            var recentRegistrations = await recentRegistrationsTask;
            var recentActivities = await recentActivitiesTask;
            
            return new AdminDashboardDto
            {
                TotalUsers = totalUsers,
                TotalCourses = await totalCoursesTask,
                TotalQuizzes = await totalQuizzesTask,
                ActiveUsers = totalUsers, // Placeholder
                RecentRegistrations = recentRegistrations
                    .GroupBy(u => u.CreatedAt.Date)
                    .Select(g => new RegistrationStatDto { Date = g.Key, Count = g.Count() })
                    .OrderBy(r => r.Date)
                    .ToList(),
                RecentActivities = recentActivities.Select(a => new ActivityLogDto
                {
                    ActivityLogId = a.ActivityLogId,
                    UserId = a.UserId,
                    Action = a.Action,
                    Entity = a.Entity,
                    Description = a.Description,
                    CreatedAt = a.CreatedAt
                }).ToList()
            };
        }

        public async Task<TeacherDashboardDto> GetTeacherDashboardAsync(int teacherId)
        {
            var courses = await _courseRepository.GetByInstructorIdAsync(teacherId);
            var courseIds = courses.Select(c => c.CourseId).ToList();
            
            if (!courseIds.Any())
            {
                return new TeacherDashboardDto
                {
                    TotalCourses = 0,
                    TotalQuizzes = 0,
                    TotalStudents = 0,
                    PendingGrading = 0,
                    CoursePerformance = new List<CoursePerformanceDto>()
                };
            }

            // Fetch all data in parallel using bulk queries
            var totalQuizzesTask = _quizRepository.CountByCourseIdsAsync(courseIds);
            var studentCountsTask = _enrollmentRepository.CountByCourseIdsAsync(courseIds);
            var avgScoresTask = _attemptRepository.GetAverageScoresByCourseIdsAsync(courseIds);

            await Task.WhenAll(totalQuizzesTask, studentCountsTask, avgScoresTask);

            var totalQuizzes = await totalQuizzesTask;
            var studentCounts = await studentCountsTask;
            var avgScores = await avgScoresTask;
            var totalStudents = studentCounts.Values.Sum();
            
            var coursePerformance = courses.Select(course => new CoursePerformanceDto
            {
                CourseId = course.CourseId,
                CourseName = course.Name,
                StudentCount = studentCounts.GetValueOrDefault(course.CourseId, 0),
                AverageScore = avgScores.GetValueOrDefault(course.CourseId, 0)
            }).ToList();

            return new TeacherDashboardDto
            {
                TotalCourses = courses.Count,
                TotalQuizzes = totalQuizzes,
                TotalStudents = totalStudents,
                PendingGrading = 0, // Placeholder
                CoursePerformance = coursePerformance
            };
        }

        public async Task<StudentDashboardDto> GetStudentDashboardAsync(int studentId)
        {
            // Fetch enrolled courses, all attempts, and recent attempts in parallel
            var enrolledCoursesTask = _courseRepository.GetByStudentIdAsync(studentId);
            var allAttemptsTask = _attemptRepository.GetByStudentIdAsync(studentId);
            var recentAttemptsTask = _attemptRepository.GetRecentAttemptsByStudentAsync(studentId, 5);

            await Task.WhenAll(enrolledCoursesTask, allAttemptsTask, recentAttemptsTask);

            var enrolledCourses = await enrolledCoursesTask;
            var allAttempts = await allAttemptsTask;
            var recentAttempts = await recentAttemptsTask;
            var avgScore = allAttempts.Any() ? allAttempts.Average(a => a.Score) : 0;
            
            // Get all quizzes for enrolled courses in one query
            var courseIds = enrolledCourses.Select(c => c.CourseId).ToList();
            var allQuizzes = courseIds.Any() 
                ? await _quizRepository.GetByCourseIdsAsync(courseIds) 
                : new List<Quiz>();

            // Create course lookup for performance
            var courseLookup = enrolledCourses.ToDictionary(c => c.CourseId);
            var quizLookup = allQuizzes.ToDictionary(q => q.QuizId);

            // Filter upcoming published quizzes with due dates
            var upcomingQuizCandidates = allQuizzes
                .Where(q => q.DueAt.HasValue && q.DueAt > DateTime.UtcNow && q.IsPublished)
                .ToList();

            // Get all student attempts for these quizzes in one query
            var quizIds = upcomingQuizCandidates.Select(q => q.QuizId).ToList();
            var studentAttempts = quizIds.Any()
                ? await _attemptRepository.GetByQuizIdsAndStudentIdAsync(quizIds, studentId)
                : new List<Attempt>();

            var attemptedQuizIds = new HashSet<int>(studentAttempts.Select(a => a.QuizId));

            // Build upcoming quizzes list
            var upcomingQuizzes = upcomingQuizCandidates
                .Where(q => !attemptedQuizIds.Contains(q.QuizId))
                .Select(q => new UpcomingQuizDto
                {
                    QuizId = q.QuizId,
                    Title = q.Title,
                    CourseName = courseLookup.GetValueOrDefault(q.CourseId)?.Name ?? "Unknown",
                    DueDate = q.DueAt
                })
                .OrderBy(q => q.DueDate)
                .Take(5)
                .ToList();

            // Fetch quiz details for recent attempts
            var recentQuizIds = recentAttempts.Select(a => a.QuizId).Distinct().ToList();
            var recentQuizzes = recentQuizIds.Any() && !quizLookup.Any()
                ? await _quizRepository.GetByIdsAsync(recentQuizIds)
                : recentQuizIds.Select(id => quizLookup.GetValueOrDefault(id)).Where(q => q != null).Cast<Quiz>().ToList();

            var recentQuizLookup = recentQuizzes.ToDictionary(q => q.QuizId);

            return new StudentDashboardDto
            {
                EnrolledCourses = enrolledCourses.Count,
                CompletedQuizzes = allAttempts.Count,
                AverageScore = (double)avgScore,
                UpcomingQuizzes = upcomingQuizzes,
                RecentResults = recentAttempts.Select(a =>
                {
                    var quiz = recentQuizLookup.GetValueOrDefault(a.QuizId);
                    var course = quiz != null ? courseLookup.GetValueOrDefault(quiz.CourseId) : null;
                    return new RecentResultDto
                    {
                        QuizId = a.QuizId,
                        QuizTitle = quiz?.Title ?? "Quiz " + a.QuizId,
                        CourseName = course?.Name ?? "Unknown",
                        Score = a.Score,
                        SubmittedAt = a.SubmittedAt ?? a.StartedAt
                    };
                }).ToList()
            };
        }

        public async Task<CourseAnalyticsDto> GetCourseAnalyticsAsync(int courseId)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null) throw new KeyNotFoundException("Course not found");

            var quizzes = await _quizRepository.GetByCourseIdAsync(courseId);
            var avgScore = await _attemptRepository.GetAverageScoreByCourseAsync(courseId);
            
            return new CourseAnalyticsDto
            {
                CourseId = course.CourseId,
                CourseName = course.Name,
                TotalStudents = 0, // Placeholder
                TotalQuizzes = quizzes.Count,
                AverageScore = avgScore,
                StudentProgress = new List<StudentProgressDto>() // Placeholder
            };
        }
    }
}
