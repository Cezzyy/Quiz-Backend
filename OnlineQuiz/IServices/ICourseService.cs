using OnlineQuiz.DTOs;

namespace OnlineQuiz.IServices
{
    public interface ICourseService
    {
        Task<CourseResponseDto> CreateCourseAsync(CreateCourseDto createCourseDto);
        Task<CourseResponseDto?> GetCourseByIdAsync(int courseId);
        Task<List<CourseResponseDto>> GetCoursesForTeacherAsync(int teacherId);
        Task<List<CourseResponseDto>> GetCoursesForStudentAsync(int studentId);
        Task<EnrollmentResponseDto> EnrollStudentAsync(EnrollStudentDto enrollStudentDto);
        Task<List<EnrollmentResponseDto>> GetCourseEnrollmentsAsync(int courseId, int teacherId);
        Task<bool> UnenrollStudentAsync(int courseId, int studentId, int teacherId);
        Task<CourseResponseDto> UpdateCourseAsync(int courseId, UpdateCourseDto updateCourseDto);
        Task<bool> DeleteCourseAsync(int courseId);
        Task<List<CourseResponseDto>> GetAllCoursesAsync();
        Task<int> BulkDeleteCoursesAsync(List<int> courseIds);
        Task<int> BulkUnenrollStudentsAsync(BulkDeleteEnrollmentsDto dto, int teacherId);
    }
}
