using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;
using System.Linq;
using System.Threading.Tasks;

namespace LinhKienDienTu_Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var sessions = await _context.ChatSessions
                .Where(s => s.Status == "Human") // Chỉ lấy các session cần người hỗ trợ
                .Include(s => s.Messages)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(sessions);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(string sessionId)
        {
            if (!Guid.TryParse(sessionId, out Guid sessionGuid))
                return BadRequest();

            var messages = await _context.ChatMessages
                .Where(m => m.SessionId == sessionGuid)
                .OrderBy(m => m.Timestamp)
                .Select(m => new {
                    m.Sender,
                    m.Content,
                    m.Timestamp
                })
                .ToListAsync();

            return Json(messages);
        }
    }
}
