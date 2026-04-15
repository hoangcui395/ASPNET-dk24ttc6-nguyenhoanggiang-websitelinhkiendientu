using System;
using System.Collections.Generic;

namespace LinhKienDienTu_Web.Models
{
    public class AdminAnalyticsViewModel
    {
        // 1. Finance & Promotion
        public decimal TotalRevenue { get; set; }
        public decimal TotalPromotionCost { get; set; }
        public List<MonthlyStats> RevenueChartData { get; set; } = new();

        // 2. Abandoned Carts
        public List<AbandonedCartInfo> AbandonedCarts { get; set; } = new();
        public int AbandonedCartCount => AbandonedCarts.Count;

        // 3. Stock Prediction
        public List<StockPredictionInfo> StockPredictions { get; set; } = new();

        // Standard stats
        public int PendingOrders { get; set; }
        public int LowStockCount { get; set; }
        public int NewUsersCount { get; set; }
        
        // 4. Golden Hour Heatmap
        public List<HeatmapPoint> HeatmapChartData { get; set; } = new();

        // 5. Top Selling Products
        public List<TopProductInfo> TopSellingProducts { get; set; } = new();

        // 6. Payment Method Distribution
        public List<PaymentMethodStat> PaymentStats { get; set; } = new();
    }

    public class HeatmapPoint
    {
        public int Day { get; set; } // 0=Sun, 1=Mon, ..., 6=Sat
        public int Hour { get; set; } // 0-23
        public int Count { get; set; }
    }

    public class TopProductInfo
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class PaymentMethodStat
    {
        public string MethodName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class MonthlyStats
    {
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal PromoCost { get; set; }
    }

    public class AbandonedCartInfo
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<string> ProductNames { get; set; } = new();
    }

    public class StockPredictionInfo
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public double SalesVelocity { get; set; } // Units per day
        public double DaysRemaining { get; set; }
        public string RiskLevel { get; set; } = "Low"; // Low, Medium, High (Urgent)
    }
}
