using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        /// Record multiple answers for an attempt (Student only)
        /// </summary>
        [HttpPost("bulk")]
        [EnableRateLimiting("quiz-submission")]
        [ProducesResponseType(typeof(List<AnswerResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<AnswerResponseDto>>> RecordBulkAnswers([FromBody] BulkAnswerRequestDto bulkAnswerDto, [FromQuery] int studentId)
        {
            try
            {
                var answers = await _answerService.RecordBulkAnswersAsync(bulkAnswerDto, studentId);
                return Ok(answers);
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
                    return Ok(new List<AnswerResponseDto>());
                }
                return Ok(answers);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing answer (Student only)
        /// </summary>
        [HttpPut("{answerId}")]
        [ProducesResponseType(typeof(AnswerResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AnswerResponseDto>> UpdateAnswer(int answerId, [FromBody] CreateAnswerDto createAnswerDto, [FromQuery] int studentId)
        {
            try
            {
                var answer = await _answerService.UpdateAnswerAsync(answerId, createAnswerDto, studentId);
                return Ok(answer);
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

        /// <summary>
        /// Delete an answer (Student only)
        /// </summary>
        [HttpDelete("{answerId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteAnswer(int answerId, [FromQuery] int studentId)
        {
            try
            {
                await _answerService.DeleteAnswerAsync(answerId, studentId);
                return NoContent();
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
