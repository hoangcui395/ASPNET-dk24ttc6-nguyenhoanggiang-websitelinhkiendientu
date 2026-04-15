using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;
using LinhKienDienTu_Web.Services;

namespace LinhKienDienTu_Web.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OrderService _orderService;
        private readonly PaymentService _paymentService;

        public OrderController(ApplicationDbContext context, OrderService orderService, PaymentService paymentService)
        {
            _context = context;
            _orderService = orderService;
            _paymentService = paymentService;
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallback(int orderId, string status)
        {
            var userId = GetCurrentUserId();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Order_ID == orderId && o.User_ID == userId);
            
            if (order == null) return NotFound();

            if (_paymentService.ValidatePaymentResponse(status))
            {
                order.Trang_Thai_Thanh_Toan = "Đã thanh toán";
                await _context.SaveChangesAsync();
                TempData["CheckoutSuccess"] = $"Thanh toán thành công cho đơn hàng #{orderId}!";
            }
            else
            {
                TempData["MyOrdersError"] = $"Thanh toán thất bại cho đơn hàng #{orderId}. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(MyOrders));
        }

        // ─────────────────────────────────────────────────────────────
        // GET /Order/MyOrders — Lịch sử đơn hàng của người dùng
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            await LoadCategoriesAsync();
            var userId = GetCurrentUserId();

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.User_ID == userId)
                .OrderByDescending(o => o.Ngay_Dat)
                .AsNoTracking()
                .ToListAsync();

            // Hiển thị thông báo thành công từ checkout
            if (TempData["CheckoutSuccess"] is string msg)
            {
                ViewBag.CheckoutSuccess = msg;
            }

            return View(orders);
        }

        // ─────────────────────────────────────────────────────────────
        // GET /Order/Detail/{id} — Chi tiết đơn hàng
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            await LoadCategoriesAsync();
            var userId = GetCurrentUserId();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Order_ID == id && o.User_ID == userId);

            if (order == null)
            {
                TempData["MyOrdersError"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền xem đơn này.";
                return RedirectToAction(nameof(MyOrders));
            }

            return View(order);
        }

        // ─────────────────────────────────────────────────────────────
        // POST /Order/Cancel — Hủy đơn hàng (kèm lý do)
        // ─────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int orderId, string lyDoHuy)
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(lyDoHuy))
            {
                TempData["CancelError"] = "Vui lòng chọn lý do hủy đơn hàng.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            var (success, message) = await _orderService.CancelOrderAsync(orderId, userId, lyDoHuy);

            if (success)
            {
                TempData["CancelSuccess"] = message;
            }
            else
            {
                TempData["CancelError"] = message;
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            return RedirectToAction(nameof(MyOrders));
        }

        // ─────────────────────────────────────────────────────────────
        // Helper
        // ─────────────────────────────────────────────────────────────
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue("UserID");
            if (!int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Người dùng chưa được xác thực hợp lệ.");
            }

            return userId;
        }

        private async Task LoadCategoriesAsync()
        {
            ViewData["Categories"] = await _context.Categories
                .Include(c => c.SubCategories)
                .ToListAsync();
        }
    }
}
