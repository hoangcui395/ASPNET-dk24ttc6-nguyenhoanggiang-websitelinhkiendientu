using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;
using LinhKienDienTu_Web.Services;

namespace LinhKienDienTu_Web.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly OrderService _orderService;
        private readonly ShippingService _shippingService;
        private readonly PaymentService _paymentService;
        private readonly ComboService _comboService;

        public CartController(ApplicationDbContext context, OrderService orderService, ShippingService shippingService, PaymentService paymentService, ComboService comboService)
        {
            _context = context;
            _orderService = orderService;
            _shippingService = shippingService;
            _paymentService = paymentService;
            _comboService = comboService;
        }

        public async Task<IActionResult> Index()
        {
            await LoadCategoriesAsync();
            var userId = GetCurrentUserId();
            var cart = await GetCartAsync(userId);

            return View(new CartIndexViewModel
            {
                Items = cart,
                Summary = BuildSummary(cart)
            });
        }

        [HttpPost]
        public async Task<IActionResult> Add(int id, int quantity = 1)
        {
            if (quantity < 1)
            {
                return BadRequest(new { success = false, message = "Số lượng không hợp lệ." });
            }

            var userId = GetCurrentUserId();
            var product = await _context.Products
                .Include(p => p.SubCategory)
                .ThenInclude(sc => sc.Category)
                .Include(p => p.Promotion)
                .FirstOrDefaultAsync(p => p.Product_ID == id);

            if (product == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy sản phẩm." });
            }

            int availableStock = product.So_Luong_Ton;
            if (product.IsCombo)
            {
                availableStock = await _comboService.CalculateComboStockAsync(product.Product_ID);
            }

            if (availableStock <= 0)
            {
                return BadRequest(new { success = false, message = "Sản phẩm đã hết hàng." });
            }

            var cartEntry = await _context.Carts
                .FirstOrDefaultAsync(c => c.User_ID == userId && c.Product_ID == id);

            var nextQuantity = quantity + (cartEntry?.So_Luong ?? 0);
            if (nextQuantity > availableStock)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Chỉ có thể thêm tối đa {availableStock} sản phẩm trong kho."
                });
            }

            if (cartEntry == null)
            {
                cartEntry = new Cart
                {
                    User_ID = userId,
                    Product_ID = product.Product_ID,
                    So_Luong = quantity,
                    Created_At = DateTime.Now,
                    Updated_At = DateTime.Now
                };

                _context.Carts.Add(cartEntry);
            }
            else
            {
                cartEntry.So_Luong = nextQuantity;
                cartEntry.Updated_At = DateTime.Now;
                _context.Carts.Update(cartEntry);
            }

            await _context.SaveChangesAsync();

            // Lấy danh sách sản phẩm gợi ý mua kèm (Cùng danh mục con)
            var suggestedProducts = await _context.Products
                .AsNoTracking()
                .Include(p => p.Promotion)
                .Where(p => p.SubCategory_ID == product.SubCategory_ID 
                         && p.Product_ID != id 
                         && p.So_Luong_Ton > 0 
                         && p.Trang_Thai != "Hết hàng")
                .Take(3)
                .ToListAsync();

            var suggestions = suggestedProducts.Select(p => new {
                id = p.Product_ID,
                name = p.Ten_San_Pham,
                image = p.Hinh_Anh ?? "/images/placeholder.png",
                price = CalculateSellingPrice(p),
                oldPrice = p.Gia_Goc,
                discount = GetActiveDiscountPercent(p)
            }).ToList();

            var cart = await GetCartAsync(userId);
            return Json(new
            {
                success = true,
                message = $"Đã thêm {quantity} x {product.Ten_San_Pham} vào giỏ hàng.",
                summary = BuildSummary(cart),
                suggestions = suggestions
            });
        }

        private static decimal GetActiveDiscountPercent(Product product)
        {
            var now = DateTime.Now;
            bool hasActivePromotion = product.Promotion != null
                && product.Promotion.Ngay_Bat_Dau <= now
                && product.Promotion.Ngay_Ket_Thuc >= now;

            return hasActivePromotion ? product.Promotion!.Phan_Tram_Giam : product.Phan_Tram_Giam;
        }

        [HttpPost]
        public async Task<IActionResult> Update(int id, int quantity)
        {
            var userId = GetCurrentUserId();
            var cartEntry = await _context.Carts
                .FirstOrDefaultAsync(c => c.User_ID == userId && c.Product_ID == id);

            if (cartEntry == null)
            {
                return NotFound(new { success = false, message = "Sản phẩm không có trong giỏ hàng." });
            }

            if (quantity <= 0)
            {
                _context.Carts.Remove(cartEntry);
                await _context.SaveChangesAsync();
                var removedSummary = BuildSummary(await GetCartAsync(userId));
                return Json(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng.", summary = removedSummary });
            }

            var product = await _context.Products
                .Include(p => p.SubCategory)
                .ThenInclude(sc => sc.Category)
                .Include(p => p.Promotion)
                .FirstOrDefaultAsync(p => p.Product_ID == id);

            if (product == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy sản phẩm." });
            }

            int availableStock = product.So_Luong_Ton;
            if (product.IsCombo)
            {
                availableStock = await _comboService.CalculateComboStockAsync(product.Product_ID);
            }

            if (quantity > availableStock)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Số lượng vượt quá tồn kho. Hiện chỉ còn {availableStock} sản phẩm."
                });
            }

            cartEntry.So_Luong = quantity;
            cartEntry.Updated_At = DateTime.Now;
            await _context.SaveChangesAsync();

            var cart = await GetCartAsync(userId);
            var item = cart.First(x => x.ProductId == id);

            return Json(new
            {
                success = true,
                message = "Cập nhật giỏ hàng thành công.",
                itemTotal = item.LineTotal,
                summary = BuildSummary(cart)
            });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = GetCurrentUserId();
            var cartEntry = await _context.Carts
                .FirstOrDefaultAsync(c => c.User_ID == userId && c.Product_ID == id);

            if (cartEntry == null)
            {
                return NotFound(new { success = false, message = "Sản phẩm không có trong giỏ hàng." });
            }

            _context.Carts.Remove(cartEntry);
            await _context.SaveChangesAsync();

            var cart = await GetCartAsync(userId);
            return Json(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng.", summary = BuildSummary(cart) });
        }

        [HttpGet]
        public async Task<IActionResult> Summary()
        {
            var userId = GetCurrentUserId();
            return Json(BuildSummary(await GetCartAsync(userId)));
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            await LoadCategoriesAsync();
            var userId = GetCurrentUserId();
            var cart = await GetCartAsync(userId);

            if (!cart.Any())
            {
                TempData["CartMessage"] = "Giỏ hàng đang trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            return View(new CheckoutViewModel
            {
                Items = cart,
                Summary = BuildSummary(cart),
                SelectedProductIds = cart.Select(c => c.ProductId).ToList()
            });
        }

        [HttpPost]
        public async Task<IActionResult> CheckoutSelected(List<int> selectedProductIds, string? InputVoucherCode)
        {
            if (selectedProductIds == null || !selectedProductIds.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            await LoadCategoriesAsync();
            var userId = GetCurrentUserId();
            var allCart = await GetCartAsync(userId);
            
            var selectedCart = allCart.Where(c => selectedProductIds.Contains(c.ProductId)).ToList();

            if (!selectedCart.Any())
            {
                TempData["Error"] = "Không tìm thấy sản phẩm đã chọn trong giỏ hàng.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CheckoutViewModel
            {
                Items = selectedCart,
                Summary = BuildSummary(selectedCart),
                SelectedProductIds = selectedProductIds,
                VoucherCode = InputVoucherCode
            };

            model.VoucherCode = model.VoucherCode?.Trim();

            if (!string.IsNullOrWhiteSpace(model.VoucherCode))
            {
                model.VoucherDiscount = await GetVoucherDiscount(model.VoucherCode, model.Summary.TotalAmount);
            }

            return View("Checkout", model);
        }

        private async Task<decimal> GetVoucherDiscount(string code, decimal subtotal)
        {
            if (string.IsNullOrWhiteSpace(code)) return 0;
            code = code.Trim();

            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Ma_Code == code);
            if (voucher == null || !voucher.IsActive || subtotal < voucher.Don_Toi_Thieu)
            {
                return 0;
            }

            decimal discount = 0;
            if (voucher.Loai_Giam_Gia == "PERCENT")
            {
                discount = subtotal * (voucher.Gia_Tri_Giam / 100m);
            }
            else
            {
                discount = voucher.Gia_Tri_Giam;
            }

            return Math.Min(discount, subtotal);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
        {
            await LoadCategoriesAsync();
            var userId = GetCurrentUserId();
            var allCart = await GetCartAsync(userId);
            
            var selectedCart = allCart.Where(c => model.SelectedProductIds.Contains(c.ProductId)).ToList();

            if (!selectedCart.Any())
            {
                TempData["CartMessage"] = "Không tìm thấy sản phẩm nào để thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                model.Items = selectedCart;
                model.Summary = BuildSummary(selectedCart);
                if (!string.IsNullOrWhiteSpace(model.VoucherCode))
                {
                    model.VoucherDiscount = await GetVoucherDiscount(model.VoucherCode, model.Summary.TotalAmount);
                }
                return View("Checkout", model);
            }

            // Tính phí ship từ ShippingService
            decimal shippingFee = _shippingService.CalculateShippingFee(model.QuanHuyen);

            // Gọi OrderService để xử lý Giỏ hàng được chọn và pass theo Voucher (nếu có)
            var result = await _orderService.ProcessCheckoutAsync(userId, model.DiaChiGiaoHang, selectedCart, model.GhiChu, model.VoucherCode, shippingFee, model.PhuongThucThanh_Toan);
            
            if (!result.Success)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                if (!result.Errors.Any())
                {
                    ModelState.AddModelError(string.Empty, result.Message);
                }

                model.Items = selectedCart;
                model.Summary = BuildSummary(selectedCart);
                if (!string.IsNullOrWhiteSpace(model.VoucherCode))
                {
                    model.VoucherDiscount = await GetVoucherDiscount(model.VoucherCode, model.Summary.TotalAmount);
                }
                return View("Checkout", model);
            }

            // Đơn hàng thành công, xóa những sản phẩm vừa thanh toán khỏi CSDL Cart trước khi redirect
            var selectedCartRows = await _context.Carts
                .Where(c => c.User_ID == userId && model.SelectedProductIds.Contains(c.Product_ID))
                .ToListAsync();
                
            if (selectedCartRows.Any()) 
            {
                _context.Carts.RemoveRange(selectedCartRows);
                await _context.SaveChangesAsync();
            }

            // Nếu thanh toán VNPay -> Chuyển hướng đến URL thanh toán
            if (model.PhuongThucThanh_Toan == "VNPay")
            {
                var paymentUrl = _paymentService.CreateVnPayPaymentUrl((int)result.OrderId, result.TotalAmount, HttpContext);
                return Redirect(paymentUrl);
            }

            TempData["CheckoutSuccess"] = result.Message;
            return RedirectToAction("MyOrders", "Order");
        }

        [HttpPost]
        public async Task<IActionResult> Reorder(int orderId)
        {
            var userId = GetCurrentUserId();
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Order_ID == orderId && o.User_ID == userId);

            if (order == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            var addedItems = 0;
            var outOfStockItems = new List<string>();

            foreach (var detail in order.OrderDetails)
            {
                if (detail.Product == null || detail.Product.Trang_Thai == "Hết hàng")
                {
                    outOfStockItems.Add(detail.Product?.Ten_San_Pham ?? "Sản phẩm không xác định");
                    continue;
                }

                int availableStock = detail.Product.So_Luong_Ton;
                if (detail.Product.IsCombo)
                {
                    availableStock = await _comboService.CalculateComboStockAsync(detail.Product_ID);
                }

                if (availableStock <= 0)
                {
                    outOfStockItems.Add(detail.Product.Ten_San_Pham);
                    continue;
                }

                var cartEntry = await _context.Carts
                    .FirstOrDefaultAsync(c => c.User_ID == userId && c.Product_ID == detail.Product_ID);

                var quantityToAdd = Math.Min(detail.So_Luong, availableStock);

                if (cartEntry == null)
                {
                    cartEntry = new Cart
                    {
                        User_ID = userId,
                        Product_ID = detail.Product_ID,
                        So_Luong = quantityToAdd,
                        Created_At = DateTime.Now,
                        Updated_At = DateTime.Now
                    };
                    _context.Carts.Add(cartEntry);
                }
                else
                {
                    cartEntry.So_Luong = Math.Min(cartEntry.So_Luong + quantityToAdd, availableStock);
                    cartEntry.Updated_At = DateTime.Now;
                    _context.Carts.Update(cartEntry);
                }
                addedItems++;
            }

            await _context.SaveChangesAsync();

            if (outOfStockItems.Any())
            {
                TempData["CartMessage"] = $"Đã thêm {addedItems} sản phẩm vào giỏ. Một số sản phẩm đã hết hàng: {string.Join(", ", outOfStockItems)}";
            }
            else
            {
                TempData["CartMessage"] = $"Đã thêm tất cả sản phẩm từ đơn hàng #{orderId} vào giỏ hàng.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ApplyVoucher([FromBody] string voucherCode)
        {
            if (string.IsNullOrWhiteSpace(voucherCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã voucher." });
            }

            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Ma_Code == voucherCode);
            if (voucher == null)
            {
                return Json(new { success = false, message = "Mã voucher không tồn tại." });
            }

            if (!voucher.IsActive)
            {
                return Json(new { success = false, message = "Mã voucher đã hết hạn hoặc hết lượt sử dụng." });
            }

            return Json(new 
            { 
                success = true, 
                message = "Áp dụng thành công!",
                type = voucher.Loai_Giam_Gia,
                value = voucher.Gia_Tri_Giam
            });
        }

        private async Task<List<CartItem>> GetCartAsync(int userId)
        {
            var cartRows = await _context.Carts
                .Where(c => c.User_ID == userId)
                .Include(c => c.Product)
                .ThenInclude(p => p.SubCategory)
                .ThenInclude(sc => sc.Category)
                .Include(c => c.Product)
                .ThenInclude(p => p.Promotion)
                .AsNoTracking()
                .ToListAsync();

            return cartRows.Select(c => new CartItem
                {
                    ProductId = c.Product_ID,
                    Name = c.Product.Ten_San_Pham,
                    Price = CalculateSellingPrice(c.Product),
                    OriginalPrice = c.Product.Gia_Goc,
                    Image = c.Product.Hinh_Anh ?? string.Empty,
                    Quantity = c.So_Luong,
                    IsTphcmOnly = IsTphcmOnly(c.Product),
                    AvailableStock = c.Product.So_Luong_Ton
                })
                .ToList();
        }

        private CartSummaryViewModel BuildSummary(List<CartItem> cart)
        {
            var original = cart.Sum(x => x.OriginalPrice * x.Quantity);
            var discounted = cart.Sum(x => x.LineTotal);
            return new CartSummaryViewModel
            {
                TotalItems = cart.Sum(x => x.Quantity),
                TotalAmount = discounted,
                TotalOriginalAmount = original,
                TotalPromotionDiscount = original - discounted,
                HasTphcmOnlyItems = cart.Any(x => x.IsTphcmOnly)
            };
        }

        private async Task LoadCategoriesAsync()
        {
            ViewData["Categories"] = await _context.Categories
                .Include(c => c.SubCategories)
                .ToListAsync();
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue("UserID");
            if (!int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Người dùng chưa được xác thực hợp lệ.");
            }

            return userId;
        }

        private static bool IsTphcmOnly(Product product)
        {
            var categoryName = product.SubCategory?.Category?.Ten_Danh_Muc?.ToLowerInvariant() ?? string.Empty;
            return categoryName.Contains("tuoi") ||
                   categoryName.Contains("tươi") ||
                   categoryName.Contains("lanh") ||
                   categoryName.Contains("lạnh");
        }

        private static decimal CalculateSellingPrice(Product product)
        {
            decimal discountPercent;

            var now = DateTime.Now;
            bool hasActivePromotion = product.Promotion != null
                && product.Promotion.Ngay_Bat_Dau <= now
                && product.Promotion.Ngay_Ket_Thuc >= now;

            if (hasActivePromotion)
            {
                discountPercent = product.Promotion!.Phan_Tram_Giam;
            }
            else
            {
                discountPercent = product.Phan_Tram_Giam;
            }

            discountPercent = Math.Min(discountPercent, 100m);
            var finalPrice = product.Gia_Goc * (1 - discountPercent / 100m);
            return finalPrice < 0 ? 0 : Math.Round(finalPrice, 0);
        }
    }
}
