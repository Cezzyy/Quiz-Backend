using OnlineQuiz.DTOs;

namespace OnlineQuiz.IServices
{
    public interface ICourseService
    {
        Task<CourseResponseDto> CreateCourseAsync(CreateCourseDto createCourseDto);
        Task<List<CourseResponseDto>> GetCoursesForTeacherAsync(int teacherId);
        Task<List<CourseResponseDto>> GetCoursesForStudentAsync(int studentId);
        Task<EnrollmentResponseDto> EnrollStudentAsync(EnrollStudentDto enrollStudentDto);
        Task<List<EnrollmentResponseDto>> GetCourseEnrollmentsAsync(int courseId, int teacherId);
    }
}
