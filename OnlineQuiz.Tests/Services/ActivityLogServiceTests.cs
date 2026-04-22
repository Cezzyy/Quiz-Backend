using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;
using OnlineQuiz.Utilities;
using Xunit;

namespace OnlineQuiz.Tests.Services
{
    [Collection("MapsterWarmup")]
    public class ActivityLogServiceTests
    {
        // ── In-memory fakes ───────────────────────────────────────────────

        private class InMemoryActivityLogRepository : IActivityLogRepository
        {
            private readonly Dictionary<long, ActivityLog> _store = new();
            private long _nextId = 1;

            public Task<ActivityLog> CreateAsync(ActivityLog log)
            {
                log.ActivityLogId = _nextId++;
                _store[log.ActivityLogId] = Clone(log);
                return Task.FromResult(Clone(log));
            }

            public Task<ActivityLog?> GetByIdAsync(long id)
            {
                _store.TryGetValue(id, out var l);
                return Task.FromResult(l != null ? Clone(l) : null);
            }

            public Task<List<ActivityLog>> GetAllAsync()
                => Task.FromResult(_store.Values.Select(Clone).ToList());

            public Task<List<ActivityLog>> GetByUserIdAsync(int userId)
                => Task.FromResult(_store.Values.Where(l => l.UserId == userId).Select(Clone).ToList());

            public Task<List<ActivityLog>> GetByActionAsync(string action)
                => Task.FromResult(_store.Values.Where(l => l.Action == action).Select(Clone).ToList());

            public Task<List<ActivityLog>> GetByEntityAsync(string entity, long? entityId = null)
                => Task.FromResult(_store.Values
                    .Where(l => l.Entity == entity && (entityId == null || l.EntityId == entityId))
                    .Select(Clone).ToList());

            public Task<List<ActivityLog>> GetFilteredAsync(ActivityLogFilterDto filter)
            {
                var query = _store.Values.AsEnumerable();
                if (filter.UserId.HasValue) query = query.Where(l => l.UserId == filter.UserId.Value);
                if (!string.IsNullOrEmpty(filter.Action)) query = query.Where(l => l.Action == filter.Action);
                if (!string.IsNullOrEmpty(filter.Entity)) query = query.Where(l => l.Entity == filter.Entity);
                if (filter.StartDate.HasValue) query = query.Where(l => l.CreatedAt >= filter.StartDate.Value);
                if (filter.EndDate.HasValue) query = query.Where(l => l.CreatedAt <= filter.EndDate.Value);
                return Task.FromResult(query.Select(Clone).ToList());
            }

            public Task<List<ActivityLog>> GetRecentAsync(int limit)
                => Task.FromResult(_store.Values.OrderByDescending(l => l.CreatedAt).Take(limit).Select(Clone).ToList());

            private static ActivityLog Clone(ActivityLog l) => new ActivityLog
            {
                ActivityLogId = l.ActivityLogId,
                UserId = l.UserId,
                Action = l.Action,
                Entity = l.Entity,
                EntityId = l.EntityId,
                Description = l.Description,
                OldValues = l.OldValues,
                NewValues = l.NewValues,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                CreatedAt = l.CreatedAt
            };
        }

        private class InMemoryUserRepository : IUserRepository
        {
            private readonly Dictionary<int, User> _store = new();
            public void Seed(User u) => _store[u.UserId] = u;
            public Task<User?> GetByIdAsync(int userId) { _store.TryGetValue(userId, out var u); return Task.FromResult(u); }
            public Task<User?> GetByEmailAsync(string email) => Task.FromResult<User?>(null);
            public Task<List<User>> GetAllAsync() => Task.FromResult(_store.Values.ToList());
            public Task<List<User>> GetByIdsAsync(List<int> ids) => Task.FromResult(_store.Values.Where(u => ids.Contains(u.UserId)).ToList());
            public Task<User> CreateAsync(User user) { _store[user.UserId] = user; return Task.FromResult(user); }
            public Task<User> UpdateAsync(User user) { _store[user.UserId] = user; return Task.FromResult(user); }
            public Task<bool> DeleteAsync(int userId) => Task.FromResult(_store.Remove(userId));
            public Task<int> BulkDeleteAsync(List<int> ids) => Task.FromResult(0);
            public Task<int> CountAsync() => Task.FromResult(_store.Count);
            public Task<int> CountByRoleAsync(int roleId) => Task.FromResult(0);
            public Task<List<User>> GetRecentRegistrationsAsync(int days) => Task.FromResult(new List<User>());
            public Task<User?> ArchiveAsync(int userId, int archivedBy) => Task.FromResult<User?>(null);
            public Task<User?> UnarchiveAsync(int userId) => Task.FromResult<User?>(null);
            public Task<int> BulkArchiveAsync(List<int> userIds, int archivedBy) => Task.FromResult(0);
            public Task<int> BulkUnarchiveAsync(List<int> userIds) => Task.FromResult(0);
            public Task<List<User>> GetArchivedAsync() => Task.FromResult(new List<User>());
            public Task<List<User>> GetAllIncludingArchivedAsync() => Task.FromResult(_store.Values.ToList());
            public Task<int> CountArchivedAsync() => Task.FromResult(0);
        }

        private static ActivityLogService CreateService(
            InMemoryActivityLogRepository? repo = null,
            InMemoryUserRepository? users = null)
            => new ActivityLogService(repo ?? new InMemoryActivityLogRepository(), users ?? new InMemoryUserRepository());

        // ── LogActivityAsync ──────────────────────────────────────────────

        [Fact]
        public async Task LogActivity_CreatesLog_WithValidActionAndEntity()
        {
            var repo = new InMemoryActivityLogRepository();
            var users = new InMemoryUserRepository();
            users.Seed(new User { UserId = 1, FullName = "Alice", Email = "alice@test.com", PasswordHash = "x", Status = "Active" });
            var svc = CreateService(repo, users);

            var dto = new CreateActivityLogDto
            {
                UserId = 1,
                Action = ActivityLogConstants.Actions.CREATE,
                Entity = ActivityLogConstants.Entities.User,
                Description = "Created user"
            };

            var result = await svc.LogActivityAsync(dto);

            Assert.NotEqual(0, result.ActivityLogId);
            Assert.Equal(1, result.UserId);
            Assert.Equal(ActivityLogConstants.Actions.CREATE, result.Action);
            Assert.Equal(ActivityLogConstants.Entities.User, result.Entity);
            Assert.Equal("Alice", result.UserFullName);
            Assert.Equal("alice@test.com", result.UserEmail);
        }

        [Fact]
        public async Task LogActivity_Throws_WhenActionIsInvalid()
        {
            var svc = CreateService();
            var dto = new CreateActivityLogDto
            {
                UserId = 1,
                Action = "INVALID_ACTION",
                Entity = ActivityLogConstants.Entities.User
            };
            await Assert.ThrowsAsync<ArgumentException>(() => svc.LogActivityAsync(dto));
        }

        [Fact]
        public async Task LogActivity_Throws_WhenEntityIsInvalid()
        {
            var svc = CreateService();
            var dto = new CreateActivityLogDto
            {
                UserId = 1,
                Action = ActivityLogConstants.Actions.CREATE,
                Entity = "InvalidEntity"
            };
            await Assert.ThrowsAsync<ArgumentException>(() => svc.LogActivityAsync(dto));
        }

        [Fact]
        public async Task LogActivity_SetsUnknown_WhenUserNotFound()
        {
            var svc = CreateService(); // no users seeded
            var dto = new CreateActivityLogDto
            {
                UserId = 999,
                Action = ActivityLogConstants.Actions.LOGIN,
                Entity = ActivityLogConstants.Entities.Auth
            };

            var result = await svc.LogActivityAsync(dto);

            Assert.Equal("Unknown", result.UserFullName);
            Assert.Equal("Unknown", result.UserEmail);
        }

        [Fact]
        public async Task LogActivity_SerializesOldAndNewValues()
        {
            var repo = new InMemoryActivityLogRepository();
            var svc = CreateService(repo);
            var dto = new CreateActivityLogDto
            {
                UserId = 1,
                Action = ActivityLogConstants.Actions.UPDATE,
                Entity = ActivityLogConstants.Entities.User,
                OldValues = new { Status = "Active" },
                NewValues = new { Status = "Archived" }
            };

            var result = await svc.LogActivityAsync(dto);

            Assert.Contains("Active", result.OldValues);
            Assert.Contains("Archived", result.NewValues);
        }

        // ── GetActivityLogByIdAsync ───────────────────────────────────────

        [Fact]
        public async Task GetActivityLogById_ReturnsNull_WhenNotFound()
        {
            var svc = CreateService();
            var result = await svc.GetActivityLogByIdAsync(999);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetActivityLogById_ReturnsLog_WhenFound()
        {
            var repo = new InMemoryActivityLogRepository();
            var users = new InMemoryUserRepository();
            users.Seed(new User { UserId = 5, FullName = "Bob", Email = "bob@test.com", PasswordHash = "x", Status = "Active" });
            var svc = CreateService(repo, users);

            var created = await svc.LogActivityAsync(new CreateActivityLogDto
            {
                UserId = 5,
                Action = ActivityLogConstants.Actions.DELETE,
                Entity = ActivityLogConstants.Entities.Course
            });

            var found = await svc.GetActivityLogByIdAsync(created.ActivityLogId);

            Assert.NotNull(found);
            Assert.Equal(5, found!.UserId);
            Assert.Equal("Bob", found.UserFullName);
        }

        // ── GetActivityLogsAsync ──────────────────────────────────────────

        [Fact]
        public async Task GetActivityLogs_ReturnsEmpty_WhenNoLogs()
        {
            var svc = CreateService();
            var result = await svc.GetActivityLogsAsync(new ActivityLogFilterDto());
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetActivityLogs_FiltersBy_UserId()
        {
            var repo = new InMemoryActivityLogRepository();
            var svc = CreateService(repo);

            await svc.LogActivityAsync(new CreateActivityLogDto { UserId = 1, Action = ActivityLogConstants.Actions.LOGIN, Entity = ActivityLogConstants.Entities.Auth });
            await svc.LogActivityAsync(new CreateActivityLogDto { UserId = 2, Action = ActivityLogConstants.Actions.LOGIN, Entity = ActivityLogConstants.Entities.Auth });
            await svc.LogActivityAsync(new CreateActivityLogDto { UserId = 1, Action = ActivityLogConstants.Actions.LOGOUT, Entity = ActivityLogConstants.Entities.Auth });

            var result = await svc.GetActivityLogsAsync(new ActivityLogFilterDto { UserId = 1 });

            Assert.Equal(2, result.Count);
            Assert.All(result, l => Assert.Equal(1, l.UserId));
        }

        // ── GetUserActivityLogsAsync ──────────────────────────────────────

        [Fact]
        public async Task GetUserActivityLogs_ReturnsOnlyThatUsersLogs()
        {
            var repo = new InMemoryActivityLogRepository();
            var svc = CreateService(repo);

            await svc.LogActivityAsync(new CreateActivityLogDto { UserId = 10, Action = ActivityLogConstants.Actions.CREATE, Entity = ActivityLogConstants.Entities.Quiz });
            await svc.LogActivityAsync(new CreateActivityLogDto { UserId = 20, Action = ActivityLogConstants.Actions.CREATE, Entity = ActivityLogConstants.Entities.Quiz });

            var result = await svc.GetUserActivityLogsAsync(userId: 10);

            Assert.Single(result);
            Assert.Equal(10, result[0].UserId);
        }

        // ── GetActivityStatisticsAsync ────────────────────────────────────

        [Fact]
        public async Task GetActivityStatistics_CountsActionsAndEntities()
        {
            var repo = new InMemoryActivityLogRepository();
            var svc = CreateService(repo);

            await svc.LogActivityAsync(new CreateActivityLogDto { UserId = 1, Action = ActivityLogConstants.Actions.CREATE, Entity = ActivityLogConstants.Entities.Quiz });
            await svc.LogActivityAsync(new CreateActivityLogDto { UserId = 1, Action = ActivityLogConstants.Actions.CREATE, Entity = ActivityLogConstants.Entities.Course });
            await svc.LogActivityAsync(new CreateActivityLogDto { UserId = 2, Action = ActivityLogConstants.Actions.DELETE, Entity = ActivityLogConstants.Entities.Quiz });

            var stats = await svc.GetActivityStatisticsAsync();

            Assert.Equal(3, stats.TotalActions);
            Assert.Equal(2, stats.UniqueUsers);
            Assert.Equal(2, stats.ActionsByType[ActivityLogConstants.Actions.CREATE]);
            Assert.Equal(1, stats.ActionsByType[ActivityLogConstants.Actions.DELETE]);
            Assert.Equal(2, stats.EntitiesAffected[ActivityLogConstants.Entities.Quiz]);
            Assert.Equal(1, stats.EntitiesAffected[ActivityLogConstants.Entities.Course]);
        }

        [Fact]
        public async Task GetActivityStatistics_ReturnsZeros_WhenNoLogs()
        {
            var svc = CreateService();
            var stats = await svc.GetActivityStatisticsAsync();
            Assert.Equal(0, stats.TotalActions);
            Assert.Equal(0, stats.UniqueUsers);
            Assert.Empty(stats.ActionsByType);
        }
    }
}
