using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.Controllers;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using OnlineQuiz.Tests.Fakes;
using Xunit;

namespace OnlineQuiz.Tests.Controllers
{
    internal class FakeCourseServiceForEnrollment : ICourseService
    {
        public bool ThrowUnauthorized { get; set; }
        public bool ThrowArgument { get; set; }
        public bool ThrowInvalidOp { get; set; }
        public bool ReturnEmptyEnrollments { get; set; }
        public bool ReturnEmptyCourses { get; set; }
        public bool UnenrollReturnsTrue { get; set; } = true;

        public Task<EnrollmentResponseDto> EnrollStudentAsync(EnrollStudentDto dto)
        {
            if (ThrowUnauthorized) throw new UnauthorizedAccessException("Not the instructor");
            if (ThrowArgument) throw new ArgumentException("Student not found");
            if (ThrowInvalidOp) throw new InvalidOperationException("Already enrolled");
            return Task.FromResult(new EnrollmentResponseDto { EnrollmentId = 1, UserId = dto.StudentId, CourseId = dto.CourseId, CourseName = "Math", StudentName = "Alice", EnrolledAt = DateTime.UtcNow });
        }

        public Task<List<EnrollmentResponseDto>> GetCourseEnrollmentsAsync(int courseId, int teacherId)
        {
            if (ThrowUnauthorized) throw new UnauthorizedAccessException("Not the instructor");
            if (ReturnEmptyEnrollments) return Task.FromResult(new List<EnrollmentResponseDto>());
            return Task.FromResult(new List<EnrollmentResponseDto>
            {
                new EnrollmentResponseDto { EnrollmentId = 1, CourseId = courseId, UserId = 100, StudentName = "Alice" }
            });
        }

        public Task<List<CourseResponseDto>> GetCoursesForStudentAsync(int studentId)
        {
            if (ReturnEmptyCourses) return Task.FromResult(new List<CourseResponseDto>());
            return Task.FromResult(new List<CourseResponseDto>
            {
                new CourseResponseDto { CourseId = 1, Code = "M101", Name = "Math", InstructorId = 5 }
            });
        }

        public Task<bool> UnenrollStudentAsync(int courseId, int studentId, int teacherId)
        {
            if (ThrowUnauthorized) throw new UnauthorizedAccessException("Not the instructor");
            return Task.FromResult(UnenrollReturnsTrue);
        }

        public Task<int> BulkUnenrollStudentsAsync(BulkDeleteEnrollmentsDto dto, int teacherId)
        {
            if (ThrowUnauthorized) throw new UnauthorizedAccessException("Not the instructor");
            if (ThrowArgument) throw new ArgumentException("Invalid request");
            return Task.FromResult(2);
        }

        // Unused stubs
        public Task<CourseResponseDto> CreateCourseAsync(CreateCourseDto dto) => throw new NotImplementedException();
        public Task<CourseResponseDto?> GetCourseByIdAsync(int id) => throw new NotImplementedException();
        public Task<List<CourseResponseDto>> GetCoursesForTeacherAsync(int id) => throw new NotImplementedException();
        public Task<CourseResponseDto> UpdateCourseAsync(int id, UpdateCourseDto dto) => throw new NotImplementedException();
        public Task<bool> DeleteCourseAsync(int id) => throw new NotImplementedException();
        public Task<List<CourseResponseDto>> GetAllCoursesAsync() => throw new NotImplementedException();
        public Task<PagedResult<CourseResponseDto>> GetAllCoursesPagedAsync(PaginationParams p) => throw new NotImplementedException();
        public Task<PagedResult<CourseResponseDto>> GetCoursesForTeacherPagedAsync(int id, PaginationParams p) => throw new NotImplementedException();
        public Task<PagedResult<CourseResponseDto>> GetCoursesForStudentPagedAsync(int id, PaginationParams p) => throw new NotImplementedException();
        public Task<int> BulkDeleteCoursesAsync(List<int> ids) => throw new NotImplementedException();
        public Task<CourseResponseDto> ArchiveCourseAsync(int id, int by) => throw new NotImplementedException();
        public Task<CourseResponseDto> UnarchiveCourseAsync(int id) => throw new NotImplementedException();
        public Task<BulkArchiveResponseDto> BulkArchiveCoursesAsync(List<int> ids, int by) => throw new NotImplementedException();
        public Task<BulkArchiveResponseDto> BulkUnarchiveCoursesAsync(List<int> ids) => throw new NotImplementedException();
        public Task<List<CourseResponseDto>> GetArchivedCoursesAsync() => throw new NotImplementedException();
        public Task<PagedResult<CourseResponseDto>> GetArchivedCoursesPagedAsync(PaginationParams p) => throw new NotImplementedException();
        public Task<ArchiveStatisticsDto> GetCourseArchiveStatisticsAsync() => throw new NotImplementedException();
        public Task<List<ClassmateDto>> GetCourseClassmatesAsync(int courseId, int studentId) => throw new NotImplementedException();
    }

    [Collection("MapsterWarmup")]
    public class EnrollmentControllerTests
    {
        private static EnrollmentController CreateController(
            FakeCourseServiceForEnrollment? svc = null,
            int userId = 42)
        {
            var controller = new EnrollmentController(
                svc ?? new FakeCourseServiceForEnrollment(),
                new FakeActivityLogService());

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };
            return controller;
        }

        private static EnrollmentController CreateControllerNoAuth(FakeCourseServiceForEnrollment? svc = null)
        {
            var controller = new EnrollmentController(svc ?? new FakeCourseServiceForEnrollment(), new FakeActivityLogService());
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            return controller;
        }

        // ── EnrollStudent ─────────────────────────────────────────────────

        [Fact]
        public async Task EnrollStudent_ReturnsCreated_WhenValid()
        {
            var controller = CreateController(userId: 42);
            var dto = new EnrollStudentDto { StudentId = 100, CourseId = 5, EnrolledBy = 42 };
            var result = await controller.EnrollStudent(dto);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var enrollment = Assert.IsType<EnrollmentResponseDto>(created.Value);
            Assert.Equal(100, enrollment.UserId);
        }

        [Fact]
        public async Task EnrollStudent_ReturnsUnauthorized_WhenNoUserIdClaim()
        {
            var controller = CreateControllerNoAuth();
            var result = await controller.EnrollStudent(new EnrollStudentDto { StudentId = 1, CourseId = 1, EnrolledBy = 0 });
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task EnrollStudent_ReturnsUnauthorized_WhenServiceThrowsUnauthorized()
        {
            var controller = CreateController(new FakeCourseServiceForEnrollment { ThrowUnauthorized = true });
            var result = await controller.EnrollStudent(new EnrollStudentDto { StudentId = 1, CourseId = 1, EnrolledBy = 42 });
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task EnrollStudent_ReturnsBadRequest_WhenStudentNotFound()
        {
            var controller = CreateController(new FakeCourseServiceForEnrollment { ThrowArgument = true });
            var result = await controller.EnrollStudent(new EnrollStudentDto { StudentId = 999, CourseId = 1, EnrolledBy = 42 });
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task EnrollStudent_ReturnsBadRequest_WhenAlreadyEnrolled()
        {
            var controller = CreateController(new FakeCourseServiceForEnrollment { ThrowInvalidOp = true });
            var result = await controller.EnrollStudent(new EnrollStudentDto { StudentId = 1, CourseId = 1, EnrolledBy = 42 });
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        // ── GetCourseEnrollments ──────────────────────────────────────────

        [Fact]
        public async Task GetCourseEnrollments_ReturnsOk_WithList()
        {
            var controller = CreateController();
            var result = await controller.GetCourseEnrollments(courseId: 5, teacherId: 42);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<EnrollmentResponseDto>>(ok.Value);
            Assert.NotEmpty(list);
        }

        [Fact]
        public async Task GetCourseEnrollments_ReturnsNotFound_WhenEmpty()
        {
            var controller = CreateController(new FakeCourseServiceForEnrollment { ReturnEmptyEnrollments = true });
            var result = await controller.GetCourseEnrollments(courseId: 5, teacherId: 42);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetCourseEnrollments_ReturnsUnauthorized_WhenNotInstructor()
        {
            var controller = CreateController(new FakeCourseServiceForEnrollment { ThrowUnauthorized = true });
            var result = await controller.GetCourseEnrollments(courseId: 5, teacherId: 99);
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        // ── GetStudentEnrollments ─────────────────────────────────────────

        [Fact]
        public async Task GetStudentEnrollments_ReturnsOk_WithCourses()
        {
            var controller = CreateController();
            var result = await controller.GetStudentEnrollments(studentId: 42);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<CourseResponseDto>>(ok.Value);
            Assert.NotEmpty(list);
        }

        [Fact]
        public async Task GetStudentEnrollments_ReturnsNotFound_WhenNoCourses()
        {
            var controller = CreateController(new FakeCourseServiceForEnrollment { ReturnEmptyCourses = true });
            var result = await controller.GetStudentEnrollments(studentId: 42);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        // ── UnenrollStudent ───────────────────────────────────────────────

        [Fact]
        public async Task UnenrollStudent_ReturnsNoContent_WhenSuccess()
        {
            var controller = CreateController(userId: 42);
            var result = await controller.UnenrollStudent(courseId: 5, studentId: 100);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UnenrollStudent_ReturnsNotFound_WhenEnrollmentMissing()
        {
            var controller = CreateController(new FakeCourseServiceForEnrollment { UnenrollReturnsTrue = false }, userId: 42);
            var result = await controller.UnenrollStudent(courseId: 5, studentId: 100);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UnenrollStudent_ReturnsUnauthorized_WhenNoUserIdClaim()
        {
            var controller = CreateControllerNoAuth();
            var result = await controller.UnenrollStudent(courseId: 5, studentId: 100);
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ── BulkUnenrollStudents ──────────────────────────────────────────

        [Fact]
        public async Task BulkUnenrollStudents_ReturnsNoContent_WhenSuccess()
        {
            var controller = CreateController(userId: 42);
            var dto = new BulkDeleteEnrollmentsDto { CourseId = 5, StudentIds = new List<int> { 1, 2 } };
            var result = await controller.BulkUnenrollStudents(dto);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task BulkUnenrollStudents_ReturnsUnauthorized_WhenNoUserIdClaim()
        {
            var controller = CreateControllerNoAuth();
            var dto = new BulkDeleteEnrollmentsDto { CourseId = 5, StudentIds = new List<int> { 1 } };
            var result = await controller.BulkUnenrollStudents(dto);
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task BulkUnenrollStudents_ReturnsUnauthorized_WhenNotInstructor()
        {
            var controller = CreateController(new FakeCourseServiceForEnrollment { ThrowUnauthorized = true }, userId: 42);
            var dto = new BulkDeleteEnrollmentsDto { CourseId = 5, StudentIds = new List<int> { 1 } };
            var result = await controller.BulkUnenrollStudents(dto);
            Assert.IsType<UnauthorizedObjectResult>(result);
        }
    }
}
