namespace OnlineQuiz.DTOs
{
    public class UserResponseDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ContactNumber { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? CreatedBy { get; set; }

        // Archive information
        public DateTime? ArchivedAt { get; set; }
        public int? ArchivedBy { get; set; }
        public string? ArchivedByName { get; set; }

        // Role information
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;

        // Student-specific data (populated if user is a student)
        public StudentData? Student { get; set; }

        // Teacher-specific data (populated if user is a teacher)
        public TeacherData? Teacher { get; set; }
    }

    public class StudentData
    {
        public string StudentId { get; set; } = string.Empty;
        public int? YearLevel { get; set; }
        public string? Section { get; set; }
        public string? Course { get; set; }
    }

    public class TeacherData
    {
        public string? Department { get; set; }
    }
}
