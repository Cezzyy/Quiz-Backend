using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface IQuestionRepository
    {
        Task<Question> CreateAsync(Question question);
        Task<Question?> GetByIdAsync(int questionId);
        Task<List<Question>> GetByQuizIdAsync(int quizId);
        Task<Question> UpdateAsync(Question question);
        Task<bool> DeleteAsync(int questionId);
    }
}
