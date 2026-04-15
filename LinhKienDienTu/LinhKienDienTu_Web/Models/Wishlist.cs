using System.ComponentModel.DataAnnotations;

namespace LinhKienDienTu_Web.Models
{
    public class Wishlist
    {
        [Key]
        public int Wishlist_ID { get; set; }

        public int User_ID { get; set; }
        public User? User { get; set; }

        public int Product_ID { get; set; }
        public Product? Product { get; set; }

        public DateTime Created_At { get; set; } = DateTime.Now;
    }
}
