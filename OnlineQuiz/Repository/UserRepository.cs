using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;
using OnlineQuiz.Utilities;

namespace OnlineQuiz.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly SupabaseService _supabaseService;

        public UserRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<User> CreateAsync(User user)
        {
            var client = _supabaseService.GetClient();
            var options = new Postgrest.QueryOptions { Returning = Postgrest.QueryOptions.ReturnType.Representation };
            var result = await client.From<User>().Insert(user, options);
            var createdUser = result.Models.First();
            
            // If UserId is not populated (still 0), fetch it by email
            if (createdUser.UserId == 0)
            {
                var fetchedUser = await GetByEmailAsync(user.Email);
                return fetchedUser ?? throw new InvalidOperationException("Failed to retrieve created user");
            }
            
            return createdUser;
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<User>()
                .Where(u => u.UserId == userId)
                .Get();
            return result.Models.FirstOrDefault();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<User>()
                .Where(u => u.Email == email)
                .Get();
            return result.Models.FirstOrDefault();
        }

        public async Task<List<User>> GetAllAsync()
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<User>()
                .Where(u => u.Status != EntityStatusConstants.Archived)
                .Get();
            return result.Models;
        }

        public async Task<List<User>> GetByIdsAsync(List<int> userIds)
        {
            if (!userIds.Any()) return new List<User>();

            var client = _supabaseService.GetClient();
            var result = await client.From<User>()
                .Filter("UserId", Postgrest.Constants.Operator.In, userIds)
                .Get();
            return result.Models;
        }

        public async Task<int> CountAsync()
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<User>()
                .Where(u => u.Status != EntityStatusConstants.Archived)
                .Count(Postgrest.Constants.CountType.Exact);
            return result;
        }

        public async Task<int> CountByRoleAsync(int roleId)
        {
            var client = _supabaseService.GetClient();
            // Since we don't have a direct link in User table, we need to join with UserRole
            // But Supabase-csharp join syntax can be tricky. 
            // Alternative: Count from UserRole table.
            var result = await client.From<UserRole>()
                .Where(ur => ur.RoleId == roleId)
                .Count(Postgrest.Constants.CountType.Exact);
            return result;
        }

        public async Task<List<User>> GetRecentRegistrationsAsync(int days)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var client = _supabaseService.GetClient();
            var result = await client.From<User>()
                .Filter("CreatedAt", Postgrest.Constants.Operator.GreaterThanOrEqual, cutoffDate.ToString("o"))
                .Where(u => u.Status != EntityStatusConstants.Archived)
                .Order("CreatedAt", Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models;
        }

        public async Task<User> UpdateAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            var client = _supabaseService.GetClient();
            var result = await client.From<User>().Update(user);
            return result.Models.First();
        }

        public async Task<bool> DeleteAsync(int userId)
        {
            var client = _supabaseService.GetClient();
            await client.From<User>()
                .Where(u => u.UserId == userId)
                .Delete();
            return true;
        }

        public async Task<int> BulkDeleteAsync(List<int> userIds)
        {
            if (!userIds.Any()) return 0;

            var client = _supabaseService.GetClient();
            await client.From<User>()
                .Filter("UserId", Postgrest.Constants.Operator.In, userIds)
                .Delete();
            return userIds.Count;
        }

        // Archive operations
        public async Task<User?> ArchiveAsync(int userId, int archivedBy)
        {
            var user = await GetByIdAsync(userId);
            if (user == null) return null;

            user.Status = EntityStatusConstants.Archived;
            user.ArchivedAt = DateTime.UtcNow;
            user.ArchivedBy = archivedBy;
            user.UpdatedAt = DateTime.UtcNow;

            return await UpdateAsync(user);
        }

        public async Task<User?> UnarchiveAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null) return null;

            user.Status = EntityStatusConstants.Active;
            user.ArchivedAt = null;
            user.ArchivedBy = null;
            user.UpdatedAt = DateTime.UtcNow;

            return await UpdateAsync(user);
        }

        public async Task<int> BulkArchiveAsync(List<int> userIds, int archivedBy)
        {
            if (!userIds.Any()) return 0;

            var users = await GetByIdsAsync(userIds);
            var archiveCount = 0;

            foreach (var user in users)
            {
                user.Status = EntityStatusConstants.Archived;
                user.ArchivedAt = DateTime.UtcNow;
                user.ArchivedBy = archivedBy;
                user.UpdatedAt = DateTime.UtcNow;

                await UpdateAsync(user);
                archiveCount++;
            }

            return archiveCount;
        }

        public async Task<int> BulkUnarchiveAsync(List<int> userIds)
        {
            if (!userIds.Any()) return 0;

            var users = await GetByIdsAsync(userIds);
            var unarchiveCount = 0;

            foreach (var user in users)
            {
                user.Status = EntityStatusConstants.Active;
                user.ArchivedAt = null;
                user.ArchivedBy = null;
                user.UpdatedAt = DateTime.UtcNow;

                await UpdateAsync(user);
                unarchiveCount++;
            }

            return unarchiveCount;
        }

        public async Task<List<User>> GetArchivedAsync()
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<User>()
                .Where(u => u.Status == EntityStatusConstants.Archived)
                .Order("ArchivedAt", Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models;
        }

        public async Task<List<User>> GetAllIncludingArchivedAsync()
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<User>().Get();
            return result.Models;
        }

        public async Task<int> CountArchivedAsync()
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<User>()
                .Where(u => u.Status == EntityStatusConstants.Archived)
                .Count(Postgrest.Constants.CountType.Exact);
            return result;
        }
    }
}
