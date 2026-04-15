using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using LinhKienDienTu_Web.Models;
using LinhKienDienTu_Web.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace LinhKienDienTu_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordSecurityService _passwordSecurity;
        private readonly IConfiguration _config;
        private readonly IPasswordHasher<User> _legacyPasswordHasher;

        public AuthController(ApplicationDbContext context, PasswordSecurityService passwordSecurity, IConfiguration config)
        {
            _context = context;
            _passwordSecurity = passwordSecurity;
            _config = config;
            _legacyPasswordHasher = new PasswordHasher<User>();
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new { Message = "Yêu cầu không hợp lệ." });

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
                return Unauthorized(new { Message = "Email hoặc mật khẩu không chính xác." });

            var loginSucceeded = false;

            if (_passwordSecurity.IsHashedByThisService(user.Mat_Khau))
            {
                loginSucceeded = _passwordSecurity.VerifyPassword(user.Mat_Khau, request.Password);
            }
            else
            {
                var legacyResult = _legacyPasswordHasher.VerifyHashedPassword(user, user.Mat_Khau, request.Password);
                if (legacyResult != PasswordVerificationResult.Failed || user.Mat_Khau == request.Password)
                {
                    loginSucceeded = true;
                }
            }

            if (!loginSucceeded)
                return Unauthorized(new { Message = "Email hoặc mật khẩu không chính xác." });

            if (user.IsLocked)
                return Unauthorized(new { Message = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ bộ phận hỗ trợ." });

            var issuer = _config["JwtOptions:Issuer"];
            var audience = _config["JwtOptions:Audience"];
            var secretKey = _config["JwtOptions:SecretKey"];

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.NameIdentifier, user.User_ID.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(120),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            return Ok(new { 
                token = jwt, 
                user = new { user.User_ID, user.Ho_Ten, user.Email, user.Role } 
            });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
