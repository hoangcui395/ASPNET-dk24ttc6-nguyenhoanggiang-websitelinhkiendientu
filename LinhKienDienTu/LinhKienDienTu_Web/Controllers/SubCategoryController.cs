using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;

namespace LinhKienDienTu_Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SubCategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubCategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _context.SubCategories
                .Include(x => x.Category)
                .OrderBy(x => x.SubCategory_ID)
                .ToListAsync();

            return View(data);
        }

        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Ten_Danh_Muc).ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(SubCategory s)
        {
            if (!await _context.Categories.AnyAsync(c => c.Category_ID == s.Category_ID))
            {
                ModelState.AddModelError(nameof(s.Category_ID), "Vui lòng chọn danh mục chính hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _context.Categories.OrderBy(c => c.Ten_Danh_Muc).ToList();
                return View(s);
            }

            try
            {
                _context.Add(s);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm danh mục con thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Không thể lưu danh mục con vào database.");
                ViewBag.Categories = _context.Categories.OrderBy(c => c.Ten_Danh_Muc).ToList();
                return View(s);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var subCategory = await _context.SubCategories.FindAsync(id);
            if (subCategory == null) return NotFound();
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Ten_Danh_Muc).ToList();
            return View(subCategory);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SubCategory s)
        {
            var inDb = await _context.SubCategories.FindAsync(s.SubCategory_ID);
            if (inDb == null) return NotFound();

            if (!await _context.Categories.AnyAsync(c => c.Category_ID == s.Category_ID))
            {
                ModelState.AddModelError(nameof(s.Category_ID), "Vui lòng chọn danh mục chính hợp lệ.");
            }

            inDb.Ten_Danh_Muc_Con = s.Ten_Danh_Muc_Con;
            inDb.Category_ID = s.Category_ID;

            ModelState.Clear();
            if (!TryValidateModel(inDb))
            {
                ViewBag.Categories = _context.Categories.OrderBy(c => c.Ten_Danh_Muc).ToList();
                return View(inDb);
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật danh mục con thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Không thể cập nhật danh mục con trong database.");
                ViewBag.Categories = _context.Categories.OrderBy(c => c.Ten_Danh_Muc).ToList();
                return View(inDb);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var subCategory = await _context.SubCategories
                .Include(s => s.Category)
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.SubCategory_ID == id);
            if (subCategory == null) return NotFound();
            return View(subCategory);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var subCategory = await _context.SubCategories
                .Include(s => s.Category)
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.SubCategory_ID == id);
            if (subCategory == null) return NotFound();
            return View(subCategory);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subCategory = await _context.SubCategories
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.SubCategory_ID == id);
            if (subCategory == null) return NotFound();

            if (subCategory.Products.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa vì danh mục con đang có sản phẩm liên kết.";
                return RedirectToAction(nameof(Index));
            }

            _context.SubCategories.Remove(subCategory);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa danh mục con thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
