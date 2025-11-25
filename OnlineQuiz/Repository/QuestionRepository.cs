using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;

namespace OnlineQuiz.Repository
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly SupabaseService _supabaseService;

        public QuestionRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<Question> CreateAsync(Question question)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Question>().Insert(question);
            return result.Models.First();
        }

        public async Task<Question?> GetByIdAsync(int questionId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Question>()
                .Where(q => q.QuestionId == questionId)
                .Get();
            return result.Models.FirstOrDefault();
        }

        public async Task<List<Question>> GetByQuizIdAsync(int quizId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Question>()
                .Where(q => q.QuizId == quizId)
                .Order("Sort_Order", Postgrest.Constants.Ordering.Ascending)
                .Get();
            return result.Models;
        }

        public async Task<Question> UpdateAsync(Question question)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Question>().Update(question);
            return result.Models.First();
        }

        public async Task<bool> DeleteAsync(int questionId)
        {
            var client = _supabaseService.GetClient();
            await client.From<Question>()
                .Where(q => q.QuestionId == questionId)
                .Delete();
            return true;
        }
    }
}
