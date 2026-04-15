using System.ComponentModel.DataAnnotations;

namespace LinhKienDienTu_Web.Models
{
    public class Promotion
    {
        [Key]
        public int Promotion_ID { get; set; }

        [Required(ErrorMessage = "Tên khuyến mãi không được để trống.")]
        public string Ten_Khuyen_Mai { get; set; }

        [Range(0, 100, ErrorMessage = "Phần trăm giảm phải từ 0 đến 100.")]
        public decimal Phan_Tram_Giam { get; set; }

        public DateTime Ngay_Bat_Dau { get; set; }
        public DateTime Ngay_Ket_Thuc { get; set; }
    }
}
