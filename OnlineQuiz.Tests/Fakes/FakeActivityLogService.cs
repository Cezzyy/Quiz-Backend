using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;

namespace OnlineQuiz.Tests.Fakes
{
    /// <summary>
    /// Shared fake IActivityLogService used across all controller and service tests.
    /// Replaces 11 near-identical per-file copies.
    /// Logged activities are captured in <see cref="Logged"/> for assertion.
    /// </summary>
    public class FakeActivityLogService : IActivityLogService
    {
        /// <summary>All DTOs passed to LogActivityAsync, in order.</summary>
        public List<CreateActivityLogDto> Logged { get; } = new();

        public Task<ActivityLogDto> LogActivityAsync(CreateActivityLogDto dto)
        {
            Logged.Add(dto);
            return Task.FromResult(new ActivityLogDto
            {
                ActivityLogId = DateTime.UtcNow.Ticks,
                UserId = dto.UserId,
                Action = dto.Action,
                Entity = dto.Entity,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            });
        }

        public Task<List<ActivityLogDto>> GetActivityLogsAsync(ActivityLogFilterDto filter)
            => Task.FromResult(new List<ActivityLogDto>());

        public Task<List<ActivityLogDto>> GetUserActivityLogsAsync(int userId, int? days = null)
            => Task.FromResult(new List<ActivityLogDto>());

        public Task<ActivityLogStatisticsDto> GetActivityStatisticsAsync(int? userId = null, int? days = 30)
            => Task.FromResult(new ActivityLogStatisticsDto());

        public Task<ActivityLogDto?> GetActivityLogByIdAsync(long activityLogId)
            => Task.FromResult<ActivityLogDto?>(null);
    }
}
