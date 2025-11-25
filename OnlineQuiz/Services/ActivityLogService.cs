using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.IServices;
using OnlineQuiz.Models;
using OnlineQuiz.Utilities;

namespace OnlineQuiz.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IActivityLogRepository _activityLogRepository;
        private readonly IUserRepository _userRepository;

        public ActivityLogService(
            IActivityLogRepository activityLogRepository,
            IUserRepository userRepository)
        {
            _activityLogRepository = activityLogRepository;
            _userRepository = userRepository;
        }

        public async Task<ActivityLogDto> LogActivityAsync(CreateActivityLogDto dto)
        {
            // Validate action
            if (!ActivityLogHelper.IsValidAction(dto.Action))
            {
                throw new ArgumentException($"Invalid action: {dto.Action}");
            }

            // Validate entity
            if (!ActivityLogHelper.IsValidEntity(dto.Entity))
            {
                throw new ArgumentException($"Invalid entity: {dto.Entity}");
            }

            // Create activity log
            var activityLog = new ActivityLog
            {
                UserId = dto.UserId,
                Action = dto.Action,
                Entity = dto.Entity,
                EntityId = dto.EntityId,
                Description = dto.Description,
                OldValues = ActivityLogHelper.SerializeToJson(dto.OldValues),
                NewValues = ActivityLogHelper.SerializeToJson(dto.NewValues),
                IpAddress = dto.IpAddress,
                UserAgent = dto.UserAgent,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _activityLogRepository.CreateAsync(activityLog);

            // Get user info for response
            var user = await _userRepository.GetByIdAsync(dto.UserId);

            return new ActivityLogDto
            {
                ActivityLogId = created.ActivityLogId,
                UserId = created.UserId,
                UserFullName = user?.FullName ?? "Unknown",
                UserEmail = user?.Email ?? "Unknown",
                Action = created.Action,
                Entity = created.Entity,
                EntityId = created.EntityId,
                Description = created.Description,
                OldValues = created.OldValues,
                NewValues = created.NewValues,
                IpAddress = created.IpAddress,
                UserAgent = created.UserAgent,
                CreatedAt = created.CreatedAt
            };
        }

        public async Task<ActivityLogDto?> GetActivityLogByIdAsync(long activityLogId)
        {
            var log = await _activityLogRepository.GetByIdAsync(activityLogId);
            if (log == null)
                return null;

            var user = await _userRepository.GetByIdAsync(log.UserId);

            return new ActivityLogDto
            {
                ActivityLogId = log.ActivityLogId,
                UserId = log.UserId,
                UserFullName = user?.FullName ?? "Unknown",
                UserEmail = user?.Email ?? "Unknown",
                Action = log.Action,
                Entity = log.Entity,
                EntityId = log.EntityId,
                Description = log.Description,
                OldValues = log.OldValues,
                NewValues = log.NewValues,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                CreatedAt = log.CreatedAt
            };
        }

        public async Task<List<ActivityLogDto>> GetActivityLogsAsync(ActivityLogFilterDto filter)
        {
            var logs = await _activityLogRepository.GetFilteredAsync(filter);

            if (!logs.Any())
                return new List<ActivityLogDto>();

            // Get all unique user IDs
            var userIds = logs.Select(l => l.UserId).Distinct().ToList();

            // Fetch users in batch
            var users = await _userRepository.GetByIdsAsync(userIds);
            var userMap = users.ToDictionary(u => u.UserId);

            return logs.Select(log => new ActivityLogDto
            {
                ActivityLogId = log.ActivityLogId,
                UserId = log.UserId,
                UserFullName = userMap.TryGetValue(log.UserId, out var user) ? user.FullName : "Unknown",
                UserEmail = userMap.TryGetValue(log.UserId, out var userEmail) ? userEmail.Email : "Unknown",
                Action = log.Action,
                Entity = log.Entity,
                EntityId = log.EntityId,
                Description = log.Description,
                OldValues = log.OldValues,
                NewValues = log.NewValues,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                CreatedAt = log.CreatedAt
            }).ToList();
        }

        public async Task<List<ActivityLogDto>> GetUserActivityLogsAsync(int userId, int? days = null)
        {
            var filter = new ActivityLogFilterDto
            {
                UserId = userId,
                PageSize = 100 // Get more records for user-specific queries
            };

            if (days.HasValue)
            {
                filter.StartDate = DateTime.UtcNow.AddDays(-days.Value);
            }

            return await GetActivityLogsAsync(filter);
        }

        public async Task<ActivityLogStatisticsDto> GetActivityStatisticsAsync(int? userId = null, int? days = 30)
        {
            var filter = new ActivityLogFilterDto
            {
                UserId = userId,
                StartDate = days.HasValue ? DateTime.UtcNow.AddDays(-days.Value) : null,
                PageSize = 1000 // Get more data for statistics
            };

            var logs = await _activityLogRepository.GetFilteredAsync(filter);

            var statistics = new ActivityLogStatisticsDto
            {
                TotalActions = logs.Count,
                UniqueUsers = logs.Select(l => l.UserId).Distinct().Count(),
                ActionsByType = logs.GroupBy(l => l.Action)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EntitiesAffected = logs.GroupBy(l => l.Entity)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            // Get recent activity (top 10)
            var recentLogs = logs.OrderByDescending(l => l.CreatedAt).Take(10).ToList();

            if (recentLogs.Any())
            {
                var userIds = recentLogs.Select(l => l.UserId).Distinct().ToList();
                var users = await _userRepository.GetByIdsAsync(userIds);
                var userMap = users.ToDictionary(u => u.UserId);

                statistics.RecentActivity = recentLogs.Select(log => new ActivityLogDto
                {
                    ActivityLogId = log.ActivityLogId,
                    UserId = log.UserId,
                    UserFullName = userMap.TryGetValue(log.UserId, out var user) ? user.FullName : "Unknown",
                    UserEmail = userMap.TryGetValue(log.UserId, out var userEmail) ? userEmail.Email : "Unknown",
                    Action = log.Action,
                    Entity = log.Entity,
                    EntityId = log.EntityId,
                    Description = log.Description,
                    OldValues = log.OldValues,
                    NewValues = log.NewValues,
                    IpAddress = log.IpAddress,
                    UserAgent = log.UserAgent,
                    CreatedAt = log.CreatedAt
                }).ToList();
            }

            return statistics;
        }
    }
}
