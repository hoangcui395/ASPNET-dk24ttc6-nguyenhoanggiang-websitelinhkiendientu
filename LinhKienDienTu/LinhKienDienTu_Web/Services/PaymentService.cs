using Microsoft.AspNetCore.Http;

namespace LinhKienDienTu_Web.Services
{
    public class PaymentService
    {
        public string CreateVnPayPaymentUrl(int orderId, decimal amount, HttpContext httpContext)
        {
            // Trong thực tế, đây là nơi xây dựng URL trỏ đến cổng VNPay với chữ ký điện tử
            // Ở đây chúng ta giả lập bằng một URL nội bộ trỏ về Action xử lý kết quả
            
            var scheme = httpContext.Request.Scheme;
            var host = httpContext.Request.Host;
            var callbackUrl = $"{scheme}://{host}/Order/PaymentCallback?orderId={orderId}&status=success";
            
            return callbackUrl;
        }

        public bool ValidatePaymentResponse(string status)
        {
            return status == "success";
        }
    }
}
