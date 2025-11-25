using System.ComponentModel.DataAnnotations;

namespace OnlineQuiz.DTOs
{
    public class CreateCourseDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int InstructorId { get; set; }

        public string? Category { get; set; }
        
        public string? Section { get; set; }

        [Required]
        public int CreatedBy { get; set; }
    }

    public class UpdateCourseDto
    {
        public string? Name { get; set; }
        public string? Status { get; set; }
        public string? Category { get; set; }
        public string? Section { get; set; }
        public int? InstructorId { get; set; }
    }

    public class CourseResponseDto
    {
        public int CourseId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int InstructorId { get; set; }
        public string? InstructorName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Section { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EnrollStudentDto
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public int CourseId { get; set; }

        public string? Section { get; set; }

        [Required]
        public int EnrolledBy { get; set; }
    }
    
    public class EnrollmentResponseDto
    {
        public int EnrollmentId { get; set; }
        public int UserId { get; set; }
        public string? StudentName { get; set; }
        public int CourseId { get; set; }
        public string? CourseName { get; set; }
        public DateTime EnrolledAt { get; set; }
        public string? Section { get; set; }
    }
}
