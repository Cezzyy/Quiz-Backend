using OnlineQuiz.DTOs;

namespace OnlineQuiz.IServices
{
    public interface IActivityLogService
    {
        Task<ActivityLogDto> LogActivityAsync(CreateActivityLogDto dto);
        Task<List<ActivityLogDto>> GetActivityLogsAsync(ActivityLogFilterDto filter);
        Task<List<ActivityLogDto>> GetUserActivityLogsAsync(int userId, int? days = null);
        Task<ActivityLogStatisticsDto> GetActivityStatisticsAsync(int? userId = null, int? days = 30);
        Task<ActivityLogDto?> GetActivityLogByIdAsync(long activityLogId);
    }
}
