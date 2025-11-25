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
    public class ActivityLogController : ControllerBase
    {
        private readonly IActivityLogService _activityLogService;

        public ActivityLogController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }

        /// <summary>
        /// Get all activity logs (Admin only)
        /// </summary>
        /// <param name="filter">Filter parameters</param>
        /// <returns>List of activity logs</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<ActivityLogDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<ActivityLogDto>>> GetActivityLogs([FromQuery] ActivityLogFilterDto filter)
        {
            try
            {
                // Only admins can view all activity logs
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Admin")
                {
                    return Forbid();
                }

                var logs = await _activityLogService.GetActivityLogsAsync(filter);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving activity logs", details = ex.Message });
            }
        }

        /// <summary>
        /// Get activity log by ID (Admin only)
        /// </summary>
        /// <param name="id">Activity Log ID</param>
        /// <returns>Activity log details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ActivityLogDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ActivityLogDto>> GetActivityLogById(long id)
        {
            try
            {
                // Only admins can view activity logs
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Admin")
                {
                    return Forbid();
                }

                var log = await _activityLogService.GetActivityLogByIdAsync(id);
                if (log == null)
                {
                    return NotFound(new { error = $"Activity log with ID {id} not found" });
                }

                return Ok(log);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the activity log", details = ex.Message });
            }
        }

        /// <summary>
        /// Get activity logs for a specific user (Admin or own user)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="days">Number of days to retrieve (optional)</param>
        /// <returns>List of activity logs for the user</returns>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(List<ActivityLogDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<ActivityLogDto>>> GetUserActivityLogs(int userId, [FromQuery] int? days = null)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Allow admins or the user themselves
                if (userRole != "Admin" && currentUserId != userId.ToString())
                {
                    return Forbid();
                }

                var logs = await _activityLogService.GetUserActivityLogsAsync(userId, days);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving user activity logs", details = ex.Message });
            }
        }

        /// <summary>
        /// Get current user's activity logs
        /// </summary>
        /// <param name="days">Number of days to retrieve (optional)</param>
        /// <returns>List of activity logs for the current user</returns>
        [HttpGet("me")]
        [ProducesResponseType(typeof(List<ActivityLogDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ActivityLogDto>>> GetMyActivityLogs([FromQuery] int? days = null)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out var userId))
                {
                    return Unauthorized(new { error = "Invalid user credentials" });
                }

                var logs = await _activityLogService.GetUserActivityLogsAsync(userId, days);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving your activity logs", details = ex.Message });
            }
        }

        /// <summary>
        /// Get activity statistics (Admin only)
        /// </summary>
        /// <param name="userId">Filter by user ID (optional)</param>
        /// <param name="days">Number of days to analyze (default: 30)</param>
        /// <returns>Activity statistics</returns>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(ActivityLogStatisticsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ActivityLogStatisticsDto>> GetActivityStatistics([FromQuery] int? userId = null, [FromQuery] int? days = 30)
        {
            try
            {
                // Only admins can view statistics
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole != "Admin")
                {
                    return Forbid();
                }

                var statistics = await _activityLogService.GetActivityStatisticsAsync(userId, days);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving activity statistics", details = ex.Message });
            }
        }

        /// <summary>
        /// Create an activity log (Internal use - typically called by other services)
        /// </summary>
        /// <param name="dto">Activity log data</param>
        /// <returns>Created activity log</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ActivityLogDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ActivityLogDto>> CreateActivityLog([FromBody] CreateActivityLogDto dto)
        {
            try
            {
                // Extract IP and User-Agent if not provided
                if (string.IsNullOrEmpty(dto.IpAddress))
                {
                    dto.IpAddress = ActivityLogHelper.GetIpAddress(HttpContext);
                }

                if (string.IsNullOrEmpty(dto.UserAgent))
                {
                    dto.UserAgent = ActivityLogHelper.GetUserAgent(HttpContext);
                }

                var log = await _activityLogService.LogActivityAsync(dto);
                return CreatedAtAction(nameof(GetActivityLogById), new { id = log.ActivityLogId }, log);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the activity log", details = ex.Message });
            }
        }
    }
}
