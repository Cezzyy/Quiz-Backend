using OnlineQuiz.DTOs;

namespace OnlineQuiz.IServices
{
    public interface IAnswerService
    {
        Task<AnswerResponseDto> RecordAnswerAsync(CreateAnswerDto createAnswerDto, int studentId);
        Task<List<AnswerResponseDto>> GetAnswersForAttemptAsync(int attemptId, int userId);
    }
}
