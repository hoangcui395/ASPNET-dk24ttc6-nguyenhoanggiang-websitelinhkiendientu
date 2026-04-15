using System;
using System.ComponentModel.DataAnnotations;

namespace LinhKienDienTu_Web.Models
{
    public class NewsletterSubscriber
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public DateTime Ngay_Dang_Ky { get; set; } = DateTime.Now;

        // Bật / tắt trạng thái nhận email
        public bool Trang_Thai { get; set; } = true;
    }
}
