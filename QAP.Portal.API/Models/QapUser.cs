using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QAP.Portal.API.Models
{
    [Table("QAP_USERS")]
    public class QapUser
    {
        [Key]
        [Column("EMAIL")]
        [MaxLength(100)]
        public string Email { get; set; } = null!;

        [Column("DISPLAY_NAME")]
        [MaxLength(100)]
        public string DisplayName { get; set; } = null!;

        [Column("ROLE")]
        [MaxLength(20)]
        public string Role { get; set; } = null!;

        [Column("PASSWORD_HASH")]
        [MaxLength(300)]
        public string PasswordHash { get; set; } = null!;

        [Column("IS_ACTIVE")]
        public int IsActive { get; set; } = 1;

        [Column("CREATED_ON")]
        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}
