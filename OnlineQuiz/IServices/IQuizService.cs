using OnlineQuiz.DTOs;

namespace OnlineQuiz.IServices
{
    public interface IQuizService
    {
        Task<QuizResponseDto> CreateQuizAsync(CreateQuizDto createQuizDto);
        Task<List<QuizResponseDto>> GetQuizzesForCourseAsync(int courseId, int userId, bool isStudent);
        Task<QuizResponseDto?> GetQuizByIdAsync(int quizId);
        Task<QuizResponseDto> UpdateQuizAsync(int quizId, UpdateQuizDto updateQuizDto, int userId);
        Task<bool> DeleteQuizAsync(int quizId, int userId);
        Task<int> BulkDeleteQuizzesAsync(List<int> quizIds, int userId);
    }
}
