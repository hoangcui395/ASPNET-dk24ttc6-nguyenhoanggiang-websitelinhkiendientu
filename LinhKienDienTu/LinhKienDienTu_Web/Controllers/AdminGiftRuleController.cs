using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;

[Authorize(Roles = "Admin")]
public class AdminGiftRuleController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminGiftRuleController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var rules = await _context.GiftRules
            .Include(g => g.MainProduct)
            .Include(g => g.GiftProduct)
            .ToListAsync();
        return View(rules);
    }

    public async Task<IActionResult> Create()
    {
        var products = await _context.Products.OrderBy(p => p.Ten_San_Pham).ToListAsync();
        ViewBag.Products = new SelectList(products, "Product_ID", "Ten_San_Pham");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GiftRule rule)
    {
        if (ModelState.IsValid)
        {
            _context.Add(rule);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm quy tắc quà tặng thành công!";
            return RedirectToAction(nameof(Index));
        }
        var products = await _context.Products.OrderBy(p => p.Ten_San_Pham).ToListAsync();
        ViewBag.Products = new SelectList(products, "Product_ID", "Ten_San_Pham");
        return View(rule);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var rule = await _context.GiftRules.FindAsync(id);
        if (rule != null)
        {
            _context.GiftRules.Remove(rule);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        return Json(new { success = false });
    }
}
