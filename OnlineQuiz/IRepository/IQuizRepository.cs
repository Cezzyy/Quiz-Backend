using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface IQuizRepository
    {
        Task<Quiz> CreateAsync(Quiz quiz);
        Task<Quiz?> GetByIdAsync(int quizId);
        Task<List<Quiz>> GetByCourseIdAsync(int courseId);
        Task<Quiz> UpdateAsync(Quiz quiz);
        Task<bool> DeleteAsync(int quizId);
        
        // Question and Choice management
        Task<Question> CreateQuestionAsync(Question question);
        Task<Choice> CreateChoiceAsync(Choice choice);
        Task<List<Question>> GetQuestionsByQuizIdAsync(int quizId);
        Task<List<Choice>> GetChoicesByQuestionIdAsync(int questionId);
    }
}
