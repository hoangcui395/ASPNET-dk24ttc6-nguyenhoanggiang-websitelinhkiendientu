using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;
using LinhKienDienTu_Web.Services;

public class OrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ComboService _comboService;

    public OrderService(ApplicationDbContext context, ComboService comboService)
    {
        _context = context;
        _comboService = comboService;
    }

    // ══════════════════════════════════════════════════════════
    // PHÍA NHÂN VIÊN (Client-facing)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Ghi nhận đơn hàng ở trạng thái "Chờ duyệt".
    /// KHÔNG trừ tồn kho — tồn kho chỉ bị trừ khi Admin xác nhận.
    /// </summary>
    public async Task<CheckoutResult> ProcessCheckoutAsync(
        int userId,
        string diaChi,
        List<CartItem> cartItems,
        string? ghiChu = null,
        string? voucherCode = null,
        decimal shippingFee = 0,
        string paymentMethod = "COD")
    {
        if (cartItems == null || !cartItems.Any())
        {
            return new CheckoutResult
            {
                Message = "Giỏ hàng đang trống.",
                Errors = new List<string> { "Không có sản phẩm nào để đặt hàng." }
            };
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var productIds = cartItems.Select(x => x.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Include(p => p.SubCategory)
                .ThenInclude(sc => sc!.Category)
            .Include(p => p.Promotion)
            .Where(p => productIds.Contains(p.Product_ID))
            .ToListAsync();

        var productMap = products.ToDictionary(p => p.Product_ID);
        var errors = new List<string>();

        foreach (var item in cartItems)
        {
            if (!productMap.ContainsKey(item.ProductId))
                errors.Add($"Sản phẩm ID {item.ProductId} không còn tồn tại.");
        }

        if (errors.Any())
        {
            await transaction.RollbackAsync();
            return new CheckoutResult { Message = "Đặt hàng thất bại.", Errors = errors };
        }

        // --- NEW: Early Stock Check (Prevent Trigger Error) ---
        foreach (var item in cartItems)
        {
            var product = productMap[item.ProductId];
            int availableStock = product.So_Luong_Ton;
            
            if (product.IsCombo)
            {
                availableStock = await _comboService.CalculateComboStockAsync(product.Product_ID);
            }

            if (availableStock < item.Quantity)
            {
                await transaction.RollbackAsync();
                return new CheckoutResult 
                { 
                    Message = "Sản phẩm hiện không đủ tồn kho.", 
                    Errors = new List<string> { $"Sản phẩm '{product.Ten_San_Pham}' hiện chỉ còn {availableStock} món." } 
                };
            }
        }

        // --- NEW: Geographic Restriction Check ---
        var isTphcmAddress = diaChi.Contains("Hồ Chí Minh") || diaChi.Contains("TP.HCM") || diaChi.Contains("Thành phố Hồ Chí Minh");
        
        foreach (var item in cartItems)
        {
            var product = productMap[item.ProductId];
            if (product.IsTphcmOnly && !isTphcmAddress)
            {
                await transaction.RollbackAsync();
                return new CheckoutResult 
                { 
                    Message = "Hạn chế địa lý.", 
                    Errors = new List<string> { $"Sản phẩm '{product.Ten_San_Pham}' chỉ giao tại TP.HCM. Vui lòng kiểm tra địa chỉ." } 
                };
            }
        }
        // ------------------------------------------

        Voucher? appliedVoucher = null;
        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            var trimmedCode = voucherCode.Trim();
            appliedVoucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Ma_Code == trimmedCode);
            if (appliedVoucher == null || !appliedVoucher.IsActive)
            {
                await transaction.RollbackAsync();
                return new CheckoutResult { Message = "Voucher không hợp lệ hoặc đã hết lượt sử dụng.", Errors = new List<string> { "Voucher invalid" } };
            }

            // --- NEW: Don_Toi_Thieu Check ---
            decimal currentTotalBeforeVoucher = cartItems.Sum(x => CalculateSellingPrice(productMap[x.ProductId]) * x.Quantity);
            if (currentTotalBeforeVoucher < appliedVoucher.Don_Toi_Thieu)
            {
                await transaction.RollbackAsync();
                return new CheckoutResult 
                { 
                    Message = "Đơn hàng không đủ điều kiện dùng Voucher.", 
                    Errors = new List<string> { $"Voucher này yêu cầu đơn hàng từ {appliedVoucher.Don_Toi_Thieu:N0}đ." } 
                };
            }
        }

        // Tạo đơn hàng — trạng thái "Chờ duyệt"
        var order = new Order
        {
            User_ID = userId,
            Ngay_Dat = DateTime.Now,
            Dia_Chi_Giao_Hang = diaChi,
            Phi_Ship = shippingFee,
            Phuong_Thuc_Thanh_Toan = paymentMethod,
            Trang_Thai_Don_Hang = "Chờ xác nhận",
            Trang_Thai_Thanh_Toan = paymentMethod == "VNPay" ? "Đang chờ thanh toán" : "Chưa thanh toán",
            Ghi_Chu = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim(),
            Tong_Tien = 0m,
            Voucher_ID = appliedVoucher?.Voucher_ID
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(); // lấy Order_ID

        // Tạo Order_Detail (chỉ ghi nhận, KHÔNG trừ kho)
        var orderDetails = new List<OrderDetail>();
        decimal totalAmount = 0m;
        decimal tienTietKiemKhuyenMai = 0m;

        foreach (var item in cartItems)
        {
            var product = productMap[item.ProductId];
            var sellingPrice = CalculateSellingPrice(product);
            
            // Tính tiền tiết kiệm riêng từng món nhờ Promotion/BaseDiscount
            tienTietKiemKhuyenMai += (product.Gia_Goc - sellingPrice) * item.Quantity;

            orderDetails.Add(new OrderDetail
            {
                Order_ID = order.Order_ID,
                Product_ID = product.Product_ID,
                So_Luong = item.Quantity,
                Gia_Ban = sellingPrice
            });

            totalAmount += sellingPrice * item.Quantity;
        }

        decimal tienTietKiemVoucher = 0m;
        // Áp dụng giảm giá từ Voucher (trên tổng tiền bill)
        if (appliedVoucher != null)
        {
            if (appliedVoucher.Loai_Giam_Gia == "PERCENT")
            {
                tienTietKiemVoucher = totalAmount * (appliedVoucher.Gia_Tri_Giam / 100m);
            }
            else // AMOUNT
            {
                tienTietKiemVoucher = appliedVoucher.Gia_Tri_Giam;
            }

            // Đảm bảo không giảm quá tổng bill
            tienTietKiemVoucher = Math.Min(tienTietKiemVoucher, totalAmount);
            totalAmount -= tienTietKiemVoucher;

            // Tăng số lượng đã dùng
            appliedVoucher.So_Luong_Da_Dung += 1;
        }

        _context.Order_Details.AddRange(orderDetails);
        order.Tong_Tien = totalAmount + shippingFee; // Tổng tiền = Tiền hàng + Phí ship
        order.Tien_Tiet_Kiem_Khuyen_Mai = tienTietKiemKhuyenMai;
        order.Tien_Tiet_Kiem_Voucher = tienTietKiemVoucher;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new CheckoutResult
        {
            Success = true,
            Message = $"Đặt hàng thành công! Đơn #{order.Order_ID} đang chờ xác nhận.",
            OrderId = order.Order_ID,
            TotalAmount = order.Tong_Tien
        };
    }

    /// <summary>
    /// Hủy đơn hàng từ phía nhân viên.
    /// Nếu đơn đã ở "Đã xác nhận" → hoàn kho.
    /// Nếu chỉ "Chờ duyệt" → không có gì để hoàn.
    /// </summary>
    public async Task<(bool Success, string Message)> CancelOrderAsync(int orderId, int userId, string lyDoHuy)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Order_ID == orderId && o.User_ID == userId);

        if (order == null)
            return (false, "Không tìm thấy đơn hàng hoặc bạn không có quyền hủy đơn này.");

        // Nếu đã xác nhận (đã trừ kho)
        if (order.Trang_Thai_Don_Hang == "Đang lấy hàng")
        {
            await RestoreStockAsync(order.OrderDetails);
        }

        order.Trang_Thai_Don_Hang = "Đã hủy";
        order.Ly_Do_Huy = string.IsNullOrWhiteSpace(lyDoHuy) ? "Không có lý do" : lyDoHuy.Trim();

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true, $"Đơn hàng #{orderId} đã được hủy thành công.");
    }

    // ══════════════════════════════════════════════════════════
    // PHÍA ADMIN (Order Management)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Admin xác nhận đơn hàng: kiểm tra kho → trừ kho → đổi trạng thái "Đã xác nhận".
    /// Transaction đảm bảo rollback nếu kho không đủ (race condition safe).
    /// </summary>
    public async Task<(bool Success, string Message, List<string> Errors)> ConfirmOrderAsync(int orderId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Order_ID == orderId);

        if (order == null)
            return (false, "Không tìm thấy đơn hàng.", new List<string>());

        if (order.Trang_Thai_Don_Hang != "Chờ xác nhận")
            return (false, $"Đơn hàng đang ở trạng thái '{order.Trang_Thai_Don_Hang}', không thể xác nhận.", new List<string>());

        // Bước 1: Kiểm tra tồn kho
        var stockErrors = new List<string>();
        foreach (var detail in order.OrderDetails)
        {
            if (detail.Product == null)
            {
                stockErrors.Add($"Sản phẩm ID {detail.Product_ID} không còn tồn tại trong hệ thống.");
                continue;
            }

            if (detail.Product.IsCombo)
            {
                // Load components
                var comboDetails = await _context.ComboDetails
                    .Include(cd => cd.ComponentProduct)
                    .Where(cd => cd.Combo_ID == detail.Product_ID)
                    .ToListAsync();

                foreach (var component in comboDetails)
                {
                    if (component.ComponentProduct == null) continue;
                    var requiredQty = component.So_Luong_Thanh_Phan * detail.So_Luong;
                    if (component.ComponentProduct.So_Luong_Ton < requiredQty)
                    {
                        stockErrors.Add($"Combo '{detail.Product.Ten_San_Pham}' thiếu thành phần '{component.ComponentProduct.Ten_San_Pham}': cần {requiredQty}, còn {component.ComponentProduct.So_Luong_Ton}.");
                    }
                }
            }
            else if (detail.Product.So_Luong_Ton < detail.So_Luong)
            {
                stockErrors.Add(
                    $"'{detail.Product.Ten_San_Pham}': cần {detail.So_Luong}, còn {detail.Product.So_Luong_Ton} trong kho.");
            }
        }

        if (stockErrors.Any())
        {
            await transaction.RollbackAsync();
            return (false, "Tồn kho không đủ để xác nhận đơn hàng này.", stockErrors);
        }

        // Bước 2: Trừ tồn kho và đánh dấu Hết hàng nếu về 0
        foreach (var detail in order.OrderDetails)
        {
            if (detail.Product == null) continue;

            if (detail.Product.IsCombo)
            {
                var comboDetails = await _context.ComboDetails
                    .Include(cd => cd.ComponentProduct)
                    .Where(cd => cd.Combo_ID == detail.Product_ID)
                    .ToListAsync();

                foreach (var component in comboDetails)
                {
                    if (component.ComponentProduct == null) continue;
                    component.ComponentProduct.So_Luong_Ton -= (component.So_Luong_Thanh_Phan * detail.So_Luong);
                    if (component.ComponentProduct.So_Luong_Ton <= 0)
                    {
                        component.ComponentProduct.So_Luong_Ton = 0;
                        component.ComponentProduct.Trang_Thai = "Hết hàng";
                    }
                }
            }
            else
            {
                detail.Product.So_Luong_Ton -= detail.So_Luong;

                if (detail.Product.So_Luong_Ton <= 0)
                {
                    detail.Product.So_Luong_Ton = 0;
                    detail.Product.Trang_Thai = "Hết hàng";
                }
            }
        }

        // Bước 3: Đổi trạng thái đơn hàng
        order.Trang_Thai_Don_Hang = "Đang lấy hàng";

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true, $"Đơn hàng #{orderId} đã được xác nhận. Tồn kho đã được cập nhật.", new List<string>());
    }

    /// <summary>Admin cập nhật trạng thái đơn hàng (Đang giao / Hoàn thành).</summary>
    public async Task<(bool Success, string Message)> UpdateOrderStatusAsync(int orderId, string newStatus)
    {
        var validTransitions = new Dictionary<string, List<string>>
        {
            ["Đang lấy hàng"] = new() { "Đang giao" },
            ["Đang giao"] = new() { "Thành công" }
        };

        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) return (false, "Không tìm thấy đơn hàng.");

        if (!validTransitions.TryGetValue(order.Trang_Thai_Don_Hang, out var allowedNext) ||
            !allowedNext.Contains(newStatus))
        {
            return (false, $"Không thể chuyển từ '{order.Trang_Thai_Don_Hang}' sang '{newStatus}'.");
        }

        order.Trang_Thai_Don_Hang = newStatus;
        await _context.SaveChangesAsync();
        return (true, $"Đơn hàng #{orderId} đã chuyển sang '{newStatus}'.");
    }

    /// <summary>
    /// Admin hủy đơn hàng — hoàn kho nếu đã xác nhận.
    /// </summary>
    public async Task<(bool Success, string Message)> AdminCancelOrderAsync(int orderId, string lyDoHuy)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Order_ID == orderId);

        if (order == null)
            return (false, "Không tìm thấy đơn hàng.");

        if (order.Trang_Thai_Don_Hang == "Thành công" || order.Trang_Thai_Don_Hang == "Đã hủy")
            return (false, $"Không thể hủy đơn ở trạng thái '{order.Trang_Thai_Don_Hang}'.");

        // Hoàn kho nếu đã trừ
        if (order.Trang_Thai_Don_Hang == "Đang lấy hàng" || order.Trang_Thai_Don_Hang == "Đang giao")
        {
            await RestoreStockAsync(order.OrderDetails);
        }

        order.Trang_Thai_Don_Hang = "Đã hủy";
        order.Ly_Do_Huy = string.IsNullOrWhiteSpace(lyDoHuy) ? "Admin hủy" : lyDoHuy.Trim();

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true, $"Đơn hàng #{orderId} đã được hủy và tồn kho đã hoàn lại.");
    }

    // ══════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════

    private async Task RestoreStockAsync(IEnumerable<OrderDetail> details)
    {
        var context = _context; // Use injected context
        foreach (var detail in details)
        {
            if (detail.Product == null) continue;

            if (detail.Product.IsCombo)
            {
                var comboDetails = await context.ComboDetails
                    .Include(cd => cd.ComponentProduct)
                    .Where(cd => cd.Combo_ID == detail.Product_ID)
                    .ToListAsync();

                foreach (var component in comboDetails)
                {
                    if (component.ComponentProduct == null) continue;
                    component.ComponentProduct.So_Luong_Ton += (component.So_Luong_Thanh_Phan * detail.So_Luong);
                    if (component.ComponentProduct.Trang_Thai == "Hết hàng" && component.ComponentProduct.So_Luong_Ton > 0)
                        component.ComponentProduct.Trang_Thai = "Còn hàng";
                }
            }
            else
            {
                detail.Product.So_Luong_Ton += detail.So_Luong;

                if (detail.Product.Trang_Thai == "Hết hàng" && detail.Product.So_Luong_Ton > 0)
                    detail.Product.Trang_Thai = "Còn hàng";
            }
        }
    }

    /// <summary>
    /// Ưu tiên % giảm từ Promotion nếu còn hiệu lực, ngược lại dùng Phan_Tram_Giam của sản phẩm.
    /// Không cộng dồn cả 2.
    /// </summary>
    public static decimal CalculateSellingPrice(Product product)
    {
        var now = DateTime.Now;
        bool hasActivePromotion = product.Promotion != null
            && product.Promotion.Ngay_Bat_Dau <= now
            && product.Promotion.Ngay_Ket_Thuc >= now;

        var discountPercent = hasActivePromotion
            ? product.Promotion!.Phan_Tram_Giam
            : product.Phan_Tram_Giam;

        discountPercent = Math.Min(discountPercent, 100m);
        var finalPrice = product.Gia_Goc * (1 - discountPercent / 100m);
        return finalPrice < 0 ? 0 : Math.Round(finalPrice, 0);
    }
}
