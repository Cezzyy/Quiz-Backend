using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.DTOs;
using OnlineQuiz.Services;
using System.Security.Claims;

namespace OnlineQuiz.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet("admin/dashboard")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AdminDashboardDto>> GetAdminDashboard()
        {
            var dashboard = await _analyticsService.GetAdminDashboardAsync();
            return Ok(dashboard);
        }

        [HttpGet("teacher/dashboard")]
        [Authorize(Roles = "Teacher")]
        public async Task<ActionResult<TeacherDashboardDto>> GetTeacherDashboard()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var dashboard = await _analyticsService.GetTeacherDashboardAsync(userId);
            return Ok(dashboard);
        }

        [HttpGet("student/dashboard")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<StudentDashboardDto>> GetStudentDashboard()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var dashboard = await _analyticsService.GetStudentDashboardAsync(userId);
            return Ok(dashboard);
        }

        [HttpGet("course/{courseId}")]
        [Authorize(Roles = "Teacher,Admin")]
        public async Task<ActionResult<CourseAnalyticsDto>> GetCourseAnalytics(int courseId)
        {
            var dashboard = await _analyticsService.GetCourseAnalyticsAsync(courseId);
            return Ok(dashboard);
        }
    }
}
