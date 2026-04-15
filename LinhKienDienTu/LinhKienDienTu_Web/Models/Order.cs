using System.ComponentModel.DataAnnotations;

namespace LinhKienDienTu_Web.Models
{
    public class Order
    {
        [Key]
        public int Order_ID { get; set; }

        public int User_ID { get; set; }
        public User? User { get; set; }

        public DateTime Ngay_Dat { get; set; }

        public decimal Tong_Tien { get; set; }

        public string Dia_Chi_Giao_Hang { get; set; } = string.Empty;
        public decimal Phi_Ship { get; set; } = 0m;

        /// <summary>Hình thức: COD (Tiền mặt), VNPay (Trực tuyến)</summary>
        public string Phuong_Thuc_Thanh_Toan { get; set; } = "COD";

        /// <summary>Trạng thái thanh toán: Chưa thanh toán / Đã thanh toán</summary>
        public string Trang_Thai_Thanh_Toan { get; set; } = "Chưa thanh toán";

        /// <summary>
        /// Trạng thái mới: Chờ xác nhận → Đang lấy hàng → Đang giao → Thành công | Đã hủy
        /// </summary>
        public string Trang_Thai_Don_Hang { get; set; } = "Chờ xác nhận";

        /// <summary>Ghi chú của khách khi đặt hàng (tùy chọn)</summary>
        public string? Ghi_Chu { get; set; }

        /// <summary>Lý do hủy đơn — chỉ có giá trị khi Trang_Thai_Don_Hang = "Đã hủy"</summary>
        public string? Ly_Do_Huy { get; set; }

        // Mở rộng Savings & Voucher
        public int? Voucher_ID { get; set; }
        public Voucher? Voucher { get; set; }

        public decimal Tien_Tiet_Kiem_Khuyen_Mai { get; set; } = 0m;
        public decimal Tien_Tiet_Kiem_Voucher { get; set; } = 0m;

        public List<OrderDetail> OrderDetails { get; set; } = new();
    }
}
