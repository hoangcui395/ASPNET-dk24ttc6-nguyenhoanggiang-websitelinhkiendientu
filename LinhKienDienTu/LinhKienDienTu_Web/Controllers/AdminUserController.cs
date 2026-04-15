using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;

namespace LinhKienDienTu_Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminUserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminUser
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.Created_At)
                .ToListAsync();
            return View(users);
        }

        // POST: AdminUser/ToggleLock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsLocked = !user.IsLocked;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã {(user.IsLocked ? "khóa" : "mở khóa")} tài khoản {user.Ho_Ten}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: AdminUser/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.User_ID == id);

            if (user == null)
            {
                return NotFound();
            }

            // Load Cart items separately
            var cartItems = await _context.Carts
                .Include(c => c.Product)
                    .ThenInclude(p => p.Promotion)
                .Where(c => c.User_ID == id)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.CartItems = cartItems;

            return View(user);
        }

        // POST: AdminUser/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Check if user has orders before deleting or handled by cascade
            var hasOrders = await _context.Orders.AnyAsync(o => o.User_ID == id);
            if (hasOrders)
            {
                TempData["Error"] = "Không thể xóa người dùng này vì đã có đơn hàng trong hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa người dùng thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
