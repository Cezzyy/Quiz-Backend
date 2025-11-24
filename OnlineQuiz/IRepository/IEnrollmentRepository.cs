using OnlineQuiz.Models;

namespace OnlineQuiz.IRepository
{
    public interface IEnrollmentRepository
    {
        Task<Enrollment> CreateAsync(Enrollment enrollment);
        Task<bool> ExistsAsync(int studentId, int courseId);
        Task<List<Enrollment>> GetByCourseIdAsync(int courseId);
    }
}
