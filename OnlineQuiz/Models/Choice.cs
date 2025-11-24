using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuiz.Models
{
    [Table("Choice")]
    public class Choice
    {
        [Key]
        [Column("ChoiceId")]
        public int ChoiceId { get; set; }

        [Required]
        [Column("QuestionId")]
        public int QuestionId { get; set; }

        [Required]
        [Column("Body")]
        public string Body { get; set; } = string.Empty;

        [Column("Is_Correct")]
        public bool IsCorrect { get; set; } = false;
    }
}
