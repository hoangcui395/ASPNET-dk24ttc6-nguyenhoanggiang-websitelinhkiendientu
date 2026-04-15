using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;
using System.Security.Claims;

namespace LinhKienDienTu_Web.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            await LoadCategoriesAsync();
            var userId = GetCurrentUserId();
            var wishlist = await _context.Wishlists
                .Include(w => w.Product)
                .Where(w => w.User_ID == userId)
                .OrderByDescending(w => w.Created_At)
                .ToListAsync();

            return View(wishlist);
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userId = GetCurrentUserId();
            var item = await _context.Wishlists.FirstOrDefaultAsync(w => w.User_ID == userId && w.Product_ID == productId);

            if (item != null)
            {
                _context.Wishlists.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true, action = "removed", message = "Đã xóa khỏi danh sách yêu thích." });
            }

            var newItem = new Wishlist
            {
                User_ID = userId,
                Product_ID = productId
            };
            _context.Wishlists.Add(newItem);
            await _context.SaveChangesAsync();

            return Json(new { success = true, action = "added", message = "Đã thêm vào danh sách yêu thích." });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = GetCurrentUserId();
            var item = await _context.Wishlists.FirstOrDefaultAsync(w => w.Wishlist_ID == id && w.User_ID == userId);
            
            if (item != null)
            {
                _context.Wishlists.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue("UserID");
            return int.Parse(userIdClaim ?? "0");
        }

        private async Task LoadCategoriesAsync()
        {
            ViewData["Categories"] = await _context.Categories
                .Include(c => c.SubCategories)
                .ToListAsync();
        }
    }
}
