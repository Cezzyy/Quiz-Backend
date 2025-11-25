using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;

namespace OnlineQuiz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnrollmentController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public EnrollmentController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        /// <summary>
        /// Enroll a student in a course (Teacher only - must be assigned to the course)
        /// </summary>
        /// <param name="enrollStudentDto">Enrollment data</param>
        /// <returns>Created enrollment details</returns>
        [HttpPost]
        [ProducesResponseType(typeof(EnrollmentResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<EnrollmentResponseDto>> EnrollStudent([FromBody] EnrollStudentDto enrollStudentDto)
        {
            try
            {
                // Validate against authenticated user identity
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) 
                               ?? User.FindFirst("id") 
                               ?? User.FindFirst("UserId");
                
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Override the EnrolledBy field to ensure it matches the authenticated user
                    enrollStudentDto.EnrolledBy = userId;
                }
                else
                {
                    // If we can't identify the user, we should probably fail or at least warn.
                    // For now, if no auth is present (dev mode?), we might skip, but the requirement is strict.
                    // Assuming auth is required for this endpoint:
                    // return Unauthorized(new { error = "User identity could not be verified" });
                    
                    // However, if the project is in a state where auth isn't fully wired, this might break testing.
                    // Given the prompt "Validate inputs against the authenticated user's identity", I will enforce it.
                     return Unauthorized(new { error = "User identity could not be verified" });
                }

                var enrollment = await _courseService.EnrollStudentAsync(enrollStudentDto);
                return CreatedAtAction(nameof(GetCourseEnrollments), new { courseId = enrollment.CourseId }, enrollment);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
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
                return StatusCode(500, new { error = "An error occurred while enrolling the student", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all enrollments for a specific course (Teacher only - must be assigned to the course)
        /// </summary>
        /// <param name="courseId">Course ID</param>
        /// <param name="teacherId">Teacher ID (for authorization)</param>
        /// <returns>List of enrollments</returns>
        [HttpGet("course/{courseId}")]
        [ProducesResponseType(typeof(List<EnrollmentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<EnrollmentResponseDto>>> GetCourseEnrollments(int courseId, [FromQuery] int teacherId)
        {
            try
            {
                var enrollments = await _courseService.GetCourseEnrollmentsAsync(courseId, teacherId);
                return Ok(enrollments);
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
                return StatusCode(500, new { error = "An error occurred while retrieving enrollments", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all courses a student is enrolled in
        /// </summary>
        /// <param name="studentId">Student ID</param>
        /// <returns>List of courses</returns>
        [HttpGet("student/{studentId}")]
        [ProducesResponseType(typeof(List<CourseResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CourseResponseDto>>> GetStudentEnrollments(int studentId)
        {
            try
            {
                var courses = await _courseService.GetCoursesForStudentAsync(studentId);
                return Ok(courses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving student enrollments", details = ex.Message });
            }
        }

        /// <summary>
        /// Remove a student from a course (Teacher only - must be assigned to the course)
        /// </summary>
        /// <param name="courseId">Course ID</param>
        /// <param name="studentId">Student ID</param>
        /// <param name="teacherId">Teacher ID (for authorization)</param>
        /// <returns>No content on success</returns>
        [HttpDelete("course/{courseId}/student/{studentId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UnenrollStudent(int courseId, int studentId, [FromQuery] int teacherId)
        {
            try
            {
                var result = await _courseService.UnenrollStudentAsync(courseId, studentId, teacherId);
                if (!result)
                {
                    return NotFound(new { error = "Enrollment not found" });
                }
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while unenrolling the student", details = ex.Message });
            }
        }
    }
}
