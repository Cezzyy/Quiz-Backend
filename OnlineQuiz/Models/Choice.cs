using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace OnlineQuiz.Models
{
    [Table("Choice")]
    public class Choice : BaseModel
    {
        [PrimaryKey("ChoiceId")]
        [Column("ChoiceId", ignoreOnInsert: true)]
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
