using System.ComponentModel.DataAnnotations;

namespace LinhKienDienTu_Web.Models
{
    public class Product
    {
        [Key]
        public int Product_ID { get; set; }

        [Required]
        public string Ten_San_Pham { get; set; }

        [Required]
        public decimal Gia_Goc { get; set; }

        public decimal Phan_Tram_Giam { get; set; }

        public int So_Luong_Ton { get; set; }

        public string? Hinh_Anh { get; set; }

        public string Trang_Thai { get; set; }

        public int SubCategory_ID { get; set; }
        public SubCategory? SubCategory { get; set; }

        public int? Promotion_ID { get; set; }
        public Promotion? Promotion { get; set; }

        public bool IsTphcmOnly { get; set; } = false;

        public bool IsCombo { get; set; } = false;

        public virtual ICollection<ComboDetail>? ComboDetails { get; set; }
    }
}