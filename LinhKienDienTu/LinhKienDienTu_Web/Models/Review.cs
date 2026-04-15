using System.ComponentModel.DataAnnotations;

namespace LinhKienDienTu_Web.Models
{
    public class Review
    {
        [Key]
        public int Review_ID { get; set; }

        public int User_ID { get; set; }
        public User? User { get; set; }

        public int Product_ID { get; set; }
        public Product? Product { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Comment { get; set; } = string.Empty;

        public DateTime Created_At { get; set; } = DateTime.Now;
    }
}
