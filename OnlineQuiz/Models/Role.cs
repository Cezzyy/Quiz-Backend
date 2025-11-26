using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace OnlineQuiz.Models
{
    [Table("Role")]
    public class Role : BaseModel
    {
        [PrimaryKey("RoleId")]
        [Column("RoleId", ignoreOnInsert: true)]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;
    }
}
