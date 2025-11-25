using OnlineQuiz.DTOs;

namespace OnlineQuiz.IServices
{
    public interface IAttemptService
    {
        Task<AttemptResponseDto> StartAttemptAsync(StartAttemptDto startAttemptDto);
        Task<AttemptResponseDto?> GetAttemptByIdAsync(int attemptId, int userId);
        Task<List<AttemptResponseDto>> GetAttemptsForQuizAsync(int quizId, int teacherId);
        Task<PagedResult<AttemptResponseDto>> GetAttemptsForQuizPagedAsync(int quizId, int teacherId, PaginationParams paginationParams);
        Task<List<AttemptResponseDto>> GetAttemptsForStudentAsync(int studentId);
        Task<PagedResult<AttemptResponseDto>> GetAttemptsForStudentPagedAsync(int studentId, PaginationParams paginationParams);
        Task<AttemptResponseDto> SubmitAttemptAsync(int attemptId, SubmitAttemptDto submitAttemptDto, int studentId);
        Task<bool> DeleteAttemptAsync(int attemptId, int userId);
        Task<int> BulkDeleteAttemptsAsync(List<int> attemptIds, int userId);
        Task<(byte[] FileContent, string FileName)> ExportQuizScoresToExcelAsync(int userId, int? quizId, int? courseId);
    }
}
