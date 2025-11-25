using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface INotificationRepository
    {
        Task<Notification> CreateAsync(Notification notification);
        Task<Notification?> GetByIdAsync(int notificationId);
        Task<List<Notification>> GetByUserIdAsync(int userId);
        Task<Notification> UpdateAsync(Notification notification);
        Task<bool> DeleteAsync(int notificationId);
    }
}
