using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinhKienDienTu_Web.Models
{
    public class ChatMessage
    {
        [Key]
        public int MessageId { get; set; }

        public Guid SessionId { get; set; }
        
        [ForeignKey("SessionId")]
        public virtual ChatSession Session { get; set; }

        [Required]
        [StringLength(50)]
        public string Sender { get; set; } // "User", "Bot", "Admin"

        [Required]
        public string Content { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
