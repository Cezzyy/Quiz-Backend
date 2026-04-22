using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.Controllers;
using OnlineQuiz.DTOs;
using OnlineQuiz.IServices;
using System.Security.Claims;
using Xunit;

namespace OnlineQuiz.Tests.Controllers
{
    internal class FakeActivityLogServiceCourse : IActivityLogService
    {
        public Task<ActivityLogDto> LogActivityAsync(CreateActivityLogDto dto)
            => Task.FromResult(new ActivityLogDto { ActivityLogId = 1, UserId = dto.UserId, Action = dto.Action, Entity = dto.Entity, CreatedAt = DateTime.UtcNow });
        public Task<List<ActivityLogDto>> GetActivityLogsAsync(ActivityLogFilterDto filter) => Task.FromResult(new List<ActivityLogDto>());
        public Task<List<ActivityLogDto>> GetUserActivityLogsAsync(int userId, int? days = null) => Task.FromResult(new List<ActivityLogDto>());
        public Task<ActivityLogStatisticsDto> GetActivityStatisticsAsync(int? userId = null, int? days = 30) => Task.FromResult(new ActivityLogStatisticsDto());
        public Task<ActivityLogDto?> GetActivityLogByIdAsync(long activityLogId) => Task.FromResult<ActivityLogDto?>(null);
    }

    internal class FakeCourseService : ICourseService
    {
        public bool ThrowUnauthorized { get; set; }
        public bool ReturnEmptyEnrollments { get; set; }

        public Task<CourseResponseDto> CreateCourseAsync(CreateCourseDto createCourseDto)
            => Task.FromResult(new CourseResponseDto { CourseId = 1, Code = createCourseDto.Code, Name = createCourseDto.Name, InstructorId = createCourseDto.InstructorId, Status = "Active" });
        public Task<CourseResponseDto?> GetCourseByIdAsync(int courseId) => Task.FromResult<CourseResponseDto?>(new CourseResponseDto { CourseId = courseId, Code = "C101", Name = "Intro", InstructorId = 2 });
        public Task<List<CourseResponseDto>> GetCoursesForTeacherAsync(int teacherId) => Task.FromResult(new List<CourseResponseDto>());
        public Task<List<CourseResponseDto>> GetCoursesForStudentAsync(int studentId) => Task.FromResult(new List<CourseResponseDto>());

        public Task<EnrollmentResponseDto> EnrollStudentAsync(EnrollStudentDto enrollStudentDto)
        {
            if (ThrowUnauthorized) throw new UnauthorizedAccessException("Not allowed to enroll");
            return Task.FromResult(new EnrollmentResponseDto
            {
                EnrollmentId = 10,
                UserId = enrollStudentDto.StudentId,
                CourseId = enrollStudentDto.CourseId,
                CourseName = "Algorithms",
                StudentName = "Alan Turing",
                EnrolledAt = DateTime.UtcNow,
                Section = enrollStudentDto.Section
            });
        }

        public Task<List<EnrollmentResponseDto>> GetCourseEnrollmentsAsync(int courseId, int teacherId)
        {
            if (ThrowUnauthorized) throw new UnauthorizedAccessException("Not allowed to list enrollments");
            if (ReturnEmptyEnrollments) return Task.FromResult(new List<EnrollmentResponseDto>());
            return Task.FromResult(new List<EnrollmentResponseDto>
            {
                new EnrollmentResponseDto { EnrollmentId = 1, CourseId = courseId, UserId = 100, StudentName = "Ada" }
            });
        }

        public Task<bool> UnenrollStudentAsync(int courseId, int studentId, int teacherId) => Task.FromResult(true);
        public Task<CourseResponseDto> UpdateCourseAsync(int courseId, UpdateCourseDto updateCourseDto) => Task.FromResult(new CourseResponseDto { CourseId = courseId, Code = "C201", Name = updateCourseDto.Name ?? "Updated", InstructorId = 2, Status = "Active" });
        public Task<bool> DeleteCourseAsync(int courseId) => Task.FromResult(true);
        public Task<List<CourseResponseDto>> GetAllCoursesAsync() => Task.FromResult(new List<CourseResponseDto>());
        public Task<PagedResult<CourseResponseDto>> GetAllCoursesPagedAsync(PaginationParams paginationParams) => Task.FromResult(new PagedResult<CourseResponseDto> { Items = new List<CourseResponseDto>(), TotalCount = 0, PageNumber = paginationParams.PageNumber, PageSize = paginationParams.PageSize });
        public Task<PagedResult<CourseResponseDto>> GetCoursesForTeacherPagedAsync(int teacherId, PaginationParams paginationParams) => Task.FromResult(new PagedResult<CourseResponseDto> { Items = new List<CourseResponseDto>(), TotalCount = 0, PageNumber = paginationParams.PageNumber, PageSize = paginationParams.PageSize });
        public Task<PagedResult<CourseResponseDto>> GetCoursesForStudentPagedAsync(int studentId, PaginationParams paginationParams) => Task.FromResult(new PagedResult<CourseResponseDto> { Items = new List<CourseResponseDto>(), TotalCount = 0, PageNumber = paginationParams.PageNumber, PageSize = paginationParams.PageSize });
        public Task<int> BulkDeleteCoursesAsync(List<int> courseIds) => Task.FromResult(courseIds.Count);
        public Task<int> BulkUnenrollStudentsAsync(BulkDeleteEnrollmentsDto dto, int teacherId) => Task.FromResult(0);
        // Archive stubs
        public Task<CourseResponseDto> ArchiveCourseAsync(int courseId, int archivedBy)
            => Task.FromResult(new CourseResponseDto { CourseId = courseId, Code = "C101", Name = "Course", Status = "Archived" });
        public Task<CourseResponseDto> UnarchiveCourseAsync(int courseId)
            => Task.FromResult(new CourseResponseDto { CourseId = courseId, Code = "C101", Name = "Course", Status = "Active" });
        public Task<BulkArchiveResponseDto> BulkArchiveCoursesAsync(List<int> courseIds, int archivedBy)
            => Task.FromResult(new BulkArchiveResponseDto { TotalRequested = courseIds.Count, SuccessCount = courseIds.Count });
        public Task<BulkArchiveResponseDto> BulkUnarchiveCoursesAsync(List<int> courseIds)
            => Task.FromResult(new BulkArchiveResponseDto { TotalRequested = courseIds.Count, SuccessCount = courseIds.Count });
        public Task<List<CourseResponseDto>> GetArchivedCoursesAsync()
            => Task.FromResult(new List<CourseResponseDto>());
        public Task<PagedResult<CourseResponseDto>> GetArchivedCoursesPagedAsync(PaginationParams paginationParams)
            => Task.FromResult(new PagedResult<CourseResponseDto> { Items = new List<CourseResponseDto>(), TotalCount = 0, PageNumber = paginationParams.PageNumber, PageSize = paginationParams.PageSize });
        public Task<ArchiveStatisticsDto> GetCourseArchiveStatisticsAsync()
            => Task.FromResult(new ArchiveStatisticsDto());
    }

    [Collection("MapsterWarmup")]
    public class CourseControllerTests
    {
        private static CourseController CreateController(FakeCourseService courseService)
        {
            var controller = new CourseController(courseService, new FakeActivityLogServiceCourse());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "42"),
                        new Claim(ClaimTypes.Role, "Teacher")
                    }, "TestAuth"))
                }
            };
            return controller;
        }

        [Fact]
        public async Task EnrollStudent_ReturnsOk_WhenValid()
        {
            var service = new FakeCourseService();
            var controller = CreateController(service);
            var dto = new EnrollStudentDto { StudentId = 100, CourseId = 5, Section = "A", EnrolledBy = 42 };

            var result = await controller.EnrollStudent(dto);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var enrollment = Assert.IsType<EnrollmentResponseDto>(ok.Value);
            Assert.Equal(100, enrollment.UserId);
            Assert.Equal(5, enrollment.CourseId);
        }

        [Fact]
        public async Task EnrollStudent_ReturnsUnauthorized_OnUnauthorizedAccess()
        {
            var service = new FakeCourseService { ThrowUnauthorized = true };
            var controller = CreateController(service);
            var dto = new EnrollStudentDto { StudentId = 101, CourseId = 6, EnrolledBy = 42 };

            var result = await controller.EnrollStudent(dto);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var payload = unauthorized.Value as dynamic;
            Assert.NotNull(payload);
        }

        [Fact]
        public async Task GetCourseEnrollments_ReturnsOk_EmptyList_WhenNone()
        {
            var service = new FakeCourseService { ReturnEmptyEnrollments = true };
            var controller = CreateController(service);

            var result = await controller.GetCourseEnrollments(courseId: 5, teacherId: 42);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsType<List<EnrollmentResponseDto>>(ok.Value);
            Assert.Empty(list);
        }

        [Fact]
        public async Task GetCourseEnrollments_ReturnsUnauthorized_OnUnauthorizedAccess()
        {
            var service = new FakeCourseService { ThrowUnauthorized = true };
            var controller = CreateController(service);

            var result = await controller.GetCourseEnrollments(courseId: 7, teacherId: 42);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var payload = unauthorized.Value as dynamic;
            Assert.NotNull(payload);
        }
    }
}