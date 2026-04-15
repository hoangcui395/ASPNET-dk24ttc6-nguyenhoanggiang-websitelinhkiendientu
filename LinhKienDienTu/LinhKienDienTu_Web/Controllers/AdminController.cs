using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string range = "7d")
    {
        var now = DateTime.Now;
        DateTime fromDate = range switch
        {
            "today" => now.Date,
            "14d" => now.Date.AddDays(-14),
            "30d" => now.Date.AddDays(-30),
            _ => now.Date.AddDays(-7)
        };

        var model = new AdminAnalyticsViewModel();

        // 1. Quick Stats
        model.PendingOrders = await _context.Orders.CountAsync(o => o.Trang_Thai_Don_Hang == "Chờ xác nhận");
        model.LowStockCount = await _context.Products.CountAsync(p => p.So_Luong_Ton < 10);
        
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        model.NewUsersCount = await _context.Users.CountAsync(u => u.Created_At >= startOfMonth);

        // 2. Financial Analytics (Promotion & Profit Margin)
        var successfulOrders = await _context.Orders
            .Where(o => o.Ngay_Dat >= fromDate && o.Trang_Thai_Don_Hang == "Thành công")
            .ToListAsync();

        model.TotalRevenue = successfulOrders.Sum(o => o.Tong_Tien);
        model.TotalPromotionCost = successfulOrders.Sum(o => o.Tien_Tiet_Kiem_Khuyen_Mai + o.Tien_Tiet_Kiem_Voucher);

        // Chart Data: Total vs Promo
        var dailyStats = successfulOrders
            .GroupBy(o => o.Ngay_Dat.Date)
            .Select(g => new MonthlyStats
            {
                Label = g.Key.ToString("dd/MM"),
                Revenue = g.Sum(o => o.Tong_Tien),
                PromoCost = g.Sum(o => o.Tien_Tiet_Kiem_Khuyen_Mai + o.Tien_Tiet_Kiem_Voucher)
            })
            .OrderBy(x => x.Label)
            .ToList();
        model.RevenueChartData = dailyStats;

        // 3. Abandoned Carts Tracker (> 24h)
        var cutoff = now.AddDays(-1);
        var abandonedItems = await _context.Carts
            .Include(c => c.User)
            .Include(c => c.Product)
            .Where(c => c.Updated_At < cutoff)
            .ToListAsync();

        model.AbandonedCarts = abandonedItems
            .GroupBy(c => c.User_ID)
            .Select(g => new AbandonedCartInfo
            {
                UserId = g.Key,
                UserName = g.First().User?.Ho_Ten ?? "Khách",
                UserEmail = g.First().User?.Email ?? "—",
                ItemCount = g.Sum(x => x.So_Luong),
                TotalValue = g.Sum(x => x.So_Luong * x.Product.Gia_Goc), // Calculating potential value
                LastUpdated = g.Max(x => x.Updated_At),
                ProductNames = g.Select(x => x.Product.Ten_San_Pham).Take(3).ToList()
            })
            .OrderByDescending(x => x.LastUpdated)
            .ToList();

        // 4. Out-of-stock Predictor (Sales Velocity)
        // Last 7 days units sold per product
        var velocityCutoff = now.AddDays(-7);
        var recentSales = await _context.Order_Details
            .Include(od => od.Order)
            .Where(od => od.Order.Ngay_Dat >= velocityCutoff && od.Order.Trang_Thai_Don_Hang == "Thành công")
            .GroupBy(od => od.Product_ID)
            .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(x => x.So_Luong) })
            .ToListAsync();

        var products = await _context.Products
            .Where(p => p.So_Luong_Ton < 50) // Only predict for relatively lower stock
            .ToListAsync();

        foreach (var p in products)
        {
            var sold = recentSales.FirstOrDefault(s => s.ProductId == p.Product_ID)?.TotalSold ?? 0;
            double velocity = sold / 7.0; // Units per day
            
            if (velocity > 0 || p.So_Luong_Ton < 5)
            {
                double daysLeft = velocity > 0 ? p.So_Luong_Ton / velocity : 999;
                
                string risk = "Low";
                if (daysLeft <= 2 || p.So_Luong_Ton == 0) risk = "Urgent";
                else if (daysLeft <= 5) risk = "Medium";

                model.StockPredictions.Add(new StockPredictionInfo
                {
                    ProductId = p.Product_ID,
                    ProductName = p.Ten_San_Pham,
                    Image = p.Hinh_Anh,
                    CurrentStock = p.So_Luong_Ton,
                    SalesVelocity = Math.Round(velocity, 2),
                    DaysRemaining = Math.Round(daysLeft, 1),
                    RiskLevel = risk
                });
            }
        }
        model.StockPredictions = model.StockPredictions.OrderBy(x => x.DaysRemaining).Take(10).ToList();
        
        // 5. Golden Hour Heatmap (Frequency by Day and Hour)
        var allOrdersInRange = await _context.Orders
            .Where(o => o.Ngay_Dat >= fromDate && o.Trang_Thai_Don_Hang != "Đã hủy")
            .Select(o => new { o.Ngay_Dat })
            .ToListAsync();

        model.HeatmapChartData = allOrdersInRange
            .GroupBy(o => new { Day = (int)o.Ngay_Dat.DayOfWeek, Hour = o.Ngay_Dat.Hour })
            .Select(g => new HeatmapPoint
            {
                Day = g.Key.Day,
                Hour = g.Key.Hour,
                Count = g.Count()
            })
            .ToList();

        // 6. Top Selling Products
        model.TopSellingProducts = await _context.Order_Details
            .Include(od => od.Order)
            .Include(od => od.Product)
            .Where(od => od.Order.Ngay_Dat >= fromDate && od.Order.Trang_Thai_Don_Hang == "Thành công")
            .GroupBy(od => new { od.Product_ID, od.Product.Ten_San_Pham, od.Product.Hinh_Anh })
            .Select(g => new TopProductInfo
            {
                ProductId = g.Key.Product_ID,
                Name = g.Key.Ten_San_Pham,
                Image = g.Key.Hinh_Anh,
                TotalSold = g.Sum(x => x.So_Luong),
                TotalRevenue = g.Sum(x => x.Gia_Ban * x.So_Luong)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(5)
            .ToListAsync();

        // 7. Payment Method Distribution
        model.PaymentStats = successfulOrders
            .GroupBy(o => o.Phuong_Thuc_Thanh_Toan)
            .Select(g => new PaymentMethodStat
            {
                MethodName = g.Key,
                Count = g.Count(),
                TotalValue = g.Sum(o => o.Tong_Tien)
            })
            .ToList();

        ViewBag.Range = range;
        return View(model);
    }

    [HttpPost]
    public IActionResult SendReminder(int userId)
    {
        // Mock function: Simulate sending email
        // In a real app, this would use ISmtpService
        TempData["Success"] = $"Đã gửi email nhắc nhở thanh toán đến người dùng #{userId} thành công!";
        return RedirectToAction("Index");
    }
}

