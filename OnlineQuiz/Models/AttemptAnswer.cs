using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuiz.Models
{
    [Table("AttemptAnswer")]
    public class AttemptAnswer
    {
        [Key]
        [Column("AttemptAnswerId")]
        public int AttemptAnswerId { get; set; }

        [Required]
        [Column("AttemptId")]
        public int AttemptId { get; set; }

        [Required]
        [Column("QuestionId")]
        public int QuestionId { get; set; }

        [Column("ChoiceId")]
        public int? ChoiceId { get; set; }

        [Column("Free_Text")]
        public string? FreeText { get; set; }

        [Column("Is_Correct")]
        public bool? IsCorrect { get; set; }
    }
}
