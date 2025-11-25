using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface IAttemptAnswerRepository
    {
        Task<AttemptAnswer> CreateAsync(AttemptAnswer answer);
        Task<AttemptAnswer?> GetByIdAsync(int answerId);
        Task<List<AttemptAnswer>> GetByAttemptIdAsync(int attemptId);
        Task<AttemptAnswer> UpdateAsync(AttemptAnswer answer);
        Task<bool> DeleteAsync(int answerId);
    }
}
