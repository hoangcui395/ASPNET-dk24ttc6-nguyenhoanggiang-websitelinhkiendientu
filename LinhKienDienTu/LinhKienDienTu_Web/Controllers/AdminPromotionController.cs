using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;

[Authorize(Roles = "Admin")]
public class AdminPromotionController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminPromotionController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _context.Products
            .Include(p => p.SubCategory)
            .OrderBy(p => p.Ten_San_Pham)
            .ToListAsync();
        return View(products);
    }

    [HttpPost]
    public async Task<IActionResult> ApplyBatch(List<int> productIds, decimal discountPercent)
    {
        if (productIds == null || !productIds.Any())
        {
            TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        if (discountPercent < 0 || discountPercent > 100)
        {
            TempData["ErrorMessage"] = "Phần trăm giảm không hợp lệ (0-100).";
            return RedirectToAction(nameof(Index));
        }

        var productsToUpdate = await _context.Products
            .Where(p => productIds.Contains(p.Product_ID))
            .ToListAsync();

        foreach (var p in productsToUpdate)
        {
            p.Phan_Tram_Giam = discountPercent;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã áp dụng mức giảm {discountPercent}% cho {productsToUpdate.Count} sản phẩm.";
        
        return RedirectToAction(nameof(Index));
    }
}
