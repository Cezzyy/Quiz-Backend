using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.Controllers;
using OnlineQuiz.DTOs;
using OnlineQuiz.Services;
using Xunit;

namespace OnlineQuiz.Tests.Controllers
{
    internal class FakeAnalyticsService : IAnalyticsService
    {
        public Task<AdminDashboardDto> GetAdminDashboardAsync()
            => Task.FromResult(new AdminDashboardDto { TotalUsers = 50, TotalCourses = 10, TotalQuizzes = 30, ActiveUsers = 40 });

        public Task<TeacherDashboardDto> GetTeacherDashboardAsync(int teacherId)
            => Task.FromResult(new TeacherDashboardDto { TotalCourses = 3, TotalStudents = 60, TotalQuizzes = 12, PendingGrading = 5 });

        public Task<StudentDashboardDto> GetStudentDashboardAsync(int studentId)
            => Task.FromResult(new StudentDashboardDto { EnrolledCourses = 4, CompletedQuizzes = 8, AverageScore = 82.5 });

        public Task<CourseAnalyticsDto> GetCourseAnalyticsAsync(int courseId)
            => Task.FromResult(new CourseAnalyticsDto { CourseId = courseId, CourseName = "Math", TotalStudents = 25, TotalQuizzes = 5, AverageScore = 78.0 });
    }

    [Collection("MapsterWarmup")]
    public class AnalyticsControllerTests
    {
        private static AnalyticsController CreateController(string role = "Admin", int userId = 1)
        {
            var controller = new AnalyticsController(new FakeAnalyticsService());
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };
            return controller;
        }

        // ── GetAdminDashboard ─────────────────────────────────────────────

        [Fact]
        public async Task GetAdminDashboard_ReturnsOk_WithDashboardData()
        {
            var controller = CreateController(role: "Admin");
            var result = await controller.GetAdminDashboard();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dashboard = Assert.IsType<AdminDashboardDto>(ok.Value);
            Assert.Equal(50, dashboard.TotalUsers);
            Assert.Equal(10, dashboard.TotalCourses);
        }

        // ── GetTeacherDashboard ───────────────────────────────────────────

        [Fact]
        public async Task GetTeacherDashboard_ReturnsOk_WithTeacherData()
        {
            var controller = CreateController(role: "Teacher", userId: 5);
            var result = await controller.GetTeacherDashboard();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dashboard = Assert.IsType<TeacherDashboardDto>(ok.Value);
            Assert.Equal(3, dashboard.TotalCourses);
            Assert.Equal(5, dashboard.PendingGrading);
        }

        // ── GetStudentDashboard ───────────────────────────────────────────

        [Fact]
        public async Task GetStudentDashboard_ReturnsOk_WithStudentData()
        {
            var controller = CreateController(role: "Student", userId: 42);
            var result = await controller.GetStudentDashboard();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dashboard = Assert.IsType<StudentDashboardDto>(ok.Value);
            Assert.Equal(4, dashboard.EnrolledCourses);
            Assert.Equal(82.5, dashboard.AverageScore);
        }

        // ── GetCourseAnalytics ────────────────────────────────────────────

        [Fact]
        public async Task GetCourseAnalytics_ReturnsOk_WithCourseData()
        {
            var controller = CreateController(role: "Teacher", userId: 5);
            var result = await controller.GetCourseAnalytics(courseId: 7);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var analytics = Assert.IsType<CourseAnalyticsDto>(ok.Value);
            Assert.Equal(7, analytics.CourseId);
            Assert.Equal(25, analytics.TotalStudents);
            Assert.Equal(78.0, analytics.AverageScore);
        }
    }
}
