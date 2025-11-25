using OnlineQuiz.DTOs;

namespace OnlineQuiz.IServices
{
    public interface IChoiceService
    {
        Task<ChoiceResponseDto> CreateChoiceAsync(CreateChoiceDto createChoiceDto, int teacherId);
        Task<List<ChoiceResponseDto>> GetChoicesForQuestionAsync(int questionId, int userId, bool isStudent);
        Task<ChoiceResponseDto> UpdateChoiceAsync(int choiceId, CreateChoiceDto updateChoiceDto, int teacherId);
        Task<bool> DeleteChoiceAsync(int choiceId, int teacherId);
    }
}
