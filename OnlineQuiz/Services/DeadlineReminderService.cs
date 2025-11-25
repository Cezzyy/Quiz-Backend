using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Utilities;

namespace OnlineQuiz.Services
{
    public class DeadlineReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DeadlineReminderService> _logger;

        public DeadlineReminderService(IServiceProvider serviceProvider, ILogger<DeadlineReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Deadline Reminder Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckDeadlinesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking deadlines.");
                }

                // Run every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task CheckDeadlinesAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var quizRepository = scope.ServiceProvider.GetRequiredService<IQuizRepository>();
                var enrollmentRepository = scope.ServiceProvider.GetRequiredService<IEnrollmentRepository>();
                var attemptRepository = scope.ServiceProvider.GetRequiredService<IAttemptRepository>();
                var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

                // Get quizzes due in the next 24 hours using database-level filtering
                var upcomingQuizzes = await quizRepository.GetUpcomingDeadlinesAsync(DateTime.UtcNow.AddHours(24));
                
                if (!upcomingQuizzes.Any())
                {
                    _logger.LogInformation("No upcoming quiz deadlines in the next 24 hours.");
                    return;
                }

                _logger.LogInformation($"Found {upcomingQuizzes.Count} quizzes with upcoming deadlines.");

                // Get all course IDs and fetch enrollments in bulk
                var courseIds = upcomingQuizzes.Select(q => q.CourseId).Distinct().ToList();
                var allEnrollmentsTask = Task.WhenAll(
                    courseIds.Select(cid => enrollmentRepository.GetByCourseIdAsync(cid))
                );
                var enrollmentsByCourse = (await allEnrollmentsTask)
                    .SelectMany(e => e)
                    .GroupBy(e => e.CourseId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Get all quiz IDs and fetch attempts in bulk
                var quizIds = upcomingQuizzes.Select(q => q.QuizId).ToList();
                var allAttemptsTask = Task.WhenAll(
                    quizIds.Select(qid => attemptRepository.GetByQuizIdAsync(qid))
                );
                var attemptsByQuiz = (await allAttemptsTask)
                    .SelectMany(a => a)
                    .GroupBy(a => a.QuizId)
                    .ToDictionary(g => g.Key, g => new HashSet<int>(g.Select(a => a.UserId)));

                var notificationsToCreate = new List<Notification>();
                var now = DateTime.UtcNow;

                foreach (var quiz in upcomingQuizzes)
                {
                    if (!quiz.DueAt.HasValue) continue;

                    var timeRemaining = quiz.DueAt.Value - now;
                    
                    // Send reminders at two points: 24h before and 2h before
                    // Since service runs hourly, check if deadline falls in specific windows
                    bool shouldSendReminder = 
                        (timeRemaining.TotalHours > 23 && timeRemaining.TotalHours <= 24) ||  // 24h reminder
                        (timeRemaining.TotalHours > 1 && timeRemaining.TotalHours <= 2);      // 2h reminder

                    if (!shouldSendReminder) continue;

                    // Get enrollments for this quiz's course
                    if (!enrollmentsByCourse.TryGetValue(quiz.CourseId, out var enrollments))
                        continue;

                    // Get attempted user IDs for this quiz
                    attemptsByQuiz.TryGetValue(quiz.QuizId, out var attemptedUserIds);

                    foreach (var enrollment in enrollments)
                    {
                        // Skip if student already attempted this quiz
                        if (attemptedUserIds != null && attemptedUserIds.Contains(enrollment.UserId))
                            continue;

                        var reminderType = timeRemaining.TotalHours > 23 ? "24-hour" : "2-hour";
                        var hoursRemaining = (int)Math.Floor(timeRemaining.TotalHours);
                        var minutesRemaining = (int)Math.Floor(timeRemaining.TotalMinutes % 60);

                        notificationsToCreate.Add(new Notification
                        {
                            UserId = enrollment.UserId,
                            Type = NotificationConstants.TypeReminder,
                            Title = $"Quiz Deadline: {quiz.Title}",
                            Message = $"Reminder: '{quiz.Title}' is due in {hoursRemaining}h {minutesRemaining}m. Don't forget to complete it!",
                            IsRead = false,
                            CreatedAt = now
                        });
                    }
                }

                // Batch create all notifications
                if (notificationsToCreate.Any())
                {
                    foreach (var notification in notificationsToCreate)
                    {
                        await notificationRepository.CreateAsync(notification);
                    }
                    _logger.LogInformation($"Sent {notificationsToCreate.Count} deadline reminder notifications.");
                }
                else
                {
                    _logger.LogInformation("No reminders needed at this time.");
                }
            }
        }
    }
}
