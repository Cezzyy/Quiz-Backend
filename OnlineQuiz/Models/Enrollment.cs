using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace OnlineQuiz.Models
{
    [Table("Enrollment")]
    public class Enrollment : BaseModel
    {
        [PrimaryKey("EnrollmentId")]
        [Column("EnrollmentId")]
        public int EnrollmentId { get; set; }

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [Column("CourseId")]
        public int CourseId { get; set; }

        [Column("EnrolledAt")]
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        [Column("Section")]
        public string? Section { get; set; }

        [Required]
        [Column("EnrolledBy")]
        public int EnrolledBy { get; set; }
    }
}
