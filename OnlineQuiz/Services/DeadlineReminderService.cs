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

                // Find quizzes due in the next 24 hours
                // Note: This requires a repository method to filter by date. 
                // Since I can't easily add a new method to IQuizRepository without modifying it, 
                // I'll fetch all quizzes (or use an existing filter if available) and filter in memory for now.
                // Optimally, we should add GetUpcomingQuizzesAsync(DateTime from, DateTime to).
                // I'll assume GetByCourseIdAsync is not efficient for this.
                // Let's see what IQuizRepository has.
                // I'll use a raw query or fetch all for now if the dataset is small, but that's bad practice.
                // I will add a method to IQuizRepository to get upcoming quizzes.
                // Wait, I can't easily modify the interface and implementation in one go without context.
                // I'll check IQuizRepository content first.
                
                // For this implementation, I will assume I can fetch all quizzes or filter them.
                // Actually, I'll just iterate through all courses -> quizzes? No, too slow.
                // I will add a method to IQuizRepository in the next step.
                // For now, I'll write the logic assuming the method exists: GetUpcomingDeadlinesAsync(DateTime threshold)
                
                // Since I can't assume it exists, I will implement the service to just log for now 
                // and then I will go and add the method to the repository.
                
                // Actually, I will write the service to use a hypothetical method and then implement it.
                var upcomingQuizzes = await quizRepository.GetUpcomingDeadlinesAsync(DateTime.UtcNow.AddHours(24));

                foreach (var quiz in upcomingQuizzes)
                {
                    if (!quiz.DueAt.HasValue) continue;

                    var enrollments = await enrollmentRepository.GetByCourseIdAsync(quiz.CourseId);
                    
                    foreach (var enrollment in enrollments)
                    {
                        // Check if student has attempted
                        var attempts = await attemptRepository.GetByQuizIdAndUserIdAsync(quiz.QuizId, enrollment.UserId);
                        if (!attempts.Any())
                        {
                            // Check if we already sent a reminder? 
                            // This is tricky without a "ReminderSent" flag or checking existing notifications.
                            // To avoid spam, we could check if a notification of type 'Reminder' exists for this quiz today.
                            // For simplicity in this MVP, we might spam every hour if we don't check.
                            // I'll check for existing notifications.
                            
                            // Optimization: This is N+1. 
                            // Better: Get all notifications for user and filter.
                            // Or just rely on the fact that it runs every hour and maybe only send if due in < 1 hour?
                            // Requirement: "near deadline".
                            // Let's send ONE reminder if it's between 23 and 24 hours away, OR between 1 and 2 hours away.
                            
                            var timeRemaining = quiz.DueAt.Value - DateTime.UtcNow;
                            if ((timeRemaining.TotalHours >= 23 && timeRemaining.TotalHours < 24) ||
                                (timeRemaining.TotalHours >= 1 && timeRemaining.TotalHours < 2))
                            {
                                var notification = new Notification
                                {
                                    UserId = enrollment.UserId,
                                    Type = NotificationConstants.TypeReminder,
                                    Title = "Quiz Deadline Approaching",
                                    Message = $"Quiz '{quiz.Title}' is due in {timeRemaining.Hours} hours and {timeRemaining.Minutes} minutes.",
                                    IsRead = false,
                                    CreatedAt = DateTime.UtcNow
                                };
                                await notificationRepository.CreateAsync(notification);
                            }
                        }
                    }
                }
            }
        }
    }
}
