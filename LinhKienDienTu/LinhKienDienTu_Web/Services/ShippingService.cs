namespace LinhKienDienTu_Web.Services
{
    public class ShippingService
    {
        private readonly List<string> _innerDistricts = new List<string>
        {
            "Quận 1", "Quận 3", "Quận 4", "Quận 5", "Quận 6", "Quận 10", "Quận 11", "Quận Phú Nhuận", "Quận Bình Thạnh", "Quận Tân Bình"
        };

        public decimal CalculateShippingFee(string? district)
        {
            if (string.IsNullOrWhiteSpace(district)) return 0;

            // TP.HCM Inner Districts
            if (_innerDistricts.Any(d => string.Equals(district.Trim(), d, StringComparison.OrdinalIgnoreCase)))
            {
                return 15000;
            }

            // Other Districts in TP.HCM
            return 30000;
        }

        public string GetEstimatedDelivery(string status)
        {
            return status switch
            {
                "Chờ xác nhận" => "Hôm nay hoặc ngày mai",
                "Đang lấy hàng" => "Dự kiến 2-4 giờ tới",
                "Đang giao" => "Dự kiến 30-60 phút tới",
                "Thành công" => "Đã giao lúc " + DateTime.Now.ToShortTimeString(),
                _ => "Chưa xác định"
            };
        }
    }
}
