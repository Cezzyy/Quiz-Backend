using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using OnlineQuiz.Utilities;
using System.Security.Claims;

namespace OnlineQuiz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExportImportLogController : ControllerBase
    {
        private readonly IExportImportLogService _logService;
        private readonly IActivityLogService _activityLogService;

        public ExportImportLogController(IExportImportLogService logService, IActivityLogService activityLogService)
        {
            _logService = logService;
            _activityLogService = activityLogService;
        }

        /// <summary>
        /// Create a new export/import log
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ExportImportLogResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ExportImportLogResponseDto>> CreateLog([FromBody] CreateExportImportLogDto createLogDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                // Ensure the user is creating a log for themselves unless they are admin (optional check, but good practice)
                if (createLogDto.UserId != userId && !User.IsInRole("Admin"))
                {
                    createLogDto.UserId = userId;
                }

                var log = await _logService.CreateLogAsync(createLogDto);

                // Log the activity
                try
                {
                    var action = createLogDto.Type.Equals("Export", StringComparison.OrdinalIgnoreCase) 
                        ? ActivityLogConstants.Actions.EXPORT 
                        : ActivityLogConstants.Actions.IMPORT;

                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = userId,
                        Action = action,
                        Entity = ActivityLogConstants.Entities.System, // Or a specific entity if we had one for logs
                        EntityId = log.LogId,
                        Description = $"{createLogDto.Type} started: {createLogDto.FileName}",
                        NewValues = new { log.LogId, log.Type, log.FileName, log.Status },
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log {createLogDto.Type} activity: {logEx.Message}");
                }

                return CreatedAtAction(nameof(GetLogById), new { id = log.LogId }, log);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific log by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ExportImportLogResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ExportImportLogResponseDto>> GetLogById(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var log = await _logService.GetLogByIdAsync(id, userId);

                if (log == null)
                {
                    return NotFound(new { error = "Log not found" });
                }

                return Ok(log);
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
        /// Get all logs for the current user
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(List<ExportImportLogResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ExportImportLogResponseDto>>> GetMyLogs()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var logs = await _logService.GetLogsForUserAsync(userId);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get logs for a specific user (Admin only)
        /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(List<ExportImportLogResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ExportImportLogResponseDto>>> GetUserLogs(int userId)
        {
            try
            {
                var logs = await _logService.GetLogsForUserAsync(userId);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update log status
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ExportImportLogResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ExportImportLogResponseDto>> UpdateLog(int id, [FromBody] UpdateExportImportLogDto updateDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var log = await _logService.UpdateLogStatusAsync(id, updateDto, userId);
                return Ok(log);
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
        /// Delete a log
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> DeleteLog(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _logService.DeleteLogAsync(id, userId);

                if (!result)
                {
                    return NotFound(new { error = "Log not found" });
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
