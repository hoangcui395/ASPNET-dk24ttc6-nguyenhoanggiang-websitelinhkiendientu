using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;

namespace LinhKienDienTu_Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OrderService _orderService;

        // Nhãn TP.HCM — phát hiện sản phẩm chỉ giao nội thành
        private static readonly string[] TphcmKeywords = new[]
        {
            "tươi", "tuoi", "lạnh", "lanh", "pizza", "mỳ ý", "my y",
            "xốt", "xot", "bằm", "bam", "phô mai", "pho mai"
        };

        public AdminOrderController(ApplicationDbContext context, OrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }

        // ─────────────────────────────────────────────────────────
        // GET /AdminOrder — Danh sách tất cả đơn hàng
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index(string? status, string? search)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .AsQueryable();

            // 1. Lọc theo trạng thái (Chuẩn hóa tên)
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Trang_Thai_Don_Hang == status);

            // 2. Tìm kiếm (Mã đơn, Tên khách hàng, Số điện thoại)
            if (!string.IsNullOrWhiteSpace(search))
            {
                if (int.TryParse(search, out var orderId))
                {
                    query = query.Where(o => o.Order_ID == orderId);
                }
                else
                {
                    var s = search.ToLower();
                    query = query.Where(o => 
                        (o.User != null && o.User.Ho_Ten.ToLower().Contains(s)) ||
                        (o.User != null && o.User.So_Dien_Thoai.Contains(s))
                    );
                }
            }

            // Ưu tiên: Chờ xác nhận lên đầu, sau đó theo ngày giảm dần
            var orders = await query
                .OrderBy(o => o.Trang_Thai_Don_Hang == "Chờ xác nhận" ? 0 :
                              o.Trang_Thai_Don_Hang == "Đang lấy hàng" ? 1 :
                              o.Trang_Thai_Don_Hang == "Đang giao" ? 2 :
                              o.Trang_Thai_Don_Hang == "Thành công" ? 3 : 4)
                .ThenByDescending(o => o.Ngay_Dat)
                .AsNoTracking()
                .ToListAsync();

            // Thống kê nhanh theo chuẩn mới
            var stats = await _context.Orders
                .GroupBy(o => o.Trang_Thai_Don_Hang)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.CountCho = stats.FirstOrDefault(s => s.Status == "Chờ xác nhận")?.Count ?? 0;
            ViewBag.CountXacNhan = stats.FirstOrDefault(s => s.Status == "Đang lấy hàng")?.Count ?? 0;
            ViewBag.CountGiao = stats.FirstOrDefault(s => s.Status == "Đang giao")?.Count ?? 0;
            ViewBag.CountHoan = stats.FirstOrDefault(s => s.Status == "Thành công")?.Count ?? 0;
            ViewBag.CountHuy = stats.FirstOrDefault(s => s.Status == "Đã hủy")?.Count ?? 0;

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;

            return View(orders);
        }

        // ─────────────────────────────────────────────────────────
        // GET /AdminOrder/Detail/{id}
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p!.SubCategory)
                            .ThenInclude(sc => sc!.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Order_ID == id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(Index));
            }

            // Phát hiện sản phẩm chỉ giao TP.HCM
            ViewBag.HasTphcmItems = order.OrderDetails.Any(od =>
                od.Product != null && IsTphcmOnly(od.Product));

            return View(order);
        }

        // ─────────────────────────────────────────────────────────
        // POST /AdminOrder/Confirm — Duyệt đơn + trừ kho
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int orderId)
        {
            var (success, message, errors) = await _orderService.ConfirmOrderAsync(orderId);

            if (success)
                TempData["Success"] = message;
            else
            {
                TempData["Error"] = message;
                if (errors.Any())
                    TempData["StockErrors"] = string.Join("\n", errors);
            }

            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        // ─────────────────────────────────────────────────────────
        // POST /AdminOrder/UpdateStatus — Giao hàng / Hoàn thành
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
        {
            var (success, message) = await _orderService.UpdateOrderStatusAsync(orderId, newStatus);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        // ─────────────────────────────────────────────────────────
        // POST /AdminOrder/Cancel — Admin hủy đơn
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int orderId, string lyDoHuy)
        {
            if (string.IsNullOrWhiteSpace(lyDoHuy))
            {
                TempData["Error"] = "Vui lòng nhập lý do hủy đơn.";
                return RedirectToAction(nameof(Detail), new { id = orderId });
            }

            var (success, message) = await _orderService.AdminCancelOrderAsync(orderId, lyDoHuy);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Detail), new { id = orderId });
        }

        // ─────────────────────────────────────────────────────────
        // HELPER
        // ─────────────────────────────────────────────────────────
        private static bool IsTphcmOnly(Product product)
        {
            var name = (product.Ten_San_Pham ?? "").ToLowerInvariant();
            return TphcmKeywords.Any(kw => name.Contains(kw));
        }
    }
}
