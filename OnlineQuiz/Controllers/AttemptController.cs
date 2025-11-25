using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;

namespace OnlineQuiz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AttemptController : ControllerBase
    {
        private readonly IAttemptService _attemptService;

        public AttemptController(IAttemptService attemptService)
        {
            _attemptService = attemptService;
        }

        /// <summary>
        /// Start a quiz attempt (Student only)
        /// </summary>
        [HttpPost("start")]
        [ProducesResponseType(typeof(AttemptResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AttemptResponseDto>> StartAttempt([FromBody] StartAttemptDto startAttemptDto)
        {
            try
            {
                var attempt = await _attemptService.StartAttemptAsync(startAttemptDto);
                return CreatedAtAction(nameof(GetAttemptById), new { attemptId = attempt.AttemptId, userId = startAttemptDto.StudentId }, attempt);
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
        /// Get attempt by ID
        /// </summary>
        [HttpGet("{attemptId}")]
        [ProducesResponseType(typeof(AttemptResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AttemptResponseDto>> GetAttemptById(int attemptId, [FromQuery] int userId)
        {
            var attempt = await _attemptService.GetAttemptByIdAsync(attemptId, userId);
            if (attempt == null)
            {
                return NotFound(new { error = "Attempt not found" });
            }
            return Ok(attempt);
        }

        /// <summary>
        /// Get all attempts for a quiz (Teacher only)
        /// </summary>
        [HttpGet("quiz/{quizId}")]
        [ProducesResponseType(typeof(List<AttemptResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<AttemptResponseDto>>> GetAttemptsForQuiz(int quizId, [FromQuery] int teacherId)
        {
            try
            {
                var attempts = await _attemptService.GetAttemptsForQuizAsync(quizId, teacherId);
                if (!attempts.Any())
                {
                    return NotFound(new { error = "No attempts found for this quiz" });
                }
                return Ok(attempts);
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
        /// Get all attempts for a student
        /// </summary>
        [HttpGet("student/{studentId}")]
        [ProducesResponseType(typeof(List<AttemptResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<AttemptResponseDto>>> GetAttemptsForStudent(int studentId)
        {
            var attempts = await _attemptService.GetAttemptsForStudentAsync(studentId);
            if (!attempts.Any())
            {
                return NotFound(new { error = "No attempts found for this student" });
            }
            return Ok(attempts);
        }

        /// <summary>
        /// Submit a quiz attempt (Student only)
        /// </summary>
        [HttpPut("{attemptId}/submit")]
        [ProducesResponseType(typeof(AttemptResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AttemptResponseDto>> SubmitAttempt(int attemptId, [FromBody] SubmitAttemptDto submitAttemptDto, [FromQuery] int studentId)
        {
            try
            {
                var attempt = await _attemptService.SubmitAttemptAsync(attemptId, submitAttemptDto, studentId);
                return Ok(attempt);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
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
