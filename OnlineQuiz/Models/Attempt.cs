using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace OnlineQuiz.Models
{
    [Table("Attempt")]
    public class Attempt : BaseModel
    {
        [PrimaryKey("AttemptId")]
        [Column("AttemptId", ignoreOnInsert: true)]
        public int AttemptId { get; set; }

        [Required]
        [Column("QuizId")]
        public int QuizId { get; set; }

        [Required]
        [Column("UserId")]
        public int UserId { get; set; }

        [Column("StartedAt")]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        [Column("SubmittedAt")]
        public DateTime? SubmittedAt { get; set; }

        [Column("Score")]
        public decimal Score { get; set; } = 0.0m;

        [Column("Time_Spent_Seconds")]
        public int? TimeSpentSeconds { get; set; }
    }
}
