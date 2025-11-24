using System.ComponentModel.DataAnnotations;

namespace OnlineQuiz.DTOs
{
    public class UpdateUserDto
    {
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(255)]
        public string? FullName { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        [MaxLength(50)]
        public string? ContactNumber { get; set; }

        [MaxLength(50)]
        public string? EmergencyContactNumber { get; set; }

        // Student-specific fields
        public int? YearLevel { get; set; }

        [MaxLength(100)]
        public string? Section { get; set; }

        [MaxLength(255)]
        public string? Course { get; set; }

        // Teacher-specific fields
        [MaxLength(255)]
        public string? Department { get; set; }
    }
}
