using System.ComponentModel.DataAnnotations;

namespace OnlineQuiz.DTOs
{
    /// <summary>
    /// Bulk delete courses (Admin only)
    /// </summary>
    public class BulkDeleteCoursesDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one course ID is required")]
        public List<int> CourseIds { get; set; } = new();
    }

    /// <summary>
    /// Bulk delete quizzes (Course instructor or admin)
    /// </summary>
    public class BulkDeleteQuizzesDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one quiz ID is required")]
        public List<int> QuizIds { get; set; } = new();

        [Required]
        public int UserId { get; set; }  // For authorization check
    }

    /// <summary>
    /// Bulk delete attempts (Teacher for their courses, Student for own unsubmitted)
    /// </summary>
    public class BulkDeleteAttemptsDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one attempt ID is required")]
        public List<int> AttemptIds { get; set; } = new();

        [Required]
        public int UserId { get; set; }  // For authorization check
    }

    /// <summary>
    /// Bulk unenroll students from a course (Course instructor)
    /// Supports two variants:
    /// 1. By enrollment IDs
    /// 2. By course ID + student IDs
    /// </summary>
    public class BulkDeleteEnrollmentsDto
    {
        // Variant 1: Delete by enrollment IDs
        public List<int>? EnrollmentIds { get; set; }

        // Variant 2: Delete by course + student IDs
        public int? CourseId { get; set; }
        public List<int>? StudentIds { get; set; }

        public bool IsValid()
        {
            // Must have either EnrollmentIds OR (CourseId + StudentIds)
            var hasVariant1 = EnrollmentIds != null && EnrollmentIds.Any();
            var hasVariant2 = CourseId.HasValue && StudentIds != null && StudentIds.Any();

            return hasVariant1 ^ hasVariant2; // XOR - exactly one must be true
        }
    }

    /// <summary>
    /// Bulk delete users (Admin only)
    /// </summary>
    public class BulkDeleteUsersDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one user ID is required")]
        public List<int> UserIds { get; set; } = new();
    }
}
