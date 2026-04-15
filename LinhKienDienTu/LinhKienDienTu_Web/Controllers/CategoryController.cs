using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;
[Authorize(Roles = "Admin")]
public class CategoryController : Controller
{
    private readonly ApplicationDbContext _context;

    public CategoryController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Categories
            .Include(c => c.SubCategories)
            .OrderBy(c => c.Category_ID)
            .ToListAsync());
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(Category c)
    {
        if (!ModelState.IsValid)
        {
            return View(c);
        }

        try
        {
            _context.Add(c);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm danh mục thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Không thể lưu danh mục vào database.");
            return View(c);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();
        return View(category);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Category c)
    {
        var categoryInDb = await _context.Categories.FindAsync(c.Category_ID);
        if (categoryInDb == null) return NotFound();

        categoryInDb.Ten_Danh_Muc = c.Ten_Danh_Muc;

        ModelState.Clear();
        if (!TryValidateModel(categoryInDb))
        {
            return View(categoryInDb);
        }

        try
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Không thể cập nhật danh mục trong database.");
            return View(categoryInDb);
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        var category = await _context.Categories
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Category_ID == id);
        if (category == null) return NotFound();
        return View(category);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Category_ID == id);
        if (category == null) return NotFound();
        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await _context.Categories
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Category_ID == id);
        if (category == null) return NotFound();

        if (category.SubCategories.Any())
        {
            TempData["ErrorMessage"] = "Không thể xóa danh mục vì đang có danh mục con liên kết.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa danh mục thành công!";
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Không thể xóa danh mục trong database.";
        }

        return RedirectToAction(nameof(Index));
    }
}