using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;

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
            return result.Models.First();
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
            var result = await client.From<User>().Get();
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
            var result = await client.From<User>().Count(Postgrest.Constants.CountType.Exact);
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
    }
}
