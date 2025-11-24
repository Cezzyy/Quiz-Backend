using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuiz.Models
{
    [Table("Role")]
    public class Role
    {
        [Key]
        [Column("RoleId")]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;
    }
}
