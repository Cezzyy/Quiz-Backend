using OnlineQuiz.DTOs;

namespace OnlineQuiz.IServices
{
    public interface INotificationService
    {
        Task<NotificationResponseDto> CreateNotificationAsync(CreateNotificationDto createNotificationDto, int createdBy);
        Task<NotificationResponseDto?> GetNotificationByIdAsync(int notificationId, int userId);
        Task<List<NotificationResponseDto>> GetNotificationsForUserAsync(int userId);
        Task<NotificationResponseDto> MarkAsReadAsync(int notificationId, int userId);
        Task<bool> DeleteNotificationAsync(int notificationId, int userId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task NotifyStudentsOfNewQuizAsync(int quizId, int courseId, string quizTitle);
        Task<int> BulkDeleteNotificationsAsync(List<int> notificationIds, int userId);
    }
}
