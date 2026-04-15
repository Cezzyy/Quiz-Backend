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
        Task<int> BulkDeleteAsync(List<int> courseIds);

        // Archive operations
        Task<Course?> ArchiveAsync(int courseId, int archivedBy);
        Task<Course?> UnarchiveAsync(int courseId);
        Task<int> BulkArchiveAsync(List<int> courseIds, int archivedBy);
        Task<int> BulkUnarchiveAsync(List<int> courseIds);
        Task<List<Course>> GetArchivedAsync();
        Task<List<Course>> GetAllIncludingArchivedAsync();
        Task<int> CountArchivedAsync();
    }
}
