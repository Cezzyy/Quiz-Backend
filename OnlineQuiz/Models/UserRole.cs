using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineQuiz.Models
{
    [Table("UserRole")]
    public class UserRole
    {
        [Column("UserId")]
        public int UserId { get; set; }

        [Column("RoleId")]
        public int RoleId { get; set; }
    }
}
