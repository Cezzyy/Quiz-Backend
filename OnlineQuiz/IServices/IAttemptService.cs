using OnlineQuiz.DTOs;

namespace OnlineQuiz.IServices
{
    public interface IAttemptService
    {
        Task<AttemptResponseDto> StartAttemptAsync(StartAttemptDto startAttemptDto);
        Task<AttemptResponseDto?> GetAttemptByIdAsync(int attemptId, int userId);
        Task<List<AttemptResponseDto>> GetAttemptsForQuizAsync(int quizId, int teacherId);
        Task<List<AttemptResponseDto>> GetAttemptsForStudentAsync(int studentId);
        Task<AttemptResponseDto> SubmitAttemptAsync(int attemptId, SubmitAttemptDto submitAttemptDto, int studentId);
    }
}
