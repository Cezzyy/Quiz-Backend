using OnlineQuiz.DTOs;

namespace OnlineQuiz.IServices
{
    public interface IQuestionService
    {
        Task<QuestionResponseDto> CreateQuestionAsync(CreateQuestionDto createQuestionDto, int teacherId);
        Task<QuestionResponseDto?> GetQuestionByIdAsync(int questionId);
        Task<List<QuestionResponseDto>> GetQuestionsForQuizAsync(int quizId, int userId, bool isStudent);
        Task<QuestionResponseDto> UpdateQuestionAsync(int questionId, CreateQuestionDto updateQuestionDto, int teacherId);
        Task<bool> DeleteQuestionAsync(int questionId, int teacherId);
    }
}
