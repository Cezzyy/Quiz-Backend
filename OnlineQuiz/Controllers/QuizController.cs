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
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _quizService;
        private readonly IActivityLogService _activityLogService;

        public QuizController(IQuizService quizService, IActivityLogService activityLogService)
        {
            _quizService = quizService;
            _activityLogService = activityLogService;
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

                // Log the CREATE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = createQuizDto.CreatedBy,
                        Action = ActivityLogConstants.Actions.CREATE,
                        Entity = ActivityLogConstants.Entities.Quiz,
                        EntityId = quiz.QuizId,
                        Description = $"Created quiz {quiz.Title} in course {quiz.CourseId}",
                        NewValues = new { quiz.QuizId, quiz.Title, quiz.CourseId, quiz.DueAt, quiz.TimeLimitMinutes },
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log CREATE activity: {logEx.Message}");
                }

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
        public async Task<ActionResult<List<QuizResponseDto>>> GetQuizzesForCourse(int courseId)
        {
            try
            {
                // Extract user info from JWT token
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Validate role claim exists
                var roleValidationError = this.ValidateUserRole(out string userRole);
                if (roleValidationError != null)
                {
                    return roleValidationError;
                }

                var isStudent = userRole == "Student";

                var quizzes = await _quizService.GetQuizzesForCourseAsync(courseId, currentUserId.Value, isStudent);
                if (!quizzes.Any())
                {
                    return Ok(new List<QuizResponseDto>());
                }
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
        /// Get quizzes for a course with pagination
        /// </summary>
        [HttpGet("course/{courseId}/paged")]
        [ProducesResponseType(typeof(PagedResult<QuizResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PagedResult<QuizResponseDto>>> GetQuizzesForCoursePaged(int courseId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Extract user info from JWT token
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Validate role claim exists
                var roleValidationError = this.ValidateUserRole(out string userRole);
                if (roleValidationError != null)
                {
                    return roleValidationError;
                }

                var isStudent = userRole == "Student";

                var paginationParams = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };
                var result = await _quizService.GetQuizzesForCoursePagedAsync(courseId, currentUserId.Value, isStudent, paginationParams);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving quizzes", details = ex.Message });
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
        /// Update a quiz (Teacher only - must be assigned to the course)
        /// </summary>
        /// <param name="quizId">Quiz ID</param>
        /// <param name="updateQuizDto">Updated quiz data</param>
        /// <returns>Updated quiz details</returns>
        [HttpPut("{quizId}")]
        [ProducesResponseType(typeof(QuizResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<QuizResponseDto>> UpdateQuiz(int quizId, [FromBody] UpdateQuizDto updateQuizDto)
        {
            try
            {
                // Extract user info from JWT token
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Get old quiz data before update
                var oldQuiz = await _quizService.GetQuizByIdAsync(quizId);

                var quiz = await _quizService.UpdateQuizAsync(quizId, updateQuizDto, currentUserId.Value);

                // Log activities
                try
                {
                    // Check for Publish/Unpublish
                    if (oldQuiz != null && updateQuizDto.IsPublished.HasValue && updateQuizDto.IsPublished.Value != oldQuiz.IsPublished)
                    {
                        var action = updateQuizDto.IsPublished.Value ? ActivityLogConstants.Actions.PUBLISH : ActivityLogConstants.Actions.UNPUBLISH;
                        await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                        {
                            UserId = currentUserId.Value,
                            Action = action,
                            Entity = ActivityLogConstants.Entities.Quiz,
                            EntityId = quizId,
                            Description = $"{(updateQuizDto.IsPublished.Value ? "Published" : "Unpublished")} quiz {quiz.Title}",
                            NewValues = new { IsPublished = updateQuizDto.IsPublished.Value },
                            IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                            UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                        });
                    }

                    // Log generic UPDATE
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId.Value,
                        Action = ActivityLogConstants.Actions.UPDATE,
                        Entity = ActivityLogConstants.Entities.Quiz,
                        EntityId = quizId,
                        Description = $"Updated quiz {quiz.Title}",
                        OldValues = oldQuiz != null ? new { oldQuiz.Title, oldQuiz.DueAt, oldQuiz.TimeLimitMinutes, oldQuiz.IsPublished } : null,
                        NewValues = new { quiz.Title, quiz.DueAt, quiz.TimeLimitMinutes, quiz.IsPublished },
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log UPDATE activity: {logEx.Message}");
                }

                return Ok(quiz);
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
        /// Delete a quiz (Course Instructor or Admin only)
        /// </summary>
        [HttpDelete("{quizId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> DeleteQuiz(int quizId)
        {
            try
            {
                // Extract user info from JWT token
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Get quiz before delete for logging
                var quiz = await _quizService.GetQuizByIdAsync(quizId);

                var result = await _quizService.DeleteQuizAsync(quizId, currentUserId.Value);
                if (!result)
                {
                    return NotFound(new { error = "Quiz not found" });
                }

                // Log the DELETE activity
                if (quiz != null)
                {
                    try
                    {
                        await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                        {
                            UserId = currentUserId.Value,
                            Action = ActivityLogConstants.Actions.DELETE,
                            Entity = ActivityLogConstants.Entities.Quiz,
                            EntityId = quizId,
                            Description = $"Deleted quiz {quiz.Title}",
                            OldValues = new { quiz.QuizId, quiz.Title, quiz.CourseId },
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
        /// Bulk delete quizzes (Course instructor or admin)
        /// </summary>
        [HttpDelete("bulk")]
        [EnableRateLimiting("bulk-operations")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> BulkDeleteQuizzes([FromBody] BulkDeleteQuizzesDto dto)
        {
            try
            {
                // Extract user info from JWT token
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var deletedCount = await _quizService.BulkDeleteQuizzesAsync(dto.QuizIds, currentUserId.Value);
                
                // Log the BULK_DELETE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId.Value,
                        Action = ActivityLogConstants.Actions.DELETE,
                        Entity = ActivityLogConstants.Entities.Quiz,
                        Description = $"Bulk deleted {deletedCount} quizzes",
                        NewValues = new { QuizIds = dto.QuizIds, DeletedCount = deletedCount },
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
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Archive a quiz (Instructor or Admin only)
        /// </summary>
        [HttpPost("{quizId}/archive")]
        [Authorize]
        [ProducesResponseType(typeof(QuizResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<QuizResponseDto>> ArchiveQuiz(int quizId)
        {
            try
            {
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var quiz = await _quizService.ArchiveQuizAsync(quizId, currentUserId.Value, currentUserId.Value);

                // Log the ARCHIVE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId.Value,
                        Action = ActivityLogConstants.Actions.UPDATE,
                        Entity = ActivityLogConstants.Entities.Quiz,
                        EntityId = quizId,
                        Description = $"Archived quiz {quiz.Title}",
                        OldValues = new { Status = "Active" },
                        NewValues = new { Status = "Archived", ArchivedAt = quiz.ArchivedAt, ArchivedBy = quiz.ArchivedBy },
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log ARCHIVE activity: {logEx.Message}");
                }

                return Ok(quiz);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while archiving quiz", details = ex.Message });
            }
        }

        /// <summary>
        /// Unarchive (restore) a quiz (Instructor or Admin only)
        /// </summary>
        [HttpPost("{quizId}/unarchive")]
        [Authorize]
        [ProducesResponseType(typeof(QuizResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<QuizResponseDto>> UnarchiveQuiz(int quizId)
        {
            try
            {
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var quiz = await _quizService.UnarchiveQuizAsync(quizId, currentUserId.Value);

                // Log the UNARCHIVE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId.Value,
                        Action = ActivityLogConstants.Actions.UPDATE,
                        Entity = ActivityLogConstants.Entities.Quiz,
                        EntityId = quizId,
                        Description = $"Unarchived quiz {quiz.Title}",
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

                return Ok(quiz);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while unarchiving quiz", details = ex.Message });
            }
        }

        /// <summary>
        /// Bulk archive quizzes (Instructor or Admin only)
        /// </summary>
        [HttpPost("bulk-archive")]
        [Authorize]
        [EnableRateLimiting("bulk-operations")]
        [ProducesResponseType(typeof(BulkArchiveResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BulkArchiveResponseDto>> BulkArchiveQuizzes([FromBody] BulkArchiveDto dto)
        {
            try
            {
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var result = await _quizService.BulkArchiveQuizzesAsync(dto.Ids, currentUserId.Value, currentUserId.Value);

                // Log the BULK_ARCHIVE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId.Value,
                        Action = ActivityLogConstants.Actions.UPDATE,
                        Entity = ActivityLogConstants.Entities.Quiz,
                        Description = $"Bulk archived {result.SuccessCount} quizzes",
                        NewValues = new { QuizIds = dto.Ids, SuccessCount = result.SuccessCount, FailureCount = result.FailureCount },
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
                return StatusCode(500, new { error = "An error occurred while bulk archiving quizzes", details = ex.Message });
            }
        }

        /// <summary>
        /// Bulk unarchive quizzes (Instructor or Admin only)
        /// </summary>
        [HttpPost("bulk-unarchive")]
        [Authorize]
        [EnableRateLimiting("bulk-operations")]
        [ProducesResponseType(typeof(BulkArchiveResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BulkArchiveResponseDto>> BulkUnarchiveQuizzes([FromBody] BulkUnarchiveDto dto)
        {
            try
            {
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var result = await _quizService.BulkUnarchiveQuizzesAsync(dto.Ids, currentUserId.Value);

                // Log the BULK_UNARCHIVE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId.Value,
                        Action = ActivityLogConstants.Actions.UPDATE,
                        Entity = ActivityLogConstants.Entities.Quiz,
                        Description = $"Bulk unarchived {result.SuccessCount} quizzes",
                        NewValues = new { QuizIds = dto.Ids, SuccessCount = result.SuccessCount, FailureCount = result.FailureCount },
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log BULK_UNARCHIVE activity: {logEx.Message}");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while bulk unarchiving quizzes", details = ex.Message });
            }
        }

        /// <summary>
        /// Get archived quizzes for a course (Instructor or Admin only)
        /// </summary>
        [HttpGet("course/{courseId}/archived")]
        [Authorize]
        [ProducesResponseType(typeof(List<QuizResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<QuizResponseDto>>> GetArchivedQuizzes(int courseId)
        {
            try
            {
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var quizzes = await _quizService.GetArchivedQuizzesAsync(courseId, currentUserId.Value);
                return Ok(quizzes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving archived quizzes", details = ex.Message });
            }
        }

        /// <summary>
        /// Get archived quizzes with pagination (Instructor or Admin only)
        /// </summary>
        [HttpGet("course/{courseId}/archived/paged")]
        [Authorize]
        [ProducesResponseType(typeof(PagedResult<QuizResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<QuizResponseDto>>> GetArchivedQuizzesPaged(int courseId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var paginationParams = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };
                var result = await _quizService.GetArchivedQuizzesPagedAsync(courseId, currentUserId.Value, paginationParams);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving archived quizzes", details = ex.Message });
            }
        }

        /// <summary>
        /// Get quiz archive statistics (Admin only)
        /// </summary>
        [HttpGet("archive-statistics")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ArchiveStatisticsDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ArchiveStatisticsDto>> GetQuizArchiveStatistics([FromQuery] int? courseId = null)
        {
            try
            {
                var statistics = await _quizService.GetQuizArchiveStatisticsAsync(courseId);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving archive statistics", details = ex.Message });
            }
        }
    }
}
