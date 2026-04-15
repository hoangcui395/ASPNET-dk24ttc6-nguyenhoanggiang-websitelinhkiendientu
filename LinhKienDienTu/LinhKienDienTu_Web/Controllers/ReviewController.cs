using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;
using System.Security.Claims;

namespace LinhKienDienTu_Web.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productId, int rating, string comment)
        {
            var userId = GetCurrentUserId();

            // Check if user has bought this product and order status is 'Thành công'
            var hasBought = await _context.Orders
                .Include(o => o.OrderDetails)
                .AnyAsync(o => o.User_ID == userId && 
                               o.Trang_Thai_Don_Hang == "Thành công" && 
                               o.OrderDetails.Any(od => od.Product_ID == productId));

            if (!hasBought)
            {
                TempData["ReviewError"] = "Bạn chỉ có thể đánh giá sản phẩm sau khi đơn hàng đã hoàn thành.";
                return RedirectToAction("Details", "Home", new { id = productId });
            }

            if (rating < 1 || rating > 5)
            {
                TempData["ReviewError"] = "Vui lòng chọn mức đánh giá từ 1 đến 5 sao.";
                return RedirectToAction("Details", "Home", new { id = productId });
            }

            var review = new Review
            {
                User_ID = userId,
                Product_ID = productId,
                Rating = rating,
                Comment = comment.Trim(),
                Created_At = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
            return RedirectToAction("Details", "Home", new { id = productId });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue("UserID");
            return int.Parse(userIdClaim ?? "0");
        }
    }
}
