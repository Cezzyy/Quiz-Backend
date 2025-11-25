using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;

namespace OnlineQuiz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _quizService;

        public QuizController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        /// <summary>
        /// Create a new quiz (Teacher only)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(QuizResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<QuizResponseDto>> CreateQuiz([FromBody] CreateQuizDto createQuizDto)
        {
            try
            {
                var quiz = await _quizService.CreateQuizAsync(createQuizDto);
                return CreatedAtAction(nameof(GetQuizById), new { quizId = quiz.QuizId }, quiz);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get quizzes for a course
        /// </summary>
        [HttpGet("course/{courseId}")]
        [ProducesResponseType(typeof(List<QuizResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<QuizResponseDto>>> GetQuizzesForCourse(int courseId, [FromQuery] int userId, [FromQuery] bool isStudent)
        {
            try
            {
                var quizzes = await _quizService.GetQuizzesForCourseAsync(courseId, userId, isStudent);
                return Ok(quizzes);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific quiz by ID
        /// </summary>
        [HttpGet("{quizId}")]
        [ProducesResponseType(typeof(QuizResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<QuizResponseDto>> GetQuizById(int quizId)
        {
            var quiz = await _quizService.GetQuizByIdAsync(quizId);
            if (quiz == null)
            {
                return NotFound(new { error = "Quiz not found" });
            }
            return Ok(quiz);
        }

        /// <summary>
        /// Delete a quiz (Admin or Course Instructor only)
        /// </summary>
        [HttpDelete("{quizId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> DeleteQuiz(int quizId, [FromQuery] int userId)
        {
            try
            {
                var result = await _quizService.DeleteQuizAsync(quizId, userId);
                if (!result)
                {
                    return NotFound(new { error = "Quiz not found" });
                }
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
