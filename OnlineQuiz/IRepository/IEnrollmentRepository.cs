using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface IEnrollmentRepository
    {
        Task<Enrollment> CreateAsync(Enrollment enrollment);
        Task<bool> ExistsAsync(int studentId, int courseId);
        Task<List<Enrollment>> GetByCourseIdAsync(int courseId);
        Task<bool> DeleteAsync(int enrollmentId);
        Task<int> CountByCourseIdAsync(int courseId);
        Task<Dictionary<int, int>> CountByCourseIdsAsync(List<int> courseIds);
    }
}
