using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;

namespace OnlineQuiz.Repository
{
    public class AttemptRepository : IAttemptRepository
    {
        private readonly SupabaseService _supabaseService;

        public AttemptRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<Attempt> CreateAsync(Attempt attempt)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Attempt>().Insert(attempt);
            return result.Models.First();
        }

        public async Task<Attempt?> GetByIdAsync(int attemptId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Attempt>()
                .Where(a => a.AttemptId == attemptId)
                .Get();
            return result.Models.FirstOrDefault();
        }

        public async Task<List<Attempt>> GetByQuizIdAsync(int quizId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Attempt>()
                .Where(a => a.QuizId == quizId)
                .Order("StartedAt", Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models;
        }

        public async Task<List<Attempt>> GetByStudentIdAsync(int studentId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Attempt>()
                .Where(a => a.UserId == studentId)
                .Order("StartedAt", Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models;
        }

        public async Task<List<Attempt>> GetByQuizIdAndUserIdAsync(int quizId, int userId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Attempt>()
                .Where(a => a.QuizId == quizId && a.UserId == userId)
                .Order("StartedAt", Postgrest.Constants.Ordering.Descending)
                .Get();
            return result.Models;
        }

        public async Task<Attempt> UpdateAsync(Attempt attempt)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Attempt>().Update(attempt);
            return result.Models.First();
        }

        public async Task<bool> DeleteAsync(int attemptId)
        {
            var client = _supabaseService.GetClient();
            await client.From<Attempt>()
                .Where(a => a.AttemptId == attemptId)
                .Delete();
            return true;
        }
    }
}
