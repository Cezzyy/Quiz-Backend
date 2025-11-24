using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;
using Postgrest;

namespace OnlineQuiz.Repository
{
    public class QuizRepository : IQuizRepository
    {
        private readonly SupabaseService _supabaseService;

        public QuizRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<Quiz> CreateAsync(Quiz quiz)
        {
            var response = await _supabaseService.GetClient().From<Quiz>().Insert(quiz);
            return response.Model;
        }

        public async Task<Quiz?> GetByIdAsync(int quizId)
        {
            var response = await _supabaseService.GetClient().From<Quiz>()
                .Where(q => q.QuizId == quizId)
                .Single();
            return response;
        }

        public async Task<List<Quiz>> GetByCourseIdAsync(int courseId)
        {
            var response = await _supabaseService.GetClient().From<Quiz>()
                .Where(q => q.CourseId == courseId)
                .Get();
            return response.Models;
        }

        public async Task<Quiz> UpdateAsync(Quiz quiz)
        {
            var response = await _supabaseService.GetClient().From<Quiz>().Update(quiz);
            return response.Model;
        }

        public async Task<bool> DeleteAsync(int quizId)
        {
            await _supabaseService.GetClient().From<Quiz>()
                .Where(q => q.QuizId == quizId)
                .Delete();
            return true;
        }

        public async Task<Question> CreateQuestionAsync(Question question)
        {
            var response = await _supabaseService.GetClient().From<Question>().Insert(question);
            return response.Model;
        }

        public async Task<Choice> CreateChoiceAsync(Choice choice)
        {
            var response = await _supabaseService.GetClient().From<Choice>().Insert(choice);
            return response.Model;
        }

        public async Task<List<Question>> GetQuestionsByQuizIdAsync(int quizId)
        {
            var response = await _supabaseService.GetClient().From<Question>()
                .Where(q => q.QuizId == quizId)
                .Order("Sort_Order", Postgrest.Constants.Ordering.Ascending)
                .Get();
            return response.Models;
        }

        public async Task<List<Choice>> GetChoicesByQuestionIdAsync(int questionId)
        {
            var response = await _supabaseService.GetClient().From<Choice>()
                .Where(c => c.QuestionId == questionId)
                .Get();
            return response.Models;
        }
    }
}
