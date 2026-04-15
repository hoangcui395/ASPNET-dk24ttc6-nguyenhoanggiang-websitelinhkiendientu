using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;
using System;

namespace LinhKienDienTu_Web.Controllers
{
    public class NewsletterController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NewsletterController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Subscribe(string email, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["NewsletterError"] = "Vui lòng nhập email hợp lệ.";
                return Redirect(returnUrl ?? "/");
            }

            email = email.Trim().ToLowerInvariant();
            var existing = await _context.NewsletterSubscribers.FirstOrDefaultAsync(s => s.Email == email);
            if (existing != null)
            {
                TempData["NewsletterMessage"] = "Bạn đã đăng ký nhận bản tin rồi. Cảm ơn bạn!";
            }
            else
            {
                _context.NewsletterSubscribers.Add(new NewsletterSubscriber
                {
                    Email = email,
                    Ngay_Dang_Ky = DateTime.Now,
                    Trang_Thai = true
                });
                await _context.SaveChangesAsync();
                TempData["NewsletterSuccess"] = "Đăng ký nhận bản tin thành công!";
            }

            return Redirect(returnUrl ?? "/");
        }
    }
}
