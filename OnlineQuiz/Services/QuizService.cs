using Mapster;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.IServices;
using OnlineQuiz.Models;

namespace OnlineQuiz.Services
{
    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _quizRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;

        public QuizService(
            IQuizRepository quizRepository,
            ICourseRepository courseRepository,
            IEnrollmentRepository enrollmentRepository)
        {
            _quizRepository = quizRepository;
            _courseRepository = courseRepository;
            _enrollmentRepository = enrollmentRepository;
        }

        public async Task<QuizResponseDto> CreateQuizAsync(CreateQuizDto createQuizDto)
        {
            // Verify course exists
            var course = await _courseRepository.GetByIdAsync(createQuizDto.CourseId);
            if (course == null)
            {
                throw new ArgumentException($"Course with ID {createQuizDto.CourseId} not found");
            }

            // Verify creator is the instructor
            if (course.InstructorId != createQuizDto.CreatedBy)
            {
                throw new UnauthorizedAccessException("Only the assigned instructor can create quizzes for this course");
            }

            var quiz = new Quiz
            {
                CourseId = createQuizDto.CourseId,
                Title = createQuizDto.Title,
                DueAt = createQuizDto.DueAt,
                TimeLimitMinutes = createQuizDto.TimeLimitMinutes,
                CreatedBy = createQuizDto.CreatedBy,
                IsPublished = false, // Default to draft
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdQuiz = await _quizRepository.CreateAsync(quiz);

            // Add Questions
            foreach (var qDto in createQuizDto.Questions)
            {
                var question = new Question
                {
                    QuizId = createdQuiz.QuizId,
                    Type = qDto.Type,
                    Body = qDto.Body,
                    Points = qDto.Points,
                    SortOrder = qDto.SortOrder
                };
                var createdQuestion = await _quizRepository.CreateQuestionAsync(question);

                foreach (var cDto in qDto.Choices)
                {
                    var choice = new Choice
                    {
                        QuestionId = createdQuestion.QuestionId,
                        Body = cDto.Body,
                        IsCorrect = cDto.IsCorrect
                    };
                    await _quizRepository.CreateChoiceAsync(choice);
                }
            }

            return await GetQuizByIdAsync(createdQuiz.QuizId) 
                ?? throw new InvalidOperationException("Failed to retrieve created quiz");
        }

        public async Task<List<QuizResponseDto>> GetQuizzesForCourseAsync(int courseId, int userId, bool isStudent)
        {
            // Verify access
            if (isStudent)
            {
                var isEnrolled = await _enrollmentRepository.ExistsAsync(userId, courseId);
                if (!isEnrolled)
                {
                    throw new UnauthorizedAccessException("Student is not enrolled in this course");
                }
            }
            else
            {
                var course = await _courseRepository.GetByIdAsync(courseId);
                if (course != null && course.InstructorId != userId)
                {
                    // Allow admin? For now strict teacher check
                    // throw new UnauthorizedAccessException("Teacher is not assigned to this course");
                }
            }

            var quizzes = await _quizRepository.GetByCourseIdAsync(courseId);
            
            // Filter for students: only published quizzes
            if (isStudent)
            {
                quizzes = quizzes.Where(q => q.IsPublished).ToList();
            }

            var response = new List<QuizResponseDto>();
            foreach (var quiz in quizzes)
            {
                // Shallow map for list view (optimization: don't load questions for list)
                response.Add(quiz.Adapt<QuizResponseDto>());
            }

            return response;
        }

        public async Task<QuizResponseDto?> GetQuizByIdAsync(int quizId)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            if (quiz == null) return null;

            var response = quiz.Adapt<QuizResponseDto>();
            
            var questions = await _quizRepository.GetQuestionsByQuizIdAsync(quizId);
            response.Questions = new List<QuestionResponseDto>();

            foreach (var q in questions)
            {
                var qDto = q.Adapt<QuestionResponseDto>();
                var choices = await _quizRepository.GetChoicesByQuestionIdAsync(q.QuestionId);
                qDto.Choices = choices.Adapt<List<ChoiceResponseDto>>();
                response.Questions.Add(qDto);
            }

            return response;
        }
    }
}
