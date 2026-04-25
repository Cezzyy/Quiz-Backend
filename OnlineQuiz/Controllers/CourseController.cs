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
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly IActivityLogService _activityLogService;

        public CourseController(ICourseService courseService, IActivityLogService activityLogService)
        {
            _courseService = courseService;
            _activityLogService = activityLogService;
        }

        /// <summary>
        /// Create a new course (Admin only)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CourseResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CourseResponseDto>> CreateCourse([FromBody] CreateCourseDto createCourseDto)
        {
            try
            {
                var course = await _courseService.CreateCourseAsync(createCourseDto);

                // Log the CREATE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = createCourseDto.CreatedBy,
                        Action = ActivityLogConstants.Actions.CREATE,
                        Entity = ActivityLogConstants.Entities.Course,
                        EntityId = course.CourseId,
                        Description = $"Created course {course.Code} - {course.Name}",
                        NewValues = new { course.CourseId, course.Code, course.Name, course.InstructorId, course.Status },
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log CREATE activity: {logEx.Message}");
                }

                return CreatedAtAction(nameof(GetCoursesForTeacher), new { teacherId = course.InstructorId }, course);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get course by ID
        /// </summary>
        [HttpGet("{courseId}")]
        [ProducesResponseType(typeof(CourseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CourseResponseDto>> GetCourseById(int courseId)
        {
            var course = await _courseService.GetCourseByIdAsync(courseId);
            if (course == null)
            {
                return NotFound(new { error = "Course not found" });
            }
            return Ok(course);
        }

        /// <summary>
        /// Get all courses
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<CourseResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CourseResponseDto>>> GetAllCourses()
        {
            var courses = await _courseService.GetAllCoursesAsync();
            if (!courses.Any())
            {
                return Ok(new List<CourseResponseDto>());
            }
            return Ok(courses);
        }

        /// <summary>
        /// Get courses assigned to a teacher
        /// </summary>
        [HttpGet("teacher/{teacherId}")]
        [ProducesResponseType(typeof(List<CourseResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CourseResponseDto>>> GetCoursesForTeacher(int teacherId)
        {
            var courses = await _courseService.GetCoursesForTeacherAsync(teacherId);
            if (!courses.Any())
            {
                return Ok(new List<CourseResponseDto>());
            }
            return Ok(courses);
        }

        /// <summary>
        /// Get courses a student is enrolled in
        /// </summary>
        [HttpGet("student/{studentId}")]
        [ProducesResponseType(typeof(List<CourseResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CourseResponseDto>>> GetCoursesForStudent(int studentId)
        {
            var courses = await _courseService.GetCoursesForStudentAsync(studentId);
            if (!courses.Any())
            {
                return Ok(new List<CourseResponseDto>());
            }
            return Ok(courses);
        }

        /// <summary>
        /// Get all courses with pagination
        /// </summary>
        [HttpGet("paged")]
        [ProducesResponseType(typeof(PagedResult<CourseResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<CourseResponseDto>>> GetAllCoursesPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var paginationParams = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };
                var result = await _courseService.GetAllCoursesPagedAsync(paginationParams);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving courses", details = ex.Message });
            }
        }

        /// <summary>
        /// Get courses for a teacher with pagination
        /// </summary>
        [HttpGet("teacher/{teacherId}/paged")]
        [ProducesResponseType(typeof(PagedResult<CourseResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<CourseResponseDto>>> GetCoursesForTeacherPaged(int teacherId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var paginationParams = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };
                var result = await _courseService.GetCoursesForTeacherPagedAsync(teacherId, paginationParams);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving courses", details = ex.Message });
            }
        }

        /// <summary>
        /// Get courses for a student with pagination
        /// </summary>
        [HttpGet("student/{studentId}/paged")]
        [ProducesResponseType(typeof(PagedResult<CourseResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<CourseResponseDto>>> GetCoursesForStudentPaged(int studentId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var paginationParams = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };
                var result = await _courseService.GetCoursesForStudentPagedAsync(studentId, paginationParams);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving courses", details = ex.Message });
            }
        }

        /// <summary>
        /// Enroll a student in a course (Teacher only)
        /// </summary>
        [HttpPost("enroll")]
        [ProducesResponseType(typeof(EnrollmentResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<EnrollmentResponseDto>> EnrollStudent([FromBody] EnrollStudentDto enrollStudentDto)
        {
            try
            {
                var enrollment = await _courseService.EnrollStudentAsync(enrollStudentDto);
                return Ok(enrollment);
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
        /// Get all enrollments for a course (Teacher only)
        /// </summary>
        [HttpGet("{courseId}/enrollments")]
        [ProducesResponseType(typeof(List<EnrollmentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<EnrollmentResponseDto>>> GetCourseEnrollments(int courseId, [FromQuery] int teacherId)
        {
            try
            {
                var enrollments = await _courseService.GetCourseEnrollmentsAsync(courseId, teacherId);
                if (!enrollments.Any())
                {
                    return Ok(new List<EnrollmentResponseDto>());
                }
                return Ok(enrollments);
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
        /// Get classmates for a course (Student only - must be enrolled)
        /// </summary>
        [HttpGet("{courseId}/classmates")]
        [ProducesResponseType(typeof(List<ClassmateDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<ClassmateDto>>> GetCourseClassmates(int courseId)
        {
            try
            {
                if (!this.TryGetAuthenticatedUserId(out int userId))
                {
                    return Unauthorized(new { error = "User identity could not be verified" });
                }

                var classmates = await _courseService.GetCourseClassmatesAsync(courseId, userId);
                return Ok(classmates);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving classmates", details = ex.Message });
            }
        }

        /// <summary>
        /// Update a course (Admin only - can reassign instructor)
        /// </summary>
        [HttpPut("{courseId}")]
        [ProducesResponseType(typeof(CourseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CourseResponseDto>> UpdateCourse(int courseId, [FromBody] UpdateCourseDto updateCourseDto)
        {
            try
            {
                // Note: We don't have easy access to old course data here without an extra query
                // For performance, we might skip old values or fetch if critical
                
                var course = await _courseService.UpdateCourseAsync(courseId, updateCourseDto);

                // Log the UPDATE activity
                try
                {
                    var currentUserId = JwtTokenGenerator.GetUserId(User) ?? 0;
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId,
                        Action = ActivityLogConstants.Actions.UPDATE,
                        Entity = ActivityLogConstants.Entities.Course,
                        EntityId = courseId,
                        Description = $"Updated course {course.Code} - {course.Name}",
                        NewValues = new { course.Code, course.Name, course.InstructorId, course.Status },
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log UPDATE activity: {logEx.Message}");
                }

                return Ok(course);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a course (Admin only)
        /// </summary>
        [HttpDelete("{courseId}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> DeleteCourse(int courseId)
        {
            try
            {
                var result = await _courseService.DeleteCourseAsync(courseId);
                if (!result)
                {
                    return NotFound(new { error = "Course not found" });
                }

                // Log the DELETE activity
                try
                {
                    var currentUserId = JwtTokenGenerator.GetUserId(User) ?? 0;
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId,
                        Action = ActivityLogConstants.Actions.DELETE,
                        Entity = ActivityLogConstants.Entities.Course,
                        EntityId = courseId,
                        Description = $"Deleted course {courseId}",
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log DELETE activity: {logEx.Message}");
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                // This handles cases where course has dependencies (quizzes or enrollments)
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                // Log unexpected errors for debugging
                Console.WriteLine($"Error deleting course {courseId}: {ex.Message}");
                return StatusCode(500, new { error = "An error occurred while deleting the course. The course may have dependencies that prevent deletion." });
            }
        }

        /// <summary>
        /// Bulk delete courses (Admin only)
        /// </summary>
        [HttpDelete("bulk")]
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("bulk-operations")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> BulkDeleteCourses([FromBody] BulkDeleteCoursesDto dto)
        {
            try
            {
                var deletedCount = await _courseService.BulkDeleteCoursesAsync(dto.CourseIds);
                
                // Log the BULK_DELETE activity
                try
                {
                    var currentUserId = JwtTokenGenerator.GetUserId(User) ?? 0;
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId,
                        Action = ActivityLogConstants.Actions.DELETE,
                        Entity = ActivityLogConstants.Entities.Course,
                        Description = $"Bulk deleted {deletedCount} courses",
                        NewValues = new { CourseIds = dto.CourseIds, DeletedCount = deletedCount },
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
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Archive a course (Admin only)
        /// </summary>
        [HttpPost("{courseId}/archive")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(CourseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CourseResponseDto>> ArchiveCourse(int courseId)
        {
            try
            {
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var course = await _courseService.ArchiveCourseAsync(courseId, currentUserId.Value);

                // Log the ARCHIVE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId.Value,
                        Action = ActivityLogConstants.Actions.UPDATE,
                        Entity = ActivityLogConstants.Entities.Course,
                        EntityId = courseId,
                        Description = $"Archived course {course.Code} - {course.Name}",
                        OldValues = new { Status = "Active" },
                        NewValues = new { Status = "Archived", ArchivedAt = course.ArchivedAt, ArchivedBy = course.ArchivedBy },
                        IpAddress = ActivityLogHelper.GetIpAddress(HttpContext),
                        UserAgent = ActivityLogHelper.GetUserAgent(HttpContext)
                    });
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"Failed to log ARCHIVE activity: {logEx.Message}");
                }

                return Ok(course);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while archiving course", details = ex.Message });
            }
        }

        /// <summary>
        /// Unarchive (restore) a course (Admin only)
        /// </summary>
        [HttpPost("{courseId}/unarchive")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(CourseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CourseResponseDto>> UnarchiveCourse(int courseId)
        {
            try
            {
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var course = await _courseService.UnarchiveCourseAsync(courseId);

                // Log the UNARCHIVE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId.Value,
                        Action = ActivityLogConstants.Actions.UPDATE,
                        Entity = ActivityLogConstants.Entities.Course,
                        EntityId = courseId,
                        Description = $"Unarchived course {course.Code} - {course.Name}",
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

                return Ok(course);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while unarchiving course", details = ex.Message });
            }
        }

        /// <summary>
        /// Bulk archive courses (Admin only)
        /// </summary>
        [HttpPost("bulk-archive")]
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("bulk-operations")]
        [ProducesResponseType(typeof(BulkArchiveResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BulkArchiveResponseDto>> BulkArchiveCourses([FromBody] BulkArchiveDto dto)
        {
            try
            {
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var result = await _courseService.BulkArchiveCoursesAsync(dto.Ids, currentUserId.Value);

                // Log the BULK_ARCHIVE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId.Value,
                        Action = ActivityLogConstants.Actions.UPDATE,
                        Entity = ActivityLogConstants.Entities.Course,
                        Description = $"Bulk archived {result.SuccessCount} courses",
                        NewValues = new { CourseIds = dto.Ids, SuccessCount = result.SuccessCount, FailureCount = result.FailureCount },
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
                return StatusCode(500, new { error = "An error occurred while bulk archiving courses", details = ex.Message });
            }
        }

        /// <summary>
        /// Bulk unarchive courses (Admin only)
        /// </summary>
        [HttpPost("bulk-unarchive")]
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("bulk-operations")]
        [ProducesResponseType(typeof(BulkArchiveResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BulkArchiveResponseDto>> BulkUnarchiveCourses([FromBody] BulkUnarchiveDto dto)
        {
            try
            {
                var currentUserId = JwtTokenGenerator.GetUserId(User);
                if (!currentUserId.HasValue || currentUserId.Value == 0)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var result = await _courseService.BulkUnarchiveCoursesAsync(dto.Ids);

                // Log the BULK_UNARCHIVE activity
                try
                {
                    await _activityLogService.LogActivityAsync(new CreateActivityLogDto
                    {
                        UserId = currentUserId.Value,
                        Action = ActivityLogConstants.Actions.UPDATE,
                        Entity = ActivityLogConstants.Entities.Course,
                        Description = $"Bulk unarchived {result.SuccessCount} courses",
                        NewValues = new { CourseIds = dto.Ids, SuccessCount = result.SuccessCount, FailureCount = result.FailureCount },
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
                return StatusCode(500, new { error = "An error occurred while bulk unarchiving courses", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all archived courses (Admin only)
        /// </summary>
        [HttpGet("archived")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(List<CourseResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CourseResponseDto>>> GetArchivedCourses()
        {
            try
            {
                var courses = await _courseService.GetArchivedCoursesAsync();
                return Ok(courses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving archived courses", details = ex.Message });
            }
        }

        /// <summary>
        /// Get archived courses with pagination (Admin only)
        /// </summary>
        [HttpGet("archived/paged")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResult<CourseResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<CourseResponseDto>>> GetArchivedCoursesPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var paginationParams = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };
                var result = await _courseService.GetArchivedCoursesPagedAsync(paginationParams);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving archived courses", details = ex.Message });
            }
        }

        /// <summary>
        /// Get course archive statistics (Admin only)
        /// </summary>
        [HttpGet("archive-statistics")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ArchiveStatisticsDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ArchiveStatisticsDto>> GetCourseArchiveStatistics()
        {
            try
            {
                var statistics = await _courseService.GetCourseArchiveStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving archive statistics", details = ex.Message });
            }
        }
    }
}
