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
        Task<List<ClassmateDto>> GetCourseClassmatesAsync(int courseId, int studentId);
        Task<bool> UnenrollStudentAsync(int courseId, int studentId, int teacherId);
        Task<CourseResponseDto> UpdateCourseAsync(int courseId, UpdateCourseDto updateCourseDto);
        Task<bool> DeleteCourseAsync(int courseId);
        Task<List<CourseResponseDto>> GetAllCoursesAsync();
        Task<PagedResult<CourseResponseDto>> GetAllCoursesPagedAsync(PaginationParams paginationParams);
        Task<PagedResult<CourseResponseDto>> GetCoursesForTeacherPagedAsync(int teacherId, PaginationParams paginationParams);
        Task<PagedResult<CourseResponseDto>> GetCoursesForStudentPagedAsync(int studentId, PaginationParams paginationParams);
        Task<int> BulkDeleteCoursesAsync(List<int> courseIds);
        Task<int> BulkUnenrollStudentsAsync(BulkDeleteEnrollmentsDto dto, int teacherId);

        // Archive operations
        Task<CourseResponseDto> ArchiveCourseAsync(int courseId, int archivedBy);
        Task<CourseResponseDto> UnarchiveCourseAsync(int courseId);
        Task<BulkArchiveResponseDto> BulkArchiveCoursesAsync(List<int> courseIds, int archivedBy);
        Task<BulkArchiveResponseDto> BulkUnarchiveCoursesAsync(List<int> courseIds);
        Task<List<CourseResponseDto>> GetArchivedCoursesAsync();
        Task<PagedResult<CourseResponseDto>> GetArchivedCoursesPagedAsync(PaginationParams paginationParams);
        Task<ArchiveStatisticsDto> GetCourseArchiveStatisticsAsync();
    }
}
