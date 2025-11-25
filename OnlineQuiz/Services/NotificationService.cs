using Mapster;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.IServices;
using OnlineQuiz.Models;
using OnlineQuiz.Utilities;

namespace OnlineQuiz.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;

        public NotificationService(
            INotificationRepository notificationRepository,
            IUserRoleRepository userRoleRepository,
            IEnrollmentRepository enrollmentRepository)
        {
            _notificationRepository = notificationRepository;
            _userRoleRepository = userRoleRepository;
            _enrollmentRepository = enrollmentRepository;
        }

        public async Task<NotificationResponseDto> CreateNotificationAsync(CreateNotificationDto createNotificationDto, int createdBy)
        {
            // Note: Access control (e.g., only Admins can create manual notifications) 
            // should be enforced at the Controller level. 
            // This service method can be called by other services for system triggers.

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

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            return await _notificationRepository.MarkAllAsReadAsync(userId);
        }

        public async Task NotifyStudentsOfNewQuizAsync(int quizId, int courseId, string quizTitle)
        {
            var enrollments = await _enrollmentRepository.GetByCourseIdAsync(courseId);
            if (!enrollments.Any()) return;

            var notifications = enrollments.Select(e => new Notification
            {
                UserId = e.UserId,
                Type = NotificationConstants.TypeQuiz,
                Title = "New Quiz Available",
                Message = $"A new quiz '{quizTitle}' has been published in your course.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _notificationRepository.CreateBatchAsync(notifications);
        }

        public async Task<int> BulkDeleteNotificationsAsync(List<int> notificationIds, int userId)
        {
            if (!notificationIds.Any()) return 0;

            // Verify ownership of all notifications
            // Note: This might be slow for many notifications. 
            // A more optimized way would be to delete where NotificationId IN ids AND UserId = userId
            // But Supabase client might not support complex delete queries easily without raw SQL.
            // For now, we'll fetch and verify or rely on repository to filter by userId if we add that method.
            
            // Better approach: Let's fetch the notifications first to verify ownership
            // Or, we can just delete with a filter on UserId if we modify the repo method.
            // Since we added BulkDeleteAsync(ids) to repo, let's stick to that but we should verify ownership.
            
            // Fetch notifications to verify ownership
            // This is not efficient for large batches but ensures security.
            // Alternatively, we could trust the client but that's bad.
            
            // Let's assume we can trust the repository to handle it if we pass userId, 
            // but the interface I added only takes IDs.
            // I will implement a check here.
            
            var notifications = new List<Notification>();
            foreach(var id in notificationIds)
            {
                var n = await _notificationRepository.GetByIdAsync(id);
                if (n != null && n.UserId == userId)
                {
                    notifications.Add(n);
                }
            }
            
            if (!notifications.Any()) return 0;
            
            var idsToDelete = notifications.Select(n => n.NotificationId).ToList();
            return await _notificationRepository.BulkDeleteAsync(idsToDelete);
        }
    }
}
