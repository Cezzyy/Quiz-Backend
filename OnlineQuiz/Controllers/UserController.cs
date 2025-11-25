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

                // Log the CREATE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = createUserDto.CreatedBy ?? 0,
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
                try
                {
                    var currentUserId = JwtTokenGenerator.GetUserId(User) ?? 0;
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId,
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
                    try
                    {
                        var currentUserId = JwtTokenGenerator.GetUserId(User) ?? 0;
                        await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                        {
                            UserId = currentUserId,
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
    }
}
