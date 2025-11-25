using OnlineQuiz.DTOs;

namespace OnlineQuiz.Services
{
    public interface IAnalyticsService
    {
        Task<AdminDashboardDto> GetAdminDashboardAsync();
        Task<TeacherDashboardDto> GetTeacherDashboardAsync(int teacherId);
        Task<StudentDashboardDto> GetStudentDashboardAsync(int studentId);
        Task<CourseAnalyticsDto> GetCourseAnalyticsAsync(int courseId);
    }
}
