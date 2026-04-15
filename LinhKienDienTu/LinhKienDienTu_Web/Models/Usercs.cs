using LinhKienDienTu_Web.Models;
using System.ComponentModel.DataAnnotations;
namespace LinhKienDienTu_Web.Models
{
    public class User
    {
        [Key]
        public int User_ID { get; set; }

        [Required]
        public string Ho_Ten { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string Mat_Khau { get; set; }

        [Required]
        public string So_Dien_Thoai { get; set; }

        public string Role { get; set; } = "User";
        public bool IsLocked { get; set; } = false;

        public DateTime Created_At { get; set; } = DateTime.Now;

        // Reset Password fields
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        public List<Order> Orders { get; set; } = new List<Order>();
    }
}
