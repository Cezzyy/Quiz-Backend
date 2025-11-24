using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace OnlineQuiz.Models
{
    [Table("Teacher")]
    public class Teacher : BaseModel
    {
        [PrimaryKey("UserId")]
        [Column("UserId")]
        public int UserId { get; set; }

        [MaxLength(255)]
        [Column("Department")]
        public string? Department { get; set; }
    }
}
