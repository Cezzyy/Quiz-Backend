using Mapster;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.IServices;
using OnlineQuiz.Models;
using OnlineQuiz.Utilities;

namespace OnlineQuiz.Services
{
    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _quizRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserRoleRepository _userRoleRepository;

        public QuizService(
            IQuizRepository quizRepository,
            ICourseRepository courseRepository,
            IEnrollmentRepository enrollmentRepository,
            IUserRepository userRepository,
            IUserRoleRepository userRoleRepository)
        {
            _quizRepository = quizRepository;
            _courseRepository = courseRepository;
            _enrollmentRepository = enrollmentRepository;
            _userRepository = userRepository;
            _userRoleRepository = userRoleRepository;
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
            if (course.InstructorUserId != createQuizDto.CreatedBy)
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
                if (course != null && course.InstructorUserId != userId)
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

            // Collect all question IDs
            var questionIds = questions.Select(q => q.QuestionId).ToList();
            
            // Batch fetch choices
            var allChoices = await _quizRepository.GetChoicesByQuestionIdsAsync(questionIds);
            var choicesMap = allChoices.GroupBy(c => c.QuestionId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var q in questions)
            {
                var qDto = q.Adapt<QuestionResponseDto>();
                if (choicesMap.TryGetValue(q.QuestionId, out var choices))
                {
                    qDto.Choices = choices.Adapt<List<ChoiceResponseDto>>();
                }
                else
                {
                    qDto.Choices = new List<ChoiceResponseDto>();
                }
                response.Questions.Add(qDto);
            }

            return response;
        }

        public async Task<bool> DeleteQuizAsync(int quizId, int userId)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            if (quiz == null)
            {
                return false;
            }

            var course = await _courseRepository.GetByIdAsync(quiz.CourseId);
            if (course == null)
            {
                throw new InvalidOperationException("Quiz belongs to a non-existent course");
            }

            // Check if user is the instructor
            if (course.InstructorUserId != userId)
            {
                // Check if user is Admin
                var userRoles = await _userRoleRepository.GetByUserIdAsync(userId);
                var isAdmin = userRoles.Any(ur => ur.RoleId == RoleConstants.Admin); // Assuming 1 is Admin Role ID

                if (!isAdmin)
                {
                    throw new UnauthorizedAccessException("Only the assigned instructor or an admin can delete quizzes");
                }
            }

            return await _quizRepository.DeleteAsync(quizId);
        }
    }
}
