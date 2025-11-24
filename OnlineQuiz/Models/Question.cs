using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuiz.Models
{
    [Table("Question")]
    public class Question
    {
        [Key]
        [Column("QuestionId")]
        public int QuestionId { get; set; }

        [Required]
        [Column("QuizId")]
        public int QuizId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("Type")]
        public string Type { get; set; } = string.Empty;

        [Required]
        [Column("Body")]
        public string Body { get; set; } = string.Empty;

        [Column("Points")]
        public decimal Points { get; set; } = 1.0m;

        [Column("Sort_Order")]
        public int SortOrder { get; set; } = 0;

        // Navigation properties
        [ForeignKey("QuizId")]
        public Quiz Quiz { get; set; } = null!;

        public ICollection<Choice> Choices { get; set; } = new List<Choice>();
        public ICollection<AttemptAnswer> AttemptAnswers { get; set; } = new List<AttemptAnswer>();
    }
}
