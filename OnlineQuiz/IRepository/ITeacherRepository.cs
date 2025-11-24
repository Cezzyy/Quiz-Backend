using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface ITeacherRepository
    {
        Task<Teacher> CreateAsync(Teacher teacher);
        Task<Teacher?> GetByUserIdAsync(int userId);
        Task<List<Teacher>> GetAllAsync();
        Task<Teacher> UpdateAsync(Teacher teacher);
        Task<bool> DeleteAsync(int userId);
    }
}
