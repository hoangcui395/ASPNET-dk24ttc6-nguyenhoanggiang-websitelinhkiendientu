using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using LinhKienDienTu_Web.Models;
using System.Security.Claims;
using LinhKienDienTu_Web.Services;
[Authorize(Roles = "Admin")]
public class ProductController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ComboService _comboService;
    private readonly string _uploadFolder;

    public ProductController(ApplicationDbContext context, ComboService comboService)
    {
        _context = context;
        _comboService = comboService;
        _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
    }

    // ========================
    // LIST
    // ========================
    public async Task<IActionResult> Index()
    {
        return View(await _context.Products
            .Include(p => p.ComboDetails)
                .ThenInclude(cd => cd.ComponentProduct)
            .ToListAsync());
    }

    // ========================
    // SEARCH & SUGGESTIONS
    // ========================
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Search(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return RedirectToAction("Index", "Home");

        keyword = keyword.Trim();

        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdStr = User.FindFirstValue("UserID");
            if (int.TryParse(userIdStr, out int userId))
            {
                _context.SearchHistories.Add(new SearchHistory
                {
                    User_ID = userId,
                    Keyword = keyword,
                    Created_At = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
        }

        var products = await _context.Products
            .Include(p => p.SubCategory)
            .ThenInclude(sc => sc.Category)
            .Include(p => p.Promotion)
            .Include(p => p.ComboDetails)
                .ThenInclude(cd => cd.ComponentProduct)
            .Where(p => p.Ten_San_Pham.Contains(keyword) 
                        || p.SubCategory.Ten_Danh_Muc_Con.Contains(keyword)
                        || p.SubCategory.Category.Ten_Danh_Muc.Contains(keyword))
            .ToListAsync();

        ViewBag.Keyword = keyword;
        return View(products); // Use the new View for Search
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Suggestions(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return Json(new object[] { });

        keyword = keyword.Trim();
        var now = DateTime.Now;
        var suggestions = await _context.Products
            .Include(p => p.SubCategory)
            .Include(p => p.Promotion)
            .Where(p => p.Ten_San_Pham.Contains(keyword) || p.SubCategory.Ten_Danh_Muc_Con.Contains(keyword))
            .Take(5)
            .ToListAsync();

        var result = suggestions.Select(p => {
            var hasPromo = p.Promotion != null && p.Promotion.Ngay_Bat_Dau <= now && p.Promotion.Ngay_Ket_Thuc >= now;
            var discount = hasPromo ? p.Promotion.Phan_Tram_Giam : p.Phan_Tram_Giam;
            var finalPrice = p.Gia_Goc * (1 - discount / 100m);
            return new
            {
                id = p.Product_ID,
                name = p.Ten_San_Pham,
                img = p.Hinh_Anh,
                price = Math.Round(finalPrice, 0)
            };
        });

        return Json(result);
    }

    // ========================
    // CREATE
    // ========================
    public IActionResult Create()
    {
        ViewBag.SubCategories = new SelectList(
            _context.SubCategories.OrderBy(x => x.Ten_Danh_Muc_Con).ToList(),
            "SubCategory_ID",
            "Ten_Danh_Muc_Con"
        );
        ViewBag.AllProducts = _context.Products.OrderBy(p => p.Ten_San_Pham).ToList();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Product product, IFormFile? imageFile, List<int> ComponentIds, List<int> ComponentQtys)
    {
        ModelState.Remove(nameof(Product.SubCategory));
        ModelState.Remove(nameof(Product.Promotion));

        if (product.SubCategory_ID <= 0 || !await _context.SubCategories.AnyAsync(x => x.SubCategory_ID == product.SubCategory_ID))
        {
            ModelState.AddModelError(nameof(product.SubCategory_ID), "Vui lòng chọn danh mục con hợp lệ.");
        }

        if (imageFile is not null && imageFile.Length > 0)
        {
            product.Hinh_Anh = await SaveImageAsync(imageFile);
        }
        else
        {
            // DB schema currently requires Hinh_Anh not null.
            product.Hinh_Anh = string.Empty;
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Add(product);
                await _context.SaveChangesAsync();

                if (product.IsCombo && ComponentIds != null)
                {
                    var comboDetails = new List<ComboDetail>();
                    for (int i = 0; i < ComponentIds.Count; i++)
                    {
                        if (ComponentIds[i] > 0 && ComponentQtys[i] > 0)
                        {
                            comboDetails.Add(new ComboDetail
                            {
                                Combo_ID = product.Product_ID,
                                Product_ID = ComponentIds[i],
                                So_Luong_Thanh_Phan = ComponentQtys[i]
                            });
                        }
                    }
                    _context.ComboDetails.AddRange(comboDetails);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Không thể lưu sản phẩm vào database. Vui lòng kiểm tra dữ liệu và thử lại.");
            }
        }

        ViewBag.SubCategories = new SelectList(
            _context.SubCategories.OrderBy(x => x.Ten_Danh_Muc_Con).ToList(),
            "SubCategory_ID",
            "Ten_Danh_Muc_Con",
            product.SubCategory_ID
        );
        ViewBag.AllProducts = _context.Products.OrderBy(p => p.Ten_San_Pham).ToList();
        return View(product);
    }

    // ========================
    // EDIT
    // ========================
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        ViewBag.SubCategories = new SelectList(
            _context.SubCategories.OrderBy(x => x.Ten_Danh_Muc_Con).ToList(),
            "SubCategory_ID",
            "Ten_Danh_Muc_Con",
            product.SubCategory_ID
        );
        ViewBag.AllProducts = _context.Products.Where(p => p.Product_ID != id).OrderBy(p => p.Ten_San_Pham).ToList();
        ViewBag.Components = _context.ComboDetails.Where(cd => cd.Combo_ID == id).ToList();
        return View(product);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Product product, IFormFile? imageFile, List<int> ComponentIds, List<int> ComponentQtys)
    {
        ModelState.Remove(nameof(Product.SubCategory));
        ModelState.Remove(nameof(Product.Promotion));

        var productInDb = await _context.Products.FindAsync(product.Product_ID);
        if (productInDb == null) return NotFound();

        if (product.SubCategory_ID <= 0 || !await _context.SubCategories.AnyAsync(x => x.SubCategory_ID == product.SubCategory_ID))
        {
            ModelState.AddModelError(nameof(product.SubCategory_ID), "Vui lòng chọn danh mục con hợp lệ.");
        }

        productInDb.Ten_San_Pham = product.Ten_San_Pham;
        productInDb.Gia_Goc = product.Gia_Goc;
        productInDb.So_Luong_Ton = product.So_Luong_Ton;
        productInDb.Trang_Thai = product.Trang_Thai;
        productInDb.SubCategory_ID = product.SubCategory_ID;
        productInDb.IsTphcmOnly = product.IsTphcmOnly;
        productInDb.IsCombo = product.IsCombo;

        if (imageFile is not null && imageFile.Length > 0)
        {
            productInDb.Hinh_Anh = await SaveImageAsync(imageFile);
        }
        else if (productInDb.Hinh_Anh is null)
        {
            // DB schema currently requires Hinh_Anh not null.
            productInDb.Hinh_Anh = string.Empty;
        }

        // Clear any binding-time errors so validation reflects the final entity state.
        ModelState.Clear();
        if (!TryValidateModel(productInDb))
        {
            ViewBag.SubCategories = new SelectList(
                _context.SubCategories.OrderBy(x => x.Ten_Danh_Muc_Con).ToList(),
                "SubCategory_ID",
                "Ten_Danh_Muc_Con",
                product.SubCategory_ID
            );
            ViewBag.AllProducts = _context.Products.Where(p => p.Product_ID != product.Product_ID).OrderBy(p => p.Ten_San_Pham).ToList();
            ViewBag.Components = _context.ComboDetails.Where(cd => cd.Combo_ID == product.Product_ID).ToList();
            return View(productInDb);
        }

        if (product.IsCombo && ComponentIds != null && ComponentIds.Contains(product.Product_ID))
        {
            ModelState.AddModelError(string.Empty, "Sản phẩm Combo không thể chứa chính nó làm thành phần.");
            ViewBag.SubCategories = new SelectList(
                _context.SubCategories.OrderBy(x => x.Ten_Danh_Muc_Con).ToList(),
                "SubCategory_ID",
                "Ten_Danh_Muc_Con",
                product.SubCategory_ID
            );
            ViewBag.AllProducts = _context.Products.Where(p => p.Product_ID != product.Product_ID).OrderBy(p => p.Ten_San_Pham).ToList();
            ViewBag.Components = _context.ComboDetails.Where(cd => cd.Combo_ID == product.Product_ID).ToList();
            return View(productInDb);
        }

        try
        {
            await _context.SaveChangesAsync();

            // Handle Combo Details
            var existingDetails = _context.ComboDetails.Where(cd => cd.Combo_ID == product.Product_ID);
            _context.ComboDetails.RemoveRange(existingDetails);

            if (productInDb.IsCombo && ComponentIds != null)
            {
                var comboDetails = new List<ComboDetail>();
                for (int i = 0; i < ComponentIds.Count; i++)
                {
                    if (ComponentIds[i] > 0 && ComponentQtys[i] > 0)
                    {
                        comboDetails.Add(new ComboDetail
                        {
                            Combo_ID = product.Product_ID,
                            Product_ID = ComponentIds[i],
                            So_Luong_Thanh_Phan = ComponentQtys[i]
                        });
                    }
                }
                _context.ComboDetails.AddRange(comboDetails);
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Không thể cập nhật sản phẩm trong database. Vui lòng kiểm tra dữ liệu và thử lại.");
            ViewBag.SubCategories = new SelectList(
                _context.SubCategories.OrderBy(x => x.Ten_Danh_Muc_Con).ToList(),
                "SubCategory_ID",
                "Ten_Danh_Muc_Con",
                product.SubCategory_ID
            );
            ViewBag.AllProducts = _context.Products.Where(p => p.Product_ID != product.Product_ID).OrderBy(p => p.Ten_San_Pham).ToList();
            ViewBag.Components = _context.ComboDetails.Where(cd => cd.Combo_ID == product.Product_ID).ToList();
            return View(productInDb);
        }
    }

    // ========================
    // DELETE
    // ========================
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // ========================
    // DETAILS
    // ========================
    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products
            .Include(p => p.SubCategory)
            .Include(p => p.ComboDetails)
                .ThenInclude(cd => cd.ComponentProduct)
            .FirstOrDefaultAsync(p => p.Product_ID == id);
        if (product == null) return NotFound();
        return View(product);
    }

    private async Task<string> SaveImageAsync(IFormFile imageFile)
    {
        var extension = Path.GetExtension(imageFile.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";

        if (!Directory.Exists(_uploadFolder))
        {
            Directory.CreateDirectory(_uploadFolder);
        }

        var filePath = Path.Combine(_uploadFolder, fileName);
        await using var stream = new FileStream(filePath, FileMode.Create);
        await imageFile.CopyToAsync(stream);

        return $"/uploads/products/{fileName}";
    }
}