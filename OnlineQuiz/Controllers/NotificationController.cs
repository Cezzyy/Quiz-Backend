using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using OnlineQuiz.Utilities;
using System.Security.Claims;

namespace OnlineQuiz.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly IActivityLogService _activityLogService;

        public NotificationController(INotificationService notificationService, IUserService userService, IActivityLogService activityLogService)
        {
            _notificationService = notificationService;
            _userService = userService;
            _activityLogService = activityLogService;
        }

        [HttpGet]
        public async Task<ActionResult<List<NotificationResponseDto>>> GetMyNotifications()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notifications = await _notificationService.GetNotificationsForUserAsync(userId);
            return Ok(notifications);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationResponseDto>> GetNotification(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notification = await _notificationService.GetNotificationByIdAsync(id, userId);

            if (notification == null)
            {
                return NotFound();
            }

            return Ok(notification);
        }

        [HttpPut("{id}/read")]
        public async Task<ActionResult<NotificationResponseDto>> MarkAsRead(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                var notification = await _notificationService.MarkAsReadAsync(id, userId);

                // Log activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = userId,
                        Action = ActivityLogConstants.Actions.UPDATE,
                        Entity = ActivityLogConstants.Entities.Notification,
                        EntityId = id,
                        Description = $"Marked notification as read: {notification.Title}",
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to log UPDATE notification activity: {ex.Message}");
                }

                return Ok(notification);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPut("read-all")]
        public async Task<ActionResult> MarkAllAsRead()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _notificationService.MarkAllAsReadAsync(userId);

            // Log activity
            try
            {
                await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                {
                    UserId = userId,
                    Action = ActivityLogConstants.Actions.UPDATE,
                    Entity = ActivityLogConstants.Entities.Notification,
                    EntityId = null,
                    Description = "Marked all notifications as read",
                    IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                    UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log UPDATE all notifications activity: {ex.Message}");
            }

            return Ok(new { message = "All notifications marked as read" });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNotification(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                var result = await _notificationService.DeleteNotificationAsync(id, userId);
                if (!result)
                {
                    return NotFound();
                }

                // Log activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = userId,
                        Action = ActivityLogConstants.Actions.DELETE,
                        Entity = ActivityLogConstants.Entities.Notification,
                        EntityId = id,
                        Description = $"Deleted notification ID: {id}",
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to log DELETE notification activity: {ex.Message}");
                }

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<NotificationResponseDto>> CreateNotification([FromBody] CreateNotificationDto createNotificationDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Validate notification type
            var validTypes = new[] {
        NotificationConstants.TypeQuiz,
        NotificationConstants.TypeCourse,
        NotificationConstants.TypeSystem,
        NotificationConstants.TypeReminder,
        NotificationConstants.TypeAnnouncement
    };

            if (!validTypes.Contains(createNotificationDto.Type))
            {
                return BadRequest($"Invalid notification type. Valid types are: {string.Join(", ", validTypes)}");
            }

            var notification = await _notificationService.CreateNotificationAsync(createNotificationDto, userId);

            // Log activity
            try
            {
                await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                {
                    UserId = userId,
                    Action = ActivityLogConstants.Actions.CREATE,
                    Entity = ActivityLogConstants.Entities.Notification,
                    EntityId = notification.NotificationId,
                    Description = $"Admin created notification: {notification.Title} (Type: {notification.Type})",
                    IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                    UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log CREATE notification activity: {ex.Message}");
            }

            return CreatedAtAction(nameof(GetNotification), new { id = notification.NotificationId }, notification);
        }
        [HttpDelete("bulk")]
        [EnableRateLimiting("bulk-operations")]
        public async Task<ActionResult> BulkDeleteNotifications([FromBody] BulkDeleteNotificationsDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                await _notificationService.BulkDeleteNotificationsAsync(dto.NotificationIds, userId);

                // Log activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = userId,
                        Action = ActivityLogConstants.Actions.DELETE,
                        Entity = ActivityLogConstants.Entities.Notification,
                        EntityId = null,
                        Description = $"Bulk deleted {dto.NotificationIds.Count} notifications",
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to log BULK DELETE notifications activity: {ex.Message}");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
