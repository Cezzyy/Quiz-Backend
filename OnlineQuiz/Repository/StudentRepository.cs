using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;

namespace OnlineQuiz.Repository
{
    public class StudentRepository : IStudentRepository
    {
        private readonly SupabaseService _supabaseService;

        public StudentRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<Student> CreateAsync(Student student)
        {
            var client = _supabaseService.GetClient();
            var options = new Postgrest.QueryOptions { Returning = Postgrest.QueryOptions.ReturnType.Representation };
            var result = await client.From<Student>().Insert(student, options);
            return result.Models.First();
        }

        public async Task<Student?> GetByUserIdAsync(int userId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Student>()
                .Where(s => s.UserId == userId)
                .Get();
            return result.Models.FirstOrDefault();
        }

        public async Task<List<Student>> GetAllAsync()
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Student>().Get();
            return result.Models;
        }

        public async Task<List<Student>> GetByIdsAsync(List<int> userIds)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Student>()
                .Filter("UserId", Postgrest.Constants.Operator.In, userIds)
                .Get();
            return result.Models;
        }

        public async Task<Student> UpdateAsync(Student student)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Student>().Update(student);
            return result.Models.First();
        }

        public async Task<bool> DeleteAsync(int userId)
        {
            var client = _supabaseService.GetClient();
            await client.From<Student>()
                .Where(s => s.UserId == userId)
                .Delete();
            return true;
        }
    }
}
