using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using OnlineQuiz.Utilities;

namespace OnlineQuiz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IActivityLogService _activityLogService;

        public UserController(IUserService userService, IActivityLogService activityLogService)
        {
            _userService = userService;
            _activityLogService = activityLogService;
        }

        /// <summary>
        /// Create a new user (Admin, Teacher, or Student)
        /// </summary>
        /// <param name="createUserDto">User creation data</param>
        /// <returns>Created user details</returns>
        [HttpPost]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                var user = await _userService.CreateUserAsync(createUserDto);

                // Log the CREATE activity using authenticated user
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (currentUserId.HasValue && currentUserId.Value > 0)
                {
                    try
                    {
                        await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                        {
                            UserId = currentUserId.Value,
                            Action = ActivityLogConstants.Actions.CREATE,
                            Entity = ActivityLogConstants.Entities.User,
                            EntityId = user.UserId,
                            Description = $"Created user {user.Email} with role {user.RoleName}",
                            NewValues = new { user.UserId, user.Email, user.FullName, user.RoleName },
                            IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                            UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                        });
                    }
                    catch (Exception logEx)
                    {
                        Console.WriteLine($"Failed to log CREATE activity: {logEx.Message}");
                    }
                }

                return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, user);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the user", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all users
        /// </summary>
        /// <returns>List of all users</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<UserResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UserResponseDto>>> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                if (!users.Any())
                {
                    return NotFound(new { error = "No users found" });
                }
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving users", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all users with pagination
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10, max: 100)</param>
        /// <returns>Paginated list of users</returns>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResult<UserResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<UserResponseDto>>> GetAllUsersPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var paginationParams = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };
                var result = await _userService.GetAllUsersPagedAsync(paginationParams);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving users", details = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserResponseDto>> GetUserById(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { error = $"User with ID {id} not found" });
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the user", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="updateUserDto">Updated user data</param>
        /// <returns>Updated user details</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserResponseDto>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                // Get old user data before update
                var oldUser = await _userService.GetUserByIdAsync(id);

                var user = await _userService.UpdateUserAsync(id, updateUserDto);

                // Log the UPDATE activity
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (currentUserId.HasValue && currentUserId.Value > 0)
                {
                    try
                    {
                        await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                        {
                            UserId = currentUserId.Value,
                            Action = ActivityLogConstants.Actions.UPDATE,
                            Entity = ActivityLogConstants.Entities.User,
                            EntityId = id,
                            Description = $"Updated user {user.Email}",
                            OldValues = oldUser != null ? new { oldUser.Email, oldUser.FullName, oldUser.Status } : null,
                            NewValues = new { user.Email, user.FullName, user.Status },
                            IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                            UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                        });
                    }
                    catch (Exception logEx)
                    {
                        Console.WriteLine($"Failed to log UPDATE activity: {logEx.Message}");
                    }
                }

                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the user", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                // Get user data before deletion
                var user = await _userService.GetUserByIdAsync(id);

                await _userService.DeleteUserAsync(id);

                // Log the DELETE activity
                if (user != null)
                {
                    var currentUserId = JwtTokenGenerator.GetUserId(User);
                    if (currentUserId.HasValue && currentUserId.Value > 0)
                    {
                        try
                        {
                            await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                            {
                                UserId = currentUserId.Value,
                            Action = ActivityLogConstants.Actions.DELETE,
                            Entity = ActivityLogConstants.Entities.User,
                            EntityId = id,
                            Description = $"Deleted user {user.Email}",
                            OldValues = new { user.UserId, user.Email, user.FullName, user.RoleName },
                            IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                            UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                        });
                    }
                        catch (Exception logEx)
                        {
                            Console.WriteLine($"Failed to log DELETE activity: {logEx.Message}");
                        }
                    }
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the user", details = ex.Message });
            }
        }

        /// <summary>
        /// Bulk delete users (Admin only)
        /// </summary>
        [HttpDelete("bulk")]
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("bulk-operations")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> BulkDeleteUsers([FromBody] BulkDeleteUsersDto dto)
        {
            try
            {
                var deletedCount = await _userService.BulkDeleteAsync(dto.UserIds);
                
                // Log the BULK_DELETE activity
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (currentUserId.HasValue && currentUserId.Value > 0)
                {
                    try
                    {
                        await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                        {
                            UserId = currentUserId.Value,
                            Action = ActivityLogConstants.Actions.DELETE,
                            Entity = ActivityLogConstants.Entities.User,
                            Description = $"Bulk deleted {deletedCount} users",
                            NewValues = new { UserIds = dto.UserIds, DeletedCount = deletedCount },
                            IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                            UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                        });
                    }
                    catch (Exception logEx)
                    {
                        Console.WriteLine($"Failed to log BULK_DELETE activity: {logEx.Message}");
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while bulk deleting users", details = ex.Message });
            }
        }

        /// <summary>
        /// Bulk import users from Excel file (Admin only)
        /// </summary>
        /// <param name="file">Excel file containing user data</param>
        /// <returns>Import results with success/failure details</returns>
        [HttpPost("bulk-import")]
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("file-operations")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(BulkUserImportResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BulkUserImportResultDto>> BulkImportUsers(IFormFile file)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file uploaded" });
                }

                // Validate file extension
                var allowedExtensions = new[] { ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { error = "Invalid file format. Only .xlsx and .xls files are allowed" });
                }

                // Get current user ID
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Process the file
                using var stream = file.OpenReadStream();
                var result = await _userService.BulkCreateUsersFromExcelAsync(stream, file.FileName, currentUserId.Value);

                // Log the BULK_IMPORT activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId.Value,
                        Action = ActivityLogConstants.Actions.IMPORT,
                        Entity = ActivityLogConstants.Entities.User,
                        EntityId = result.LogId,
                        Description = $"Bulk imported users from {file.FileName}: {result.SuccessCount} succeeded, {result.FailureCount} failed",
                        NewValues = new { 
                            FileName = file.FileName, 
                            TotalRows = result.TotalRows,
                            SuccessCount = result.SuccessCount, 
                            FailureCount = result.FailureCount,
                            LogId = result.LogId
                        },
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log BULK_IMPORT activity: {logEx.Message}");
                }

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while importing users", details = ex.Message });
            }
        }

        /// <summary>
        /// Reset user password (Admin only)
        /// </summary>
        [HttpPut("{id}/reset-password")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                await _userService.ResetPasswordAsync(id, resetPasswordDto.NewPassword);

                // Log activity
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (currentUserId.HasValue && currentUserId.Value > 0)
                {
                    try
                    {
                        await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                        {
                            UserId = currentUserId.Value,
                            Action = ActivityLogConstants.Actions.UPDATE,
                            Entity = ActivityLogConstants.Entities.User,
                            EntityId = id,
                            Description = $"Reset password for user {id}",
                            IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                            UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                        });
                    }
                    catch (Exception logEx)
                    {
                        Console.WriteLine($"Failed to log password reset: {logEx.Message}");
                    }
                }

                return Ok(new { message = "Password reset successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while resetting password", details = ex.Message });
            }
        }

        /// <summary>
        /// Archive a user (Admin only)
        /// </summary>
        [HttpPost("{id}/archive")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserResponseDto>> ArchiveUser(int id)
        {
            try
            {
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var user = await _userService.ArchiveUserAsync(id, currentUserId.Value);

                // Log the ARCHIVE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId.Value,
                        Action = ActivityLogConstants.Actions.UPDATE,
                        Entity = ActivityLogConstants.Entities.User,
                        EntityId = id,
                        Description = $"Archived user {user.Email}",
                        OldValues = new { Status = "Active" },
                        NewValues = new { Status = "Archived", ArchivedAt = user.ArchivedAt, ArchivedBy = user.ArchivedBy },
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log ARCHIVE activity: {logEx.Message}");
                }

                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while archiving user", details = ex.Message });
            }
        }

        /// <summary>
        /// Unarchive (restore) a user (Admin only)
        /// </summary>
        [HttpPost("{id}/unarchive")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserResponseDto>> UnarchiveUser(int id)
        {
            try
            {
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var user = await _userService.UnarchiveUserAsync(id);

                // Log the UNARCHIVE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId.Value,
                        Action = ActivityLogConstants.Actions.UPDATE,
                        Entity = ActivityLogConstants.Entities.User,
                        EntityId = id,
                        Description = $"Unarchived user {user.Email}",
                        OldValues = new { Status = "Archived" },
                        NewValues = new { Status = "Active" },
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log UNARCHIVE activity: {logEx.Message}");
                }

                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while unarchiving user", details = ex.Message });
            }
        }

        /// <summary>
        /// Bulk archive users (Admin only)
        /// </summary>
        [HttpPost("bulk-archive")]
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("bulk-operations")]
        [ProducesResponseType(typeof(BulkArchiveResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BulkArchiveResponseDto>> BulkArchiveUsers([FromBody] BulkArchiveDto dto)
        {
            try
            {
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var result = await _userService.BulkArchiveUsersAsync(dto.Ids, currentUserId.Value);

                // Log the BULK_ARCHIVE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId.Value,
                        Action = ActivityLogConstants.Actions.UPDATE,
                        Entity = ActivityLogConstants.Entities.User,
                        Description = $"Bulk archived {result.SuccessCount} users",
                        NewValues = new { UserIds = dto.Ids, SuccessCount = result.SuccessCount, FailureCount = result.FailureCount },
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log BULK_ARCHIVE activity: {logEx.Message}");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while bulk archiving users", details = ex.Message });
            }
        }

        /// <summary>
        /// Bulk unarchive users (Admin only)
        /// </summary>
        [HttpPost("bulk-unarchive")]
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("bulk-operations")]
        [ProducesResponseType(typeof(BulkArchiveResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BulkArchiveResponseDto>> BulkUnarchiveUsers([FromBody] BulkUnarchiveDto dto)
        {
            try
            {
                var result = await _userService.BulkUnarchiveUsersAsync(dto.Ids);

                // Log the BULK_UNARCHIVE activity
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (currentUserId.HasValue && currentUserId.Value > 0)
                {
                    try
                    {
                        await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                        {
                            UserId = currentUserId.Value,
                            Action = ActivityLogConstants.Actions.UPDATE,
                            Entity = ActivityLogConstants.Entities.User,
                            Description = $"Bulk unarchived {result.SuccessCount} users",
                            NewValues = new { UserIds = dto.Ids, SuccessCount = result.SuccessCount, FailureCount = result.FailureCount },
                            IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                            UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                        });
                    }
                    catch (Exception logEx)
                    {
                        Console.WriteLine($"Failed to log BULK_UNARCHIVE activity: {logEx.Message}");
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while bulk unarchiving users", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all archived users (Admin only)
        /// </summary>
        [HttpGet("archived")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(List<UserResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UserResponseDto>>> GetArchivedUsers()
        {
            try
            {
                var users = await _userService.GetArchivedUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving archived users", details = ex.Message });
            }
        }

        /// <summary>
        /// Get archived users with pagination (Admin only)
        /// </summary>
        [HttpGet("archived/paged")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResult<UserResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<UserResponseDto>>> GetArchivedUsersPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var paginationParams = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };
                var result = await _userService.GetArchivedUsersPagedAsync(paginationParams);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving archived users", details = ex.Message });
            }
        }

        /// <summary>
        /// Get user archive statistics (Admin only)
        /// </summary>
        [HttpGet("archive-statistics")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ArchiveStatisticsDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ArchiveStatisticsDto>> GetUserArchiveStatistics()
        {
            try
            {
                var statistics = await _userService.GetUserArchiveStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving archive statistics", details = ex.Message });
            }
        }
    }
}
