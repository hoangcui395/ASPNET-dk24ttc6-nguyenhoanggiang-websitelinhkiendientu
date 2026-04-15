using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;

[Authorize(Roles = "Admin")]
public class PromotionController : Controller
{
    private readonly ApplicationDbContext _context;

    public PromotionController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ─────────────────────────────────────────────
    // Danh sách khuyến mãi
    // ─────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var promotions = await _context.Promotions
            .OrderByDescending(x => x.Promotion_ID)
            .ToListAsync();

        // Gắn số sản phẩm đang dùng mỗi promotion
        var productCounts = await _context.Products
            .Where(p => p.Promotion_ID != null)
            .GroupBy(p => p.Promotion_ID)
            .Select(g => new { PromotionId = g.Key!.Value, Count = g.Count() })
            .ToDictionaryAsync(x => x.PromotionId, x => x.Count);

        ViewBag.ProductCounts = productCounts;
        return View(promotions);
    }

    // ─────────────────────────────────────────────
    // Tạo khuyến mãi
    // ─────────────────────────────────────────────
    public async Task<IActionResult> Create()
    {
        await LoadScopeDataAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Promotion p,
        int? categoryId,
        int? subCategoryId,
        int[]? productIds)
    {
        if (p.Ngay_Ket_Thuc < p.Ngay_Bat_Dau)
            ModelState.AddModelError(nameof(p.Ngay_Ket_Thuc), "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");

        if (!ModelState.IsValid)
        {
            await LoadScopeDataAsync();
            return View(p);
        }

        _context.Promotions.Add(p);
        await _context.SaveChangesAsync();

        await ApplyPromotionAsync(p.Promotion_ID, categoryId, subCategoryId, productIds);
        TempData["SuccessMessage"] = $"Tạo khuyến mãi '{p.Ten_Khuyen_Mai}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    // ─────────────────────────────────────────────
    // Sửa khuyến mãi
    // ─────────────────────────────────────────────
    public async Task<IActionResult> Edit(int id)
    {
        var p = await _context.Promotions.FindAsync(id);
        if (p == null) return NotFound();
        await LoadScopeDataAsync(id);
        return View(p);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(
        Promotion p,
        int? categoryId,
        int? subCategoryId,
        int[]? productIds)
    {
        var inDb = await _context.Promotions.FindAsync(p.Promotion_ID);
        if (inDb == null) return NotFound();

        if (p.Ngay_Ket_Thuc < p.Ngay_Bat_Dau)
            ModelState.AddModelError(nameof(p.Ngay_Ket_Thuc), "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");

        if (!ModelState.IsValid)
        {
            await LoadScopeDataAsync(p.Promotion_ID);
            return View(p);
        }

        inDb.Ten_Khuyen_Mai = p.Ten_Khuyen_Mai;
        inDb.Phan_Tram_Giam = p.Phan_Tram_Giam;
        inDb.Ngay_Bat_Dau = p.Ngay_Bat_Dau;
        inDb.Ngay_Ket_Thuc = p.Ngay_Ket_Thuc;

        await _context.SaveChangesAsync();
        await ApplyPromotionAsync(inDb.Promotion_ID, categoryId, subCategoryId, productIds);

        TempData["SuccessMessage"] = $"Cập nhật khuyến mãi '{inDb.Ten_Khuyen_Mai}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    // ─────────────────────────────────────────────
    // Chi tiết
    // ─────────────────────────────────────────────
    public async Task<IActionResult> Details(int id)
    {
        var p = await _context.Promotions.FindAsync(id);
        if (p == null) return NotFound();

        var products = await _context.Products
            .Include(x => x.SubCategory)
            .Where(x => x.Promotion_ID == id)
            .OrderBy(x => x.Ten_San_Pham)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.Products = products;
        return View(p);
    }

    // ─────────────────────────────────────────────
    // Xóa khuyến mãi (gỡ khỏi sản phẩm trước)
    // ─────────────────────────────────────────────
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _context.Promotions.FindAsync(id);
        if (p == null) return NotFound();
        ViewBag.ProductCount = await _context.Products.CountAsync(x => x.Promotion_ID == id);
        return View(p);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var p = await _context.Promotions.FindAsync(id);
        if (p == null) return NotFound();

        // Gỡ promotion khỏi tất cả sản phẩm
        var products = await _context.Products.Where(x => x.Promotion_ID == id).ToListAsync();
        foreach (var product in products) product.Promotion_ID = null;

        _context.Promotions.Remove(p);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Xóa khuyến mãi '{p.Ten_Khuyen_Mai}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    // ─────────────────────────────────────────────
    // Gỡ khuyến mãi khỏi 1 sản phẩm cụ thể
    // ─────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> RemoveProduct(int promotionId, int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product != null && product.Promotion_ID == promotionId)
        {
            product.Promotion_ID = null;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Details), new { id = promotionId });
    }

    // ─────────────────────────────────────────────
    // PRIVATE HELPERS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Gán Promotion_ID cho các sản phẩm theo 3 phạm vi có thể kết hợp:
    /// 1. Theo Danh mục chính (categoryId)
    /// 2. Theo Danh mục con (subCategoryId) — ưu tiên cao hơn categoryId
    /// 3. Theo từng sản phẩm cụ thể (productIds — multi-select)
    /// Nếu không chọn gì cả → không thay đổi gì.
    /// </summary>
    private async Task ApplyPromotionAsync(
        int promotionId,
        int? categoryId,
        int? subCategoryId,
        int[]? productIds)
    {
        var productIdsToUpdate = new HashSet<int>();

        // Phạm vi: Danh mục con
        if (subCategoryId.HasValue && subCategoryId.Value > 0)
        {
            var ids = await _context.Products
                .Where(p => p.SubCategory_ID == subCategoryId.Value)
                .Select(p => p.Product_ID)
                .ToListAsync();
            ids.ForEach(id => productIdsToUpdate.Add(id));
        }

        // Phạm vi: Danh mục chính (nếu chưa có subcategory)
        if (categoryId.HasValue && categoryId.Value > 0 && !subCategoryId.HasValue)
        {
            var subIds = await _context.SubCategories
                .Where(s => s.Category_ID == categoryId.Value)
                .Select(s => s.SubCategory_ID)
                .ToListAsync();

            var ids = await _context.Products
                .Where(p => subIds.Contains(p.SubCategory_ID))
                .Select(p => p.Product_ID)
                .ToListAsync();
            ids.ForEach(id => productIdsToUpdate.Add(id));
        }

        // Phạm vi: Sản phẩm cụ thể (multi-select)
        if (productIds != null && productIds.Length > 0)
        {
            foreach (var pid in productIds)
                productIdsToUpdate.Add(pid);
        }

        if (!productIdsToUpdate.Any()) return;

        var products = await _context.Products
            .Where(p => productIdsToUpdate.Contains(p.Product_ID))
            .ToListAsync();

        foreach (var product in products)
            product.Promotion_ID = promotionId;

        await _context.SaveChangesAsync();
    }

    private async Task LoadScopeDataAsync(int? currentPromotionId = null)
    {
        ViewBag.Categories = await _context.Categories
            .OrderBy(c => c.Ten_Danh_Muc)
            .ToListAsync();

        ViewBag.SubCategories = await _context.SubCategories
            .Include(s => s.Category)
            .OrderBy(s => s.Ten_Danh_Muc_Con)
            .ToListAsync();

        ViewBag.AllProducts = await _context.Products
            .Include(p => p.SubCategory)
            .OrderBy(p => p.Ten_San_Pham)
            .AsNoTracking()
            .ToListAsync();

        // Sản phẩm đang được gán cho promotion này (để pre-select khi Edit)
        if (currentPromotionId.HasValue)
        {
            ViewBag.CurrentProductIds = await _context.Products
                .Where(p => p.Promotion_ID == currentPromotionId.Value)
                .Select(p => p.Product_ID)
                .ToListAsync();
        }
        else
        {
            ViewBag.CurrentProductIds = new List<int>();
        }
    }
}
