using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinhKienDienTu_Web.Models
{
    public class ChatSession
    {
        [Key]
        public Guid SessionId { get; set; } = Guid.NewGuid();

        [StringLength(100)]
        public string GuestOrUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string Status { get; set; } = "Bot"; // "Bot", "Human", "Offline" // Cấp độ 1, 2 = Bot. Cấp độ 3 = Human

        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
