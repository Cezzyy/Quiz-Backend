using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface IChoiceRepository
    {
        Task<Choice> CreateAsync(Choice choice);
        Task<Choice?> GetByIdAsync(int choiceId);
        Task<List<Choice>> GetByQuestionIdAsync(int questionId);
        Task<Choice> UpdateAsync(Choice choice);
        Task<bool> DeleteAsync(int choiceId);
    }
}
