using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;

namespace OnlineQuiz.Repository
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly SupabaseService _supabaseService;

        public UserRoleRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<UserRole> CreateAsync(UserRole userRole)
        {
            var client = _supabaseService.GetClient();
            var options = new Postgrest.QueryOptions { Returning = Postgrest.QueryOptions.ReturnType.Representation };
            var result = await client.From<UserRole>().Insert(userRole, options);
            return result.Models.First();
        }

        public async Task<List<UserRole>> GetByUserIdAsync(int userId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<UserRole>()
                .Where(ur => ur.UserId == userId)
                .Get();
            return result.Models;
        }

        public async Task<List<UserRole>> GetAllAsync()
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<UserRole>().Get();
            return result.Models;
        }

        public async Task<bool> DeleteByUserIdAsync(int userId)
        {
            var client = _supabaseService.GetClient();
            await client.From<UserRole>()
                .Where(ur => ur.UserId == userId)
                .Delete();
            return true;
        }

        public async Task<bool> IsAdminAsync(int userId)
        {
            var roles = await GetByUserIdAsync(userId);
            return roles.Any(r => r.RoleId == 1); // RoleId 1 is Admin
        }
    }
}
