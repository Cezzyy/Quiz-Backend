namespace OnlineQuiz.DTOs
{
    public class AdminDashboardDto
    {
        public int TotalUsers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalQuizzes { get; set; }
        public int ActiveUsers { get; set; }
        public List<RegistrationStatDto> RecentRegistrations { get; set; } = new();
        public List<ActivityLogDto> RecentActivities { get; set; } = new();
    }

    public class RegistrationStatDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class TeacherDashboardDto
    {
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalQuizzes { get; set; }
        public int PendingGrading { get; set; }
        public List<CoursePerformanceDto> CoursePerformance { get; set; } = new();
    }

    public class CoursePerformanceDto
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public double AverageScore { get; set; }
    }

    public class StudentDashboardDto
    {
        public int EnrolledCourses { get; set; }
        public int CompletedQuizzes { get; set; }
        public double AverageScore { get; set; }
        public List<UpcomingQuizDto> UpcomingQuizzes { get; set; } = new();
        public List<RecentResultDto> RecentResults { get; set; } = new();
    }

    public class UpcomingQuizDto
    {
        public int QuizId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
    }

    public class RecentResultDto
    {
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class CourseAnalyticsDto
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int TotalQuizzes { get; set; }
        public double AverageScore { get; set; }
        public List<StudentProgressDto> StudentProgress { get; set; } = new();
    }

    public class StudentProgressDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int QuizzesTaken { get; set; }
        public double AverageScore { get; set; }
    }
}
