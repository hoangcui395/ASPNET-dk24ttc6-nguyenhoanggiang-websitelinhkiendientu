using System.ComponentModel.DataAnnotations;

namespace LinhKienDienTu_Web.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ cụ thể.")]
        public string DiaChiGiaoHang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn Quận/Huyện.")]
        public string QuanHuyen { get; set; } = string.Empty;

        public string PhuongThucThanh_Toan { get; set; } = "COD";

        /// <summary>Ghi chú đơn hàng — tùy chọn, không bắt buộc</summary>
        public string? GhiChu { get; set; }

        public List<CartItem> Items { get; set; } = new();
        public CartSummaryViewModel Summary { get; set; } = new();

        /// <summary>Lưu trữ danh sách ProductId được khách hàng chọn từ Giỏ hàng</summary>
        public List<int> SelectedProductIds { get; set; } = new();

        /// <summary>Voucher nếu khách hàng nhập</summary>
        public string? VoucherCode { get; set; }

        /// <summary>Số tiền được giảm từ Voucher</summary>
        public decimal VoucherDiscount { get; set; } = 0;
    }
}
