using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;

[Authorize(Roles = "Admin")]
public class AdminVoucherController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminVoucherController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var vouchers = await _context.Vouchers.OrderByDescending(v => v.Ngay_Bat_Dau).ToListAsync();
        return View(vouchers);
    }

    public IActionResult Create()
    {
        var voucher = new Voucher
        {
            Ngay_Bat_Dau = DateTime.Now,
            Ngay_Het_Han = DateTime.Now.AddDays(30)
        };
        return View(voucher);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Voucher voucher)
    {
        if (ModelState.IsValid)
        {
            _context.Add(voucher);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tạo mã giảm giá thành công!";
            return RedirectToAction(nameof(Index));
        }
        return View(voucher);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var voucher = await _context.Vouchers.FindAsync(id);
        if (voucher == null) return NotFound();
        return View(voucher);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Voucher voucher)
    {
        if (ModelState.IsValid)
        {
            _context.Update(voucher);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật Voucher thành công!";
            return RedirectToAction(nameof(Index));
        }
        return View(voucher);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var voucher = await _context.Vouchers.FindAsync(id);
        if (voucher != null)
        {
            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        return Json(new { success = false });
    }
}
