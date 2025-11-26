using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface IStudentRepository
    {
        Task<Student> CreateAsync(Student student);
        Task<Student?> GetByUserIdAsync(int userId);
        Task<List<Student>> GetAllAsync();
        Task<List<Student>> GetByIdsAsync(List<int> userIds);
        Task<Student> UpdateAsync(Student student);
        Task<bool> DeleteAsync(int userId);
    }
}
