using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;
using System.Diagnostics;

namespace LinhKienDienTu_Web.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoryId, int? subCategoryId)
        {
            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .ToListAsync();

            var query = _context.Products
                .Include(p => p.SubCategory)
                .ThenInclude(sc => sc.Category)
                .Include(p => p.Promotion)
                .Include(p => p.ComboDetails)
                    .ThenInclude(cd => cd.ComponentProduct)
                .AsQueryable();

            if (subCategoryId.HasValue)
            {
                query = query.Where(p => p.SubCategory_ID == subCategoryId.Value);
                var subCat = await _context.SubCategories.FindAsync(subCategoryId.Value);
                ViewBag.FilterName = subCat?.Ten_Danh_Muc_Con;
                ViewBag.FilterType = "Danh mục con";
            }
            else if (categoryId.HasValue)
            {
                query = query.Where(p => p.SubCategory.Category_ID == categoryId.Value);
                var cat = await _context.Categories.FindAsync(categoryId.Value);
                ViewBag.FilterName = cat?.Ten_Danh_Muc;
                ViewBag.FilterType = "Danh mục chính";
            }

            var products = await query.ToListAsync();

            ViewData["Categories"] = categories;
            ViewBag.CategoryId = categoryId;
            ViewBag.SubCategoryId = subCategoryId;

            return View(products);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult DeliveryPolicy()
        {
            return View();
        }

        public IActionResult GeneralPolicy()
        {
            return View();
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.SubCategory)
                .ThenInclude(sc => sc.Category)
                .Include(p => p.Promotion)
                .Include(p => p.ComboDetails)
                    .ThenInclude(cd => cd.ComponentProduct)
                .FirstOrDefaultAsync(p => p.Product_ID == id);

            if (product == null)
            {
                return NotFound("Sản phẩm không tồn tại hoặc đã bị xóa.");
            }

            // Fetch Gift Rule for this product
            var giftRule = await _context.GiftRules
                .Include(g => g.GiftProduct)
                .FirstOrDefaultAsync(g => g.MainProduct_ID == id && g.IsActive);
            
            ViewBag.GiftRule = giftRule;

            // Fetch Reviews
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.Product_ID == id)
                .OrderByDescending(r => r.Created_At)
                .ToListAsync();
            ViewBag.Reviews = reviews;

            // Check Wishlist & Reviewability if logged in
            bool isInWishlist = false;
            bool canReview = false;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                                  ?? User.FindFirst("UserID")?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    isInWishlist = await _context.Wishlists.AnyAsync(w => w.User_ID == userId && w.Product_ID == id);
                    
                    canReview = await _context.Orders
                        .Include(o => o.OrderDetails)
                        .AnyAsync(o => o.User_ID == userId && 
                                       o.Trang_Thai_Don_Hang == "Thành công" && 
                                       o.OrderDetails.Any(od => od.Product_ID == id));
                }
            }

            ViewBag.IsInWishlist = isInWishlist;
            ViewBag.CanReview = canReview;

            return View(product);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
