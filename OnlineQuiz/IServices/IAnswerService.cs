using OnlineQuiz.DTOs;

namespace OnlineQuiz.IServices
{
    public interface IAnswerService
    {
        Task<AnswerResponseDto> RecordAnswerAsync(CreateAnswerDto createAnswerDto, int studentId);
        Task<AttemptWithAnswersDto> GetAnswersForAttemptAsync(int attemptId, int userId);
        Task<AnswerResponseDto> UpdateAnswerAsync(int answerId, CreateAnswerDto updateAnswerDto, int studentId);
        Task<bool> DeleteAnswerAsync(int answerId, int studentId);
        Task<List<AnswerResponseDto>> RecordBulkAnswersAsync(BulkAnswerRequestDto bulkAnswerDto, int studentId);
    }
}
