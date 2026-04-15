using System;
using System.ComponentModel.DataAnnotations;

namespace LinhKienDienTu_Web.Models
{
    public class SearchHistory
    {
        [Key]
        public int ID { get; set; }

        // Nullable User_ID in case we decide to log anonymous searches in the future,
        // though the requirement implies logged-in users.
        public int? User_ID { get; set; }

        [Required]
        public string Keyword { get; set; }

        public DateTime Created_At { get; set; } = DateTime.Now;

        // Navigation property for mapping (optional, but good practice).
        public User? User { get; set; }
    }
}
