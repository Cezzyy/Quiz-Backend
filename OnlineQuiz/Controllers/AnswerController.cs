using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;

namespace OnlineQuiz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnswerController : ControllerBase
    {
        private readonly IAnswerService _answerService;

        public AnswerController(IAnswerService answerService)
        {
            _answerService = answerService;
        }

        /// <summary>
        /// Record an answer for a question (Student only)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AnswerResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AnswerResponseDto>> RecordAnswer([FromBody] CreateAnswerDto createAnswerDto, [FromQuery] int studentId)
        {
            try
            {
                var answer = await _answerService.RecordAnswerAsync(createAnswerDto, studentId);
                return CreatedAtAction(nameof(GetAnswersForAttempt), new { attemptId = answer.AttemptId, userId = studentId }, answer);
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
        /// Get all answers for an attempt
        /// </summary>
        [HttpGet("attempt/{attemptId}")]
        [ProducesResponseType(typeof(List<AnswerResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<AnswerResponseDto>>> GetAnswersForAttempt(int attemptId, [FromQuery] int userId)
        {
            try
            {
                var answers = await _answerService.GetAnswersForAttemptAsync(attemptId, userId);
                if (!answers.Any())
                {
                    return NotFound(new { error = "No answers found for this attempt" });
                }
                return Ok(answers);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
