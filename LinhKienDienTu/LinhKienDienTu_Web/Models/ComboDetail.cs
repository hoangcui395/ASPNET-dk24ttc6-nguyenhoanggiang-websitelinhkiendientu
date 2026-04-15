using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinhKienDienTu_Web.Models
{
    public class ComboDetail
    {
        [Key, Column(Order = 0)]
        public int Combo_ID { get; set; } // Khóa ngoại trỏ đến Product_ID của Thùng/Combo

        [Key, Column(Order = 1)]
        public int Product_ID { get; set; } // Khóa ngoại trỏ đến Product_ID của Sản phẩm lẻ

        [Required]
        public int So_Luong_Thanh_Phan { get; set; } // Số lượng sản phẩm lẻ có trong 1 Combo

        // Navigation properties
        [ForeignKey("Combo_ID")]
        public virtual Product ComboProduct { get; set; }

        [ForeignKey("Product_ID")]
        public virtual Product ComponentProduct { get; set; }
    }
}
