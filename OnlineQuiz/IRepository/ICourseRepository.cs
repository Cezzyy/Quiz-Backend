using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface ICourseRepository
    {
        Task<Course> CreateAsync(Course course);
        Task<Course?> GetByIdAsync(int courseId);
        Task<List<Course>> GetAllAsync();
        Task<Course> UpdateAsync(Course course);
        Task<bool> DeleteAsync(int courseId);
        Task<List<Course>> GetByInstructorIdAsync(int instructorId);
        Task<List<Course>> GetByStudentIdAsync(int studentId);
        Task<int> CountAsync();
        Task<int> CountByInstructorAsync(int instructorId);
    }
}
