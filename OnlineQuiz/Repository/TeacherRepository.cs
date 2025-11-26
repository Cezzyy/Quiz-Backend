using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;

namespace OnlineQuiz.Repository
{
    public class TeacherRepository : ITeacherRepository
    {
        private readonly SupabaseService _supabaseService;

        public TeacherRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<Teacher> CreateAsync(Teacher teacher)
        {
            var client = _supabaseService.GetClient();
            var options = new Postgrest.QueryOptions { Returning = Postgrest.QueryOptions.ReturnType.Representation };
            var result = await client.From<Teacher>().Insert(teacher, options);
            return result.Models.First();
        }

        public async Task<Teacher?> GetByUserIdAsync(int userId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Teacher>()
                .Where(t => t.UserId == userId)
                .Get();
            return result.Models.FirstOrDefault();
        }

        public async Task<List<Teacher>> GetAllAsync()
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Teacher>().Get();
            return result.Models;
        }

        public async Task<Teacher> UpdateAsync(Teacher teacher)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Teacher>().Update(teacher);
            return result.Models.First();
        }

        public async Task<bool> DeleteAsync(int userId)
        {
            var client = _supabaseService.GetClient();
            await client.From<Teacher>()
                .Where(t => t.UserId == userId)
                .Delete();
            return true;
        }
    }
}
