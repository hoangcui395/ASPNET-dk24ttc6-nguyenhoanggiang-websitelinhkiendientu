using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinhKienDienTu_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChatbotController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("init")]
        public async Task<IActionResult> InitSession([FromBody] ChatInitRequest request)
        {
            var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.GuestOrUserId == request.UserId && s.Status != "Closed");
            if (session == null)
            {
                session = new ChatSession
                {
                    GuestOrUserId = request.UserId,
                    Status = "Bot"
                };
                _context.ChatSessions.Add(session);
                await _context.SaveChangesAsync();
            }

            return Ok(new { sessionId = session.SessionId, status = session.Status });
        }

        [HttpPost("message")]
        public async Task<IActionResult> ProcessMessage([FromBody] ChatMessageRequest request)
        {
            if (!Guid.TryParse(request.SessionId, out Guid sessionGuid))
                return BadRequest("Invalid SessionId");

            var session = await _context.ChatSessions.FindAsync(sessionGuid);
            if (session == null) return NotFound();

            // Lưu tin nhắn người dùng
            var userMsg = new ChatMessage
            {
                SessionId = sessionGuid,
                Sender = "User",
                Content = request.Message,
                Timestamp = DateTime.Now
            };
            _context.ChatMessages.Add(userMsg);
            await _context.SaveChangesAsync();

            if (session.Status == "Human")
            {
                return Ok(new { type = "Human", message = "Đang kết nối với tư vấn viên..." });
            }

            // Logic Bot (Level 1 & 2)
            var response = await GetBotResponse(request.Message);
            
            // Lưu tin nhắn Bot
            var botMsg = new ChatMessage
            {
                SessionId = sessionGuid,
                Sender = "Bot",
                Content = response.Message,
                Timestamp = DateTime.Now
            };
            _context.ChatMessages.Add(botMsg);
            await _context.SaveChangesAsync();

            return Ok(response);
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> TransferToHuman([FromBody] string sessionId)
        {
            if (!Guid.TryParse(sessionId, out Guid sessionGuid))
                return BadRequest("Invalid SessionId");

            var session = await _context.ChatSessions.FindAsync(sessionGuid);
            if (session == null) return NotFound();

            session.Status = "Human";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Yêu cầu đã được gửi tới nhân viên hỗ trợ." });
        }

        private async Task<BotResponse> GetBotResponse(string input)
        {
            input = input.ToLower();

            // Cấp độ 1: FAQ
            if (input.Contains("quà tặng") || input.Contains("khuyến mãi"))
            {
                return new BotResponse { Message = "Sản phẩm có quà tặng, hãy đảm bảo bạn chọn đủ số lượng tối thiểu để nhận được quà kèm theo nhé! 🎁" };
            }
            if (input.Contains("tphcm") || input.Contains("hồ chí minh") || input.Contains("giao hàng"))
            {
                return new BotResponse { Message = "Các sản phẩm [CHỈ GIAO HÀNG TẠI TP.HCM] là hàng đông lạnh/tươi sống, cần bảo quản khắt khe nên chưa thể giao tỉnh xa được ạ. Mong bạn thông cảm! 🧊" };
            }
            if (input.Contains("đổi trả"))
            {
                return new BotResponse { Message = "Linh Kiện Điện Tử HG hỗ trợ đổi trả trong vòng 24h đối với hàng tươi sống nếu có lỗi từ nhà sản xuất ạ. 🔄" };
            }

            // Cấp độ 2: Tư vấn mua hàng & Tồn kho
            if (input.Contains("cho bé") || input.Contains("em bé"))
            {
                var products = await _context.Products
                    .Where(p => p.Ten_San_Pham.Contains("Cháo") || p.Ten_San_Pham.Contains("Baby") || p.SubCategory.Ten_Danh_Muc_Con.Contains("Cháo"))
                    .Take(3)
                    .ToListAsync();
                
                string list = "Dạ, Linh Kiện Điện Tử HG có các sản phẩm dinh dưỡng cho bé như:\n" + string.Join("\n", products.Select(p => $"- {p.Ten_San_Pham} ({p.Gia_Goc.ToString("N0")}đ)"));
                return new BotResponse { Message = list, IsProductList = true, Products = products.Select(p => new { p.Product_ID, p.Ten_San_Pham, p.Hinh_Anh }).ToList<object>() };
            }

            if (input.Contains("ăn kiêng") || input.Contains("giảm cân"))
            {
                var products = await _context.Products
                    .Where(p => p.Ten_San_Pham.Contains("Gạo lứt") || p.Ten_San_Pham.Contains("Ức gà") || p.SubCategory.Ten_Danh_Muc_Con.Contains("Eat Clean"))
                    .Take(3)
                    .ToListAsync();

                string list = "🌿 Thực đơn Eat Clean cho bạn đây ạ:\n" + string.Join("\n", products.Select(p => $"- {p.Ten_San_Pham} ({p.Gia_Goc.ToString("N0")}đ)"));
                return new BotResponse { Message = list, IsProductList = true, Products = products.Select(p => new { p.Product_ID, p.Ten_San_Pham, p.Hinh_Anh }).ToList<object>() };
            }

            // Kiểm tra tồn kho cụ thể
            if (input.Contains("còn") && (input.Contains("không") || input.Contains("ko")))
            {
                var keyword = input.Replace("còn", "").Replace("không", "").Replace("ko", "").Replace("?", "").Trim();
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Ten_San_Pham.Contains(keyword));
                if (product != null)
                {
                    if (product.So_Luong_Ton > 0)
                        return new BotResponse { Message = $"Dạ, {product.Ten_San_Pham} vẫn còn hàng (còn {product.So_Luong_Ton} sản phẩm) ạ. Bạn đặt ngay nhé! ✅" };
                    else
                        return new BotResponse { Message = $"Rất tiếc, {product.Ten_San_Pham} hiện đang tạm hết hàng ạ. ❌" };
                }
            }

            return new BotResponse { Message = "Xin lỗi, tôi chưa hiểu ý bạn. Bạn có thể hỏi về 'khuyến mãi', 'giao hàng hcm', hoặc các món 'cho bé', 'ăn kiêng' được không ạ? Ngoài ra bạn có thể nhấn nút 'Gặp nhân viên' để được hỗ trợ trực tiếp." };
        }
    }

    public class ChatInitRequest { public string UserId { get; set; } }
    public class ChatMessageRequest { public string SessionId { get; set; } public string Message { get; set; } }
    public class BotResponse { 
        public string Message { get; set; } 
        public bool IsProductList { get; set; } = false;
        public List<object> Products { get; set; }
    }
}
