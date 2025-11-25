using OnlineQuiz.DTOs;
using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface IActivityLogRepository
    {
        Task<ActivityLog> CreateAsync(ActivityLog log);
        Task<ActivityLog?> GetByIdAsync(long activityLogId);
        Task<List<ActivityLog>> GetAllAsync();
        Task<List<ActivityLog>> GetByUserIdAsync(int userId);
        Task<List<ActivityLog>> GetByActionAsync(string action);
        Task<List<ActivityLog>> GetByEntityAsync(string entity, long? entityId = null);
        Task<List<ActivityLog>> GetFilteredAsync(ActivityLogFilterDto filter);
        Task<List<ActivityLog>> GetRecentAsync(int limit);
    }
}
