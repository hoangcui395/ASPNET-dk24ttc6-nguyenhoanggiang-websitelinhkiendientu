using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace LinhKienDienTu_Web.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var server = smtpSettings["Server"]!;
            var port = int.Parse(smtpSettings["Port"]!);
            var senderName = smtpSettings["SenderName"]!;
            var senderEmail = smtpSettings["SenderEmail"]!;
            var username = smtpSettings["Username"]!;
            var password = smtpSettings["Password"]!;

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            using var client = new SmtpClient(server, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            await client.SendMailAsync(mailMessage);
        }
    }
}
