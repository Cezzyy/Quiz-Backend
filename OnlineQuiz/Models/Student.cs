using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace OnlineQuiz.Models
{
    [Table("Student")]
    public class Student : BaseModel
    {
        [PrimaryKey("UserId")]
        [Column("UserId")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("StudentId")]
        public string StudentId { get; set; } = string.Empty;

        [Column("Year_Level")]
        public int? YearLevel { get; set; }

        [MaxLength(100)]
        [Column("Section")]
        public string? Section { get; set; }

        [MaxLength(255)]
        [Column("Course")]
        public string? Course { get; set; }
    }
}
