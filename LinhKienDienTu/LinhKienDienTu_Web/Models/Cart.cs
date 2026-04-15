using System.ComponentModel.DataAnnotations;

namespace LinhKienDienTu_Web.Models
{
    public class Cart
    {
        [Key]
        public int Cart_ID { get; set; }

        public int User_ID { get; set; }
        public User User { get; set; } = null!;

        public int Product_ID { get; set; }
        public Product Product { get; set; } = null!;

        public int So_Luong { get; set; }

        public DateTime Created_At { get; set; } = DateTime.Now;

        public DateTime Updated_At { get; set; } = DateTime.Now;
    }
}
