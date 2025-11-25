using OnlineQuiz.DTOs;

namespace OnlineQuiz.IServices
{
    public interface IQuizService
    {
        Task<QuizResponseDto> CreateQuizAsync(CreateQuizDto createQuizDto);
        Task<List<QuizResponseDto>> GetQuizzesForCourseAsync(int courseId, int userId, bool isStudent);
        Task<QuizResponseDto?> GetQuizByIdAsync(int quizId);
        Task<bool> DeleteQuizAsync(int quizId, int userId);
        Task<QuizResponseDto> UpdateQuizAsync(int quizId, UpdateQuizDto updateQuizDto, int teacherId);
        Task<QuizResponseDto> PublishQuizAsync(int quizId, bool isPublished, int teacherId);
    }
}
