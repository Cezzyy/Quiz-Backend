using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;

namespace OnlineQuiz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public CourseController(ICourseService courseService)
        {
            _courseService = courseService;
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
                return CreatedAtAction(nameof(GetCoursesForTeacher), new { teacherId = course.InstructorId }, course);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get courses assigned to a teacher
        /// </summary>
        [HttpGet("teacher/{teacherId}")]
        [ProducesResponseType(typeof(List<CourseResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CourseResponseDto>>> GetCoursesForTeacher(int teacherId)
        {
            var courses = await _courseService.GetCoursesForTeacherAsync(teacherId);
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
            return Ok(courses);
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
    }
}
