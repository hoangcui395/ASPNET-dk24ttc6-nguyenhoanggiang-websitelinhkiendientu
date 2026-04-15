using Microsoft.AspNetCore.SignalR;
using LinhKienDienTu_Web.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace LinhKienDienTu_Web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        // Người dùng gửi tin nhắn (khi đang ở chế độ Human)
        public async Task SendMessage(string sessionId, string message)
        {
            if (Guid.TryParse(sessionId, out Guid sessionGuid))
            {
                var msg = new ChatMessage
                {
                    SessionId = sessionGuid,
                    Sender = "User",
                    Content = message,
                    Timestamp = DateTime.Now
                };
                _context.ChatMessages.Add(msg);
                await _context.SaveChangesAsync();

                // Gửi tới Admin
                await Clients.Group("Admins").SendAsync("ReceiveMessage", sessionId, "User", message);
            }
        }

        // Admin gửi tin nhắn cho người dùng
        public async Task AdminSendMessage(string sessionId, string message)
        {
            if (Guid.TryParse(sessionId, out Guid sessionGuid))
            {
                var msg = new ChatMessage
                {
                    SessionId = sessionGuid,
                    Sender = "Admin",
                    Content = message,
                    Timestamp = DateTime.Now
                };
                _context.ChatMessages.Add(msg);
                await _context.SaveChangesAsync();

                // Gửi tới Người dùng (dựa trên sessionId làm group name)
                await Clients.Group(sessionId).SendAsync("ReceiveMessage", sessionId, "Admin", message);
            }
        }

        // Người dùng tham gia group của chính mình
        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }

        // Admin tham gia group Admins để nhận thông báo
        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }
    }
}
