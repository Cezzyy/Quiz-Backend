using Mapster;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.IServices;
using OnlineQuiz.Models;

namespace OnlineQuiz.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRoleRepository _userRoleRepository;

        public NotificationService(
            INotificationRepository notificationRepository,
            IUserRoleRepository userRoleRepository)
        {
            _notificationRepository = notificationRepository;
            _userRoleRepository = userRoleRepository;
        }

        public async Task<NotificationResponseDto> CreateNotificationAsync(CreateNotificationDto createNotificationDto, int createdBy)
        {
            // Check if creator is admin
            var isAdmin = await _userRoleRepository.IsAdminAsync(createdBy);
            if (!isAdmin)
            {
                throw new UnauthorizedAccessException("Only admins can create notifications");
            }

            var notification = createNotificationDto.Adapt<Notification>();
            notification.CreatedAt = DateTime.UtcNow;

            var createdNotification = await _notificationRepository.CreateAsync(notification);
            return createdNotification.Adapt<NotificationResponseDto>();
        }

        public async Task<NotificationResponseDto?> GetNotificationByIdAsync(int notificationId, int userId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null)
            {
                return null;
            }

            // Users can only view their own notifications unless they're admin
            var isAdmin = await _userRoleRepository.IsAdminAsync(userId);
            if (notification.UserId != userId && !isAdmin)
            {
                throw new UnauthorizedAccessException("You can only view your own notifications");
            }

            return notification.Adapt<NotificationResponseDto>();
        }

        public async Task<List<NotificationResponseDto>> GetNotificationsForUserAsync(int userId)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId);
            return notifications.Adapt<List<NotificationResponseDto>>();
        }

        public async Task<NotificationResponseDto> MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null)
            {
                throw new ArgumentException($"Notification with ID {notificationId} not found");
            }

            if (notification.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only mark your own notifications as read");
            }

            notification.IsRead = true;
            var updatedNotification = await _notificationRepository.UpdateAsync(notification);
            return updatedNotification.Adapt<NotificationResponseDto>();
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId, int userId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null)
            {
                return false;
            }

            // Users can only delete their own notifications unless they're admin
            var isAdmin = await _userRoleRepository.IsAdminAsync(userId);
            if (notification.UserId != userId && !isAdmin)
            {
                throw new UnauthorizedAccessException("You can only delete your own notifications");
            }

            return await _notificationRepository.DeleteAsync(notificationId);
        }
    }
}
