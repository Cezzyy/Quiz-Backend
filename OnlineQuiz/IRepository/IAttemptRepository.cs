using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface IAttemptRepository
    {
        Task<Attempt> CreateAsync(Attempt attempt);
        Task<Attempt?> GetByIdAsync(int attemptId);
        Task<List<Attempt>> GetByQuizIdAsync(int quizId);
        Task<List<Attempt>> GetByStudentIdAsync(int studentId);
        Task<Attempt> UpdateAsync(Attempt attempt);
        Task<bool> DeleteAsync(int attemptId);
    }
}
