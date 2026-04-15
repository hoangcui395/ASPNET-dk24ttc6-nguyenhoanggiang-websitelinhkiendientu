using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;
using LinhKienDienTu_Web.Services;
using System.Security.Claims;

namespace LinhKienDienTu_Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordSecurityService _passwordSecurityService;
        private readonly IPasswordHasher<User> _legacyPasswordHasher;
        private readonly ILogger<AccountController> _logger;
        private readonly LinhKienDienTu_Web.Services.IEmailSender _emailSender;

        public AccountController(
            ApplicationDbContext context,
            PasswordSecurityService passwordSecurityService,
            ILogger<AccountController> logger,
            LinhKienDienTu_Web.Services.IEmailSender emailSender)
        {
            _context = context;
            _passwordSecurityService = passwordSecurityService;
            _legacyPasswordHasher = new PasswordHasher<User>();
            _logger = logger;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ email và mật khẩu.";
                return View();
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null)
            {
                ViewBag.Error = "Email hoặc mật khẩu không chính xác.";
                return View();
            }

            var loginSucceeded = false;
            var shouldUpgradePassword = false;

            if (_passwordSecurityService.IsHashedByThisService(user.Mat_Khau))
            {
                loginSucceeded = _passwordSecurityService.VerifyPassword(user.Mat_Khau, password);
            }
            else
            {
                var legacyResult = _legacyPasswordHasher.VerifyHashedPassword(user, user.Mat_Khau, password);
                if (legacyResult != PasswordVerificationResult.Failed)
                {
                    loginSucceeded = true;
                    shouldUpgradePassword = true;
                }
                else if (user.Mat_Khau == password)
                {
                    loginSucceeded = true;
                    shouldUpgradePassword = true;
                }
            }

            if (!loginSucceeded)
            {
                ViewBag.Error = "Email hoặc mật khẩu không chính xác.";
                return View();
            }

            if (user.IsLocked)
            {
                ViewBag.Error = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ bộ phận hỗ trợ.";
                return View();
            }

            if (shouldUpgradePassword)
            {
                user.Mat_Khau = _passwordSecurityService.HashPassword(password);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Ho_Ten),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserID", user.User_ID.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                ViewBag.Error = $"Lỗi từ nhà cung cấp: {remoteError}";
                return View("Login");
            }

            var info = await HttpContext.AuthenticateAsync("ExternalCookie");
            var claimsPrincipal = info?.Principal;

            if (claimsPrincipal == null)
            {
                return RedirectToAction("Login");
            }

            var email = claimsPrincipal.FindFirstValue(ClaimTypes.Email);
            var name = claimsPrincipal.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Không thể lấy thông tin email từ nhà cung cấp đăng nhập.";
                return View("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            
            if (user == null)
            {
                // Auto register user
                user = new User
                {
                    Ho_Ten = name ?? "User",
                    Email = email,
                    So_Dien_Thoai = "N/A", // Required by DB
                    Mat_Khau = _passwordSecurityService.HashPassword(Guid.NewGuid().ToString()),
                    Created_At = DateTime.Now,
                    Role = "User"
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // Sign in to main scheme
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Ho_Ten),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserID", user.User_ID.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth"); // Using schema name matching auth config
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);
            await HttpContext.SignOutAsync("ExternalCookie");

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Email = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            model.So_Dien_Thoai = model.So_Dien_Thoai?.Trim() ?? string.Empty;
            model.Ho_Ten = model.Ho_Ten?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Mat_Khau) ||
                string.IsNullOrWhiteSpace(model.Ho_Ten) ||
                string.IsNullOrWhiteSpace(model.So_Dien_Thoai))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View(model);
            }

            if (model.Mat_Khau.Length < 6)
            {
                ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự.";
                return View(model);
            }

            // Kiểm tra trùng Email trước (Email không mã hóa nên tìm được ngay)
            var userByEmail = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email);
            
            if (userByEmail != null)
            {
                ViewBag.Error = "Email đã tồn tại.";
                return View(model);
            }

            // Đối với Số điện thoại (do có mã hóa ngẫu nhiên nên không thể so sánh trực tiếp trong DB)
            // Tạm thời cho phép lưu nếu không trùng Email để đảm bảo hệ thống không bị crash
            // Trong thực tế, nên dùng cơ chế HMAC hoặc Deterministic Encryption nếu muốn chặn trùng Phone.
            
            try
            {
                var user = new User
                {
                    Ho_Ten = model.Ho_Ten,
                    Email = model.Email,
                    So_Dien_Thoai = model.So_Dien_Thoai,
                    Mat_Khau = _passwordSecurityService.HashPassword(model.Mat_Khau),
                    Created_At = DateTime.Now,
                    Role = "User",
                    IsLocked = false
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User registered successfully: {Email}", user.Email);
                TempData["RegisterSuccess"] = "Đăng ký tài khoản thành công. Vui lòng đăng nhập để tiếp tục.";
                return RedirectToAction("Login");
            }
            catch (DbUpdateException dbEx)
            {
                var error = dbEx.InnerException?.Message ?? dbEx.Message;
                _logger.LogError(dbEx, "DB Error while registering user: {Email}", model.Email);

                if (error.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("unique", StringComparison.OrdinalIgnoreCase))
                {
                    ViewBag.Error = "Email hoặc số điện thoại đã tồn tại.";
                }
                else if (error.Contains("string or binary data would be truncated", StringComparison.OrdinalIgnoreCase))
                {
                    ViewBag.Error = "Dữ liệu đăng ký vượt quá giới hạn độ dài cột trong cơ sở dữ liệu.";
                }
                else
                {
                    ViewBag.Error = "Lỗi khi lưu tài khoản vào cơ sở dữ liệu. Vui lòng thử lại.";
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while registering user: {Email}", model.Email);
                ViewBag.Error = "Đã xảy ra lỗi hệ thống khi đăng ký.";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Vui lòng nhập Email.";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.Trim());
            if (user == null)
            {
                // Tránh lộ thông tin email có tồn tại hay không bằng cách luôn báo lỗi chung chung hoặc thành công chung chung
                ViewBag.Message = "Nếu email tồn tại, link khôi phục đã được gửi tới hòm thư của bạn.";
                return View();
            }

            // Tạo Token ngẫu nhiên
            user.ResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpiry = DateTime.Now.AddHours(1); // Có hiệu lực 1 giờ
            await _context.SaveChangesAsync();

            var resetLink = Url.Action("ResetPassword", "Account", new { token = user.ResetToken, email = user.Email }, Request.Scheme);

            var message = $"<p>Chào {user.Ho_Ten},</p>" +
                          $"<p>Bạn vừa yêu cầu khôi phục mật khẩu tài khoản Linh Kiện Điện Tử HG.</p>" +
                          $"<p>Vui lòng click vào link sau để đặt lại mật khẩu của bạn (có hiệu lực trong 1 giờ):</p>" +
                          $"<p><a href='{resetLink}'>Khôi phục mật khẩu</a></p>" +
                          $"<p>Trân trọng,</p>";

            try
            {
                // Gửi email
                await _emailSender.SendEmailAsync(user.Email, "Khôi phục mật khẩu Linh Kiện Điện Tử HG", message);
                ViewBag.Message = "Link khôi phục mật khẩu đã được gửi tới email của bạn (Vui lòng kiểm tra màn hình nếu đang chạy local test).";
                
                // HIỂN THỊ LINK ĐỂ NHANH CHÓNG TEST THEO YÊU CẦU CỦA ĐỀ BÀI:
                ViewBag.MockLink = resetLink;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email khôi phục mật khẩu.");
                ViewBag.Error = "Có lỗi xảy ra khi gửi email khôi phục. Vui lòng thử lại sau.";
                
                // HIỂN THỊ TRỰC TIẾP LÚC LỖI EMAIL (Cho môi trường dev)
                ViewBag.MockLink = resetLink;
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.ResetToken == token);
            if (user == null || user.ResetTokenExpiry < DateTime.Now)
            {
                TempData["Error"] = "Link khôi phục mật khẩu không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction("Login");
            }

            ViewBag.Token = token;
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, string email, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu không khớp hoặc không hợp lệ.";
                ViewBag.Token = token;
                ViewBag.Email = email;
                return View();
            }

            if (newPassword.Length < 6)
            {
                ViewBag.Error = "Mật khẩu phải dài ít nhất 6 ký tự.";
                ViewBag.Token = token;
                ViewBag.Email = email;
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.ResetToken == token);
            if (user == null || user.ResetTokenExpiry < DateTime.Now)
            {
                TempData["Error"] = "Link khôi phục mật khẩu không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction("Login");
            }

            user.Mat_Khau = _passwordSecurityService.HashPassword(newPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            TempData["CheckoutSuccess"] = "Mật khẩu của bạn đã được đặt lại thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userIdStr = User.FindFirstValue("UserID");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login");

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(string hoTen, string soDienThoai)
        {
            var userIdStr = User.FindFirstValue("UserID");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(soDienThoai))
            {
                TempData["Error"] = "Vui lòng điền đủ thông tin.";
                return RedirectToAction("Profile");
            }

            var existingPhone = await _context.Users.FirstOrDefaultAsync(u => u.So_Dien_Thoai == soDienThoai && u.User_ID != userId);
            if (existingPhone != null)
            {
                TempData["Error"] = "Số điện thoại đã được đăng ký cho tài khoản khác.";
                return RedirectToAction("Profile");
            }

            user.Ho_Ten = hoTen.Trim();
            user.So_Dien_Thoai = soDienThoai.Trim();
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var userIdStr = User.FindFirstValue("UserID");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["ErrorPwd"] = "Vui lòng nhập mật khẩu cũ và mật khẩu mới.";
                return RedirectToAction("Profile");
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorPwd"] = "Mật khẩu xác nhận không khớp.";
                return RedirectToAction("Profile");
            }

            if (newPassword.Length < 6)
            {
                TempData["ErrorPwd"] = "Mật khẩu mới phải từ 6 ký tự.";
                return RedirectToAction("Profile");
            }

            // Verify old password
            bool ok = false;
            if (_passwordSecurityService.IsHashedByThisService(user.Mat_Khau))
                ok = _passwordSecurityService.VerifyPassword(user.Mat_Khau, oldPassword);
            else
            {
                var legacyResult = _legacyPasswordHasher.VerifyHashedPassword(user, user.Mat_Khau, oldPassword);
                ok = (legacyResult != PasswordVerificationResult.Failed) || (user.Mat_Khau == oldPassword);
            }

            if (!ok)
            {
                TempData["ErrorPwd"] = "Mật khẩu cũ không chính xác.";
                return RedirectToAction("Profile");
            }

            user.Mat_Khau = _passwordSecurityService.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessPwd"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Index", "Home");
        }
    }
}
