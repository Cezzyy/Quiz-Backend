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
            var result = await client.From<User>().Insert(user);
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
    }
}
