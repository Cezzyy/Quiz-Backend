using OnlineQuiz.DTOs;

namespace OnlineQuiz.IServices
{
    public interface IQuizService
    {
        Task<QuizResponseDto> CreateQuizAsync(CreateQuizDto createQuizDto);
        Task<List<QuizResponseDto>> GetQuizzesForCourseAsync(int courseId, int userId, bool isStudent);
        Task<QuizResponseDto?> GetQuizByIdAsync(int quizId);
    }
}
