using System.ComponentModel.DataAnnotations;

namespace LinhKienDienTu_Web.Models
{
    public class Voucher
    {
        [Key]
        public int Voucher_ID { get; set; }

        [Required(ErrorMessage = "Mã Voucher không được để trống")]
        [StringLength(50)]
        public string Ma_Code { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Loai_Giam_Gia { get; set; } = "PERCENT"; // PERCENT, AMOUNT

        [Required(ErrorMessage = "Giá trị giảm không được để trống")]
        public decimal Gia_Tri_Giam { get; set; }

        public decimal Don_Toi_Thieu { get; set; } = 0;

        public int So_Luong_Gioi_Han { get; set; } = 0;

        public int So_Luong_Da_Dung { get; set; } = 0;

        public DateTime Ngay_Bat_Dau { get; set; } = DateTime.Now;

        public DateTime Ngay_Het_Han { get; set; }

        public bool Trang_Thai { get; set; } = true;

        public bool IsActive => Trang_Thai && 
                                Ngay_Bat_Dau <= DateTime.Now && 
                                Ngay_Het_Han > DateTime.Now && 
                                So_Luong_Da_Dung < So_Luong_Gioi_Han;
    }
}
