using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using OnlineQuiz.Utilities;

namespace OnlineQuiz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AttemptController : ControllerBase
    {
        private readonly IAttemptService _attemptService;
        private readonly IActivityLogService _activityLogService;

        public AttemptController(IAttemptService attemptService, IActivityLogService activityLogService)
        {
            _attemptService = attemptService;
            _activityLogService = activityLogService;
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
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AttemptResponseDto>> GetAttemptById(int attemptId, [FromQuery] int userId)
        {
            try
            {
                var attempt = await _attemptService.GetAttemptByIdAsync(attemptId, userId);
                if (attempt == null)
                {
                    return NotFound(new { error = "Attempt not found" });
                }
                return Ok(attempt);
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
                    return Ok(new List<AttemptResponseDto>());
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
                return Ok(new List<AttemptResponseDto>());
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

                // Log the SUBMIT activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = studentId,
                        Action = ActivityLogConstants.Actions.SUBMIT,
                        Entity = ActivityLogConstants.Entities.Attempt,
                        EntityId = attempt.AttemptId,
                        Description = $"Submitted attempt for quiz {attempt.QuizId} with score {attempt.Score}",
                        NewValues = new { attempt.AttemptId, attempt.QuizId, attempt.Score, attempt.SubmittedAt },
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log SUBMIT activity: {logEx.Message}");
                }

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


        /// <summary>
        /// Delete an attempt
        /// </summary>
        [HttpDelete("{attemptId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteAttempt(int attemptId, [FromQuery] int userId)
        {
            try
            {
                await _attemptService.DeleteAttemptAsync(attemptId, userId);
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

        /// <summary>
        /// Bulk delete attempts (Teacher for their courses, Student for own unsubmitted)
        /// </summary>
        [HttpDelete("bulk")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> BulkDeleteAttempts([FromBody] BulkDeleteAttemptsDto dto)
        {
            try
            {
                var deletedCount = await _attemptService.BulkDeleteAttemptsAsync(dto.AttemptIds, dto.UserId);
                
                // Log the BULK_DELETE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = dto.UserId,
                        Action = ActivityLogConstants.Actions.DELETE,
                        Entity = ActivityLogConstants.Entities.Attempt,
                        Description = $"Bulk deleted {deletedCount} attempts",
                        NewValues = new { AttemptIds = dto.AttemptIds, DeletedCount = deletedCount },
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log BULK_DELETE activity: {logEx.Message}");
                }

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Export quiz scores to Excel (Admin or Teacher only)
        /// </summary>
        [HttpGet("export")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExportScores([FromQuery] int userId, [FromQuery] int? quizId, [FromQuery] int? courseId)
        {
            try
            {
                var (fileContent, fileName) = await _attemptService.ExportQuizScoresToExcelAsync(userId, quizId, courseId);

                // Log the EXPORT activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = userId,
                        Action = ActivityLogConstants.Actions.EXPORT,
                        Entity = ActivityLogConstants.Entities.Attempt,
                        Description = $"Exported quiz scores to {fileName}",
                        NewValues = new { QuizId = quizId, CourseId = courseId, FileName = fileName },
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log EXPORT activity: {logEx.Message}");
                }

                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
