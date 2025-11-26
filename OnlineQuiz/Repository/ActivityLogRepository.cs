using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;
using Postgrest;

namespace OnlineQuiz.Repository
{
    public class ActivityLogRepository : IActivityLogRepository
    {
        private readonly SupabaseService _supabaseService;

        public ActivityLogRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<ActivityLog> CreateAsync(ActivityLog log)
        {
            var client = _supabaseService.GetClient();
            var options = new Postgrest.QueryOptions { Returning = Postgrest.QueryOptions.ReturnType.Representation };
            var result = await client.From<ActivityLog>().Insert(log, options);
            var created = result.Models.First();
            
            // If ActivityLogId is not populated, fetch by UserId and CreatedAt
            if (created.ActivityLogId == 0)
            {
                var fetchResult = await client.From<ActivityLog>()
                    .Where(a => a.UserId == log.UserId && a.Action == log.Action)
                    .Order("CreatedAt", Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();
                return fetchResult.Models.FirstOrDefault() ?? created;
            }
            
            return created;
        }

        public async Task<ActivityLog?> GetByIdAsync(long activityLogId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<ActivityLog>()
                .Where(a => a.ActivityLogId == activityLogId)
                .Get();
            return result.Models.FirstOrDefault();
        }

        public async Task<List<ActivityLog>> GetAllAsync()
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<ActivityLog>()
                .Order("CreatedAt", Constants.Ordering.Descending)
                .Get();
            return result.Models;
        }

        public async Task<List<ActivityLog>> GetByUserIdAsync(int userId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<ActivityLog>()
                .Where(a => a.UserId == userId)
                .Order("CreatedAt", Constants.Ordering.Descending)
                .Get();
            return result.Models;
        }

        public async Task<List<ActivityLog>> GetByActionAsync(string action)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<ActivityLog>()
                .Filter("Action", Constants.Operator.Equals, action)
                .Order("CreatedAt", Constants.Ordering.Descending)
                .Get();
            return result.Models;
        }

        public async Task<List<ActivityLog>> GetByEntityAsync(string entity, long? entityId = null)
        {
            var client = _supabaseService.GetClient();
            var query = client.From<ActivityLog>()
                .Filter("Entity", Constants.Operator.Equals, entity);

            if (entityId.HasValue)
            {
                query = query.Filter("EntityId", Constants.Operator.Equals, entityId.Value.ToString());
            }

            var result = await query
                .Order("CreatedAt", Constants.Ordering.Descending)
                .Get();
            return result.Models;
        }

        public async Task<List<ActivityLog>> GetFilteredAsync(ActivityLogFilterDto filter)
        {
            var client = _supabaseService.GetClient();
            
            // Build query with all filters applied at once
            var baseQuery = (Postgrest.Table<ActivityLog>)client.From<ActivityLog>();

            // Apply filters - we need to apply them directly without trying to chain conditionally
            if (filter.UserId.HasValue)
            {
                baseQuery = baseQuery.Where(a => a.UserId == filter.UserId.Value);
            }

            if (!string.IsNullOrEmpty(filter.Action))
            {
                baseQuery = baseQuery.Filter("Action", Constants.Operator.Equals, filter.Action);
            }

            if (!string.IsNullOrEmpty(filter.Entity))
            {
                baseQuery = baseQuery.Filter("Entity", Constants.Operator.Equals, filter.Entity);
            }

            if (filter.EntityId.HasValue)
            {
                baseQuery = baseQuery.Filter("EntityId", Constants.Operator.Equals, filter.EntityId.Value.ToString());
            }

            if (filter.StartDate.HasValue)
            {
                baseQuery = baseQuery.Filter("CreatedAt", Constants.Operator.GreaterThanOrEqual, filter.StartDate.Value.ToString("o"));
            }

            if (filter.EndDate.HasValue)
            {
                baseQuery = baseQuery.Filter("CreatedAt", Constants.Operator.LessThanOrEqual, filter.EndDate.Value.ToString("o"));
            }

            // Apply ordering and pagination
            baseQuery = baseQuery.Order("CreatedAt", Constants.Ordering.Descending);

            var skip = (filter.PageNumber - 1) * filter.PageSize;
            baseQuery = baseQuery.Range(skip, skip + filter.PageSize - 1);

            var result = await baseQuery.Get();
            return result.Models;
        }

        public async Task<List<ActivityLog>> GetRecentAsync(int limit)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<ActivityLog>()
                .Order("CreatedAt", Constants.Ordering.Descending)
                .Limit(limit)
                .Get();
            return result.Models;
        }
    }
}
