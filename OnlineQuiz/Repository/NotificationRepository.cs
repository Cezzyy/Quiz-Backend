using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;

namespace OnlineQuiz.Repository
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly SupabaseService _supabaseService;

        public NotificationRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<Notification> CreateAsync(Notification notification)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Notification>().Insert(notification);
            return result.Models.First();
        }

        public async Task<Notification?> GetByIdAsync(int notificationId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Notification>()
                .Where(n => n.NotificationId == notificationId)
                .Get();
            return result.Models.FirstOrDefault();
        }

        public async Task<List<Notification>> GetByUserIdAsync(int userId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Notification>()
                .Where(n => n.UserId == userId)
                .Order("CreatedAt", Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models;
        }

        public async Task<Notification> UpdateAsync(Notification notification)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Notification>().Update(notification);
            return result.Models.First();
        }

        public async Task<bool> DeleteAsync(int notificationId)
        {
            var client = _supabaseService.GetClient();
            await client.From<Notification>()
                .Where(n => n.NotificationId == notificationId)
                .Delete();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            var client = _supabaseService.GetClient();
            await client.From<Notification>()
                .Where(n => n.UserId == userId && n.IsRead == false)
                .Set(n => n.IsRead, true)
                .Update();
            return true;
        }

        public async Task<List<Notification>> CreateBatchAsync(List<Notification> notifications)
        {
            if (notifications == null || !notifications.Any())
                return new List<Notification>();

            var client = _supabaseService.GetClient();
            var result = await client.From<Notification>().Insert(notifications);
            return result.Models;
        }

        public async Task<int> BulkDeleteAsync(List<int> notificationIds)
        {
            if (notificationIds == null || !notificationIds.Any())
                return 0;

            var client = _supabaseService.GetClient();
            await client.From<Notification>()
                .Filter("NotificationId", Postgrest.Constants.Operator.In, notificationIds)
                .Delete();
            
            return notificationIds.Count;
        }
    }
}
