using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace OnlineQuiz.Models
{
    [Table("User")]
    public class User : BaseModel
    {
        [PrimaryKey("UserId")]
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("PasswordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("FullName")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("Status")]
        public string Status { get; set; } = "Active";

        [MaxLength(50)]
        [Column("ContactNumber")]
        public string? ContactNumber { get; set; }

        [MaxLength(50)]
        [Column("EmergencyContactNumber")]
        public string? EmergencyContactNumber { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UpdatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("CreatedBy")]
        public int? CreatedBy { get; set; }
    }
}
