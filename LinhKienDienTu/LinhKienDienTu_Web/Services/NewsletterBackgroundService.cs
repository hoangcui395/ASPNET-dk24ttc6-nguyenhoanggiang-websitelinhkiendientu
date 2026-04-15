using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using LinhKienDienTu_Web.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace LinhKienDienTu_Web.Services
{
    public class NewsletterBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<NewsletterBackgroundService> _logger;

        public NewsletterBackgroundService(IServiceProvider services, ILogger<NewsletterBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Newsletter Background Service is starting.");

            // Loop until the application stops
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DoWorkAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in the Newsletter Background Service.");
                }

                // Simulate waiting for a specific period before the next blast (e.g., 24 hours).
                // For demo/testing, we'll wait 5 minutes here.
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task DoWorkAsync(CancellationToken stoppingToken)
        {
            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                // Get active subscribers
                var subscribers = await dbContext.NewsletterSubscribers
                    .Where(s => s.Trang_Thai)
                    .ToListAsync(stoppingToken);

                if (subscribers.Any())
                {
                    _logger.LogInformation($"Found {subscribers.Count} active subscribers. Preparing to blast news...");

                    string subject = "🌟 Khuyến Mãi Cực Sốc Từ Linh Kiện Điện Tử HG";
                    string htmlMessage = @"
                        <h2>Ưu đãi đặc biệt hôm nay!</h2>
                        <p>Chúng tôi vừa cập nhật rất nhiều món ngon. Nhanh tay đặt hàng để nhận khuyến mãi khủng!</p>
                        <p>Trân trọng,<br>Linh Kiện Điện Tử HG Team</p>";

                    foreach (var sub in subscribers)
                    {
                        try
                        {
                            await emailSender.SendEmailAsync(sub.Email, subject, htmlMessage);
                            // Real systems logic: Check if we haven't sent this campaign to the user today, etc.
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Could not send newsletter to {sub.Email}");
                        }
                    }

                    _logger.LogInformation("Newsletter blast completed.");
                }
            }
        }
    }
}
