using System.ComponentModel.DataAnnotations;

namespace OnlineQuiz.DTOs
{
    public class CreateUserDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(255)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role ID is required")]
        [Range(1, 3, ErrorMessage = "Role ID must be 1 (Admin), 2 (Teacher), or 3 (Student)")]
        public int RoleId { get; set; }

        [MaxLength(50)]
        public string? ContactNumber { get; set; }

        [MaxLength(255)]
        public string? EmergencyContactPerson { get; set; }

        [MaxLength(50)]
        public string? EmergencyContactNumber { get; set; }

        // Student-specific fields (required if RoleId = 3)
        [MaxLength(50)]
        public string? StudentId { get; set; }

        public int? YearLevel { get; set; }

        [MaxLength(100)]
        public string? Section { get; set; }

        [MaxLength(255)]
        public string? Course { get; set; }

        // Teacher-specific fields (optional if RoleId = 2)
        [MaxLength(255)]
        public string? Department { get; set; }

        public int? CreatedBy { get; set; }
    }
}
