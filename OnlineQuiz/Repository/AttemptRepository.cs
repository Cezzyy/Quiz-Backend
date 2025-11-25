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

        public async Task<double> GetAverageScoreByCourseAsync(int courseId)
        {
            var client = _supabaseService.GetClient();
            // We need to join with Quiz to filter by CourseId
            // Since Supabase-csharp join is limited, we might need to fetch attempts for quizzes in the course
            // Or use a stored procedure/view if performance is an issue.
            // For now, let's fetch quizzes for the course first, then attempts.
            
            var quizzes = await client.From<Quiz>().Where(q => q.CourseId == courseId).Select("QuizId").Get();
            var quizIds = quizzes.Models.Select(q => q.QuizId).ToList();
            
            if (!quizIds.Any()) return 0;

            var attempts = await client.From<Attempt>()
                .Filter("QuizId", Postgrest.Constants.Operator.In, quizIds)
                .Get();
                
            if (!attempts.Models.Any()) return 0;
            
            return (double)attempts.Models.Average(a => a.Score);
        }

        public async Task<List<Attempt>> GetRecentAttemptsByStudentAsync(int studentId, int count)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Attempt>()
                .Where(a => a.UserId == studentId)
                .Order("StartedAt", Postgrest.Constants.Ordering.Descending)
                .Range(0, count - 1)
                .Get();
            return result.Models;
        }

        public async Task<Dictionary<int, double>> GetAverageScoresByCourseIdsAsync(List<int> courseIds)
        {
            if (!courseIds.Any()) return new Dictionary<int, double>();

            var client = _supabaseService.GetClient();
            
            // Fetch all quizzes for these courses
            var quizzes = await client.From<Quiz>()
                .Filter("CourseId", Postgrest.Constants.Operator.In, courseIds)
                .Get();

            if (!quizzes.Models.Any()) return new Dictionary<int, double>();

            var quizIds = quizzes.Models.Select(q => q.QuizId).ToList();
            var quizToCourseMap = quizzes.Models.ToDictionary(q => q.QuizId, q => q.CourseId);

            // Fetch all attempts for these quizzes
            var attempts = await client.From<Attempt>()
                .Filter("QuizId", Postgrest.Constants.Operator.In, quizIds)
                .Get();

            if (!attempts.Models.Any()) return new Dictionary<int, double>();

            // Group by course and calculate averages
            return attempts.Models
                .Where(a => quizToCourseMap.ContainsKey(a.QuizId))
                .GroupBy(a => quizToCourseMap[a.QuizId])
                .ToDictionary(
                    g => g.Key,
                    g => (double)g.Average(a => a.Score)
                );
        }

        public async Task<List<Attempt>> GetByQuizIdsAndStudentIdAsync(List<int> quizIds, int studentId)
        {
            if (!quizIds.Any()) return new List<Attempt>();

            var client = _supabaseService.GetClient();
            var result = await client.From<Attempt>()
                .Filter("QuizId", Postgrest.Constants.Operator.In, quizIds)
                .Where(a => a.UserId == studentId)
                .Get();
            return result.Models;
        }

        public async Task<int> BulkDeleteAsync(List<int> attemptIds)
        {
            if (!attemptIds.Any()) return 0;

            var client = _supabaseService.GetClient();
            await client.From<Attempt>()
                .Filter("AttemptId", Postgrest.Constants.Operator.In, attemptIds)
                .Delete();
            return attemptIds.Count;
        }
    }
}
