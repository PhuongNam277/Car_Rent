using System.Text;
using System.Security.Cryptography;
using Car_Rent.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Car_Rent.Security;
using Car_Rent.ViewModels.Auth;
using static System.Net.WebRequestMethods;
using Microsoft.AspNetCore.Authentication.Google;

namespace Car_Rent.Controllers
{
    public class LoginController : Controller
    {
        private readonly CarRentalDbContext _context;
        private readonly EmailService _emailService;

        public LoginController(CarRentalDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // Session key
        private const string K_UserId = "PWRESET_USERID";
        private const string K_Email = "PWRESET_EMAIL";
        private const string K_OtpHash = "PWRESET_OTPHASH"; // Hex sha-256
        private const string K_Expire = "PWRESET_EXPIRES"; // ticks (long)
        private const string K_Attempts = "PWRESET_ATTEMPTS"; // int
        private const string K_Verified = "PWRESET_VERIFIED"; // int (0/1)
        private const string K_Used = "PWRESET_USED"; // int (0/1)

        


        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // Nút login with gg sẽ gọi vào đây
        [HttpGet]
        public IActionResult Google(string? returnUrl = "/")
        {
            // để chống open-redirect, chỉ chấp nhận local url
            if(string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }

            var props = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback), "Login", new { returnUrl })
            };

            return Challenge(props, GoogleDefaults.AuthenticationScheme);
        }

        // Gg trả về đây (redirectUri ở trên)
        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = "/")
        {
            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }

            // Lấy kết quả xác thực từ Google
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal == null)
            {
                return RedirectToAction(nameof(Index)); // về trang login nếu fail
            }

            var email = result.Principal.FindFirstValue(ClaimTypes.Email)
                ?? result.Principal.FindFirst("email")?.Value;

            var fullName = result.Principal.FindFirstValue(ClaimTypes.Name)
                ?? result.Principal.FindFirst("name")?.Value;

            var avatar = result.Principal.FindFirst("urn:google:picture")?.Value;

            if(string.IsNullOrWhiteSpace(email))
            {
                // không lấy được email thì hủy
                TempData["Error"] = "Google does not provide email. Please try another account.";
                return RedirectToAction(nameof(Index));
            }

            // Tìm hoặc tạo user local
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                // Lấy role 'User' làm mặc định
                int? userRoleId = await _context.Roles
                    .Where(r => r.RoleName == "User")
                    .Select(r => (int?)r.RoleId)
                    .FirstOrDefaultAsync();

                user = new User
                {
                    Username = GenerateUniqueUsernameFromEmail(email),
                    Email = email,
                    FullName = fullName ?? email,
                    RoleId = userRoleId,
                    PasswordHash = "GOOGLE_OAUTH", // đánh dấu external; không dùng để login thường
                    CreatedDate = DateTime.UtcNow
                };

                // Nếu có cột AvatarUrl thì lưu
                try
                {
                    var avatarProp = typeof(User).GetProperty("AvatarUrl");
                    if (avatarProp != null && !string.IsNullOrWhiteSpace(avatar))
                    {
                        avatarProp.SetValue(user, avatar);
                    }
                }
                catch { }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            if (user.IsBlocked)
            {
                TempData["Error"] = "Your account has been blocked.";
            }

            // Lấy RoleName để đưa vào claim "Role" (phục vụ AdminOnly)
            string roleName = await _context.Roles
                .Where(r => r.RoleId == user.RoleId)
                .Select(r => r.RoleName)
                .FirstOrDefaultAsync() ?? "User";

            // Lấy tenant của user
            var tenantIds = await _context.TenantMemberships
                .Where(tm => tm.UserId == user.UserId)
                .Select(tm => tm.TenantId)
                .ToListAsync();

            var claims = new List<Claim>
            {
                new Claim("UserId", user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName.Trim()),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
            };

            // Nếu chỉ 1 tenant -> gắn claim `tenant_id`
            if (tenantIds.Count == 1)
            {
                claims.Add(new Claim("tenant_id", tenantIds[0].ToString()));
            }

            if (!string.IsNullOrEmpty(avatar)) claims.Add(new Claim("AvatarUrl", avatar));

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);

            // Nếu nhiều tenant, đăng nhập tạm rồi chuyển sang chọn tenant
            if (tenantIds.Count > 1)
            {
                await HttpContext.SignInAsync("MyCookieAuth", principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(2)
                });
                return RedirectToAction("Select", "Tenant", new { returnUrl });
            }

            await HttpContext.SignInAsync("MyCookieAuth", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            });

            return LocalRedirect(returnUrl);
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid information. " : e.ErrorMessage)
                    .Distinct()
                    .ToList();

                TempData["ErrorMessage"] = errors.Count > 0
                    ? string.Join("\n", errors)
                    : "Please check the information again.";
                return View(model);
            }

            // PHẢI Include Role để lấy RoleName
            var user = await _context.Users
                                     .Include(u => u.Role)
                                     .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Incorrect username or password.";
                return View(model);
            }

            // Chặn tài khoản bị block
            if (user.IsBlocked)
            {
                TempData["ErrorMessage"] = "Your account has been locked. Please contact the administrator.";
                return View(model);
            }

            // Verify password
            if (!PasswordHasherUtil.VerifyPassword(model.Password, user.PasswordHash))
            {
                TempData["ErrorMessage"] = "Incorrect username or password.";
                return View(model);
            }

            // Nâng cấp hash nếu cần
            if (PasswordHasherUtil.NeedsRehash(user.PasswordHash))
            {
                user.PasswordHash = PasswordHasherUtil.HashPassword(model.Password);
                await _context.SaveChangesAsync();
            }

            // LẤY RoleName và chuẩn hoá
            var roleName = (user.Role?.RoleName ?? "User").Trim(); // ví dụ: "Admin", "User", "SuperAdmin"

            // Lấy tenant của user
            var tenantIds = await _context.TenantMemberships
                .Where(tm => tm.UserId == user.UserId)
                .Select(tm => tm.TenantId)
                .ToListAsync();

            // TẠO CLAIMS ĐẦY ĐỦ (phải có ClaimTypes.Role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, roleName),        // QUAN TRỌNG cho [Authorize(Roles="...")]
                new Claim("UserId", user.UserId.ToString())  // anh đang dùng ở OnValidatePrincipal
                // có thể thêm Email/FullName nếu cần
            };

            // Nếu user chỉ thuộc một tenant thì gắn luôn claim 'tenant_id'
            if(tenantIds.Count == 1)
            {
                claims.Add(new Claim("tenant_id", tenantIds[0].ToString()));
            }

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);

            // Nếu user thuộc nhiều tenant thì đăng nhập tạm và chuyển sang màn chọn tenant
            if (tenantIds.Count > 1)
            {
                await HttpContext.SignInAsync("MyCookieAuth", principal);
                return RedirectToAction("Select", "Tenant", new { returnUrl });
            }

            // (tuỳ) remember-me
            //var authProps = new AuthenticationProperties
            //{
            //    IsPersistent = model.RememberMe, // nếu có field RememberMe
            //    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            //};

            await HttpContext.SignInAsync("MyCookieAuth", principal);


            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                if (returnUrl.ToLower().Contains("/contact/send"))
                    return RedirectToAction("Index", "Contact");

                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Main");
        }


        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");

            return RedirectToAction("Index", "Main");
        }

        // Action Denied
        public IActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            return View();
        }

        // B1: Nhap email, gui otp
        [HttpGet]
        public IActionResult ForgotPassword() => View(new ForgotPasswordRequest());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest req)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Email is not valid.";
                return View(req);
            }

            var email = req.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            TempData["SuccessMessage"] = "If the email is registered, an OTP has been sent.";

            if(user == null)
            {
                // No such user, but don't reveal this
                return View(req);
            }
                
            // Reset old flow
            ClearPwResetSession();

            // Generate OTP & save to session (hash + expire)
            var otp = GenerateOtp6();
            HttpContext.Session.SetInt32(K_UserId, user.UserId);
            HttpContext.Session.SetString(K_Email, user.Email);
            HttpContext.Session.SetString(K_OtpHash, HashOtp(otp));
            HttpContext.Session.SetString(K_Expire, UtcNow().AddMinutes(15).Ticks.ToString());
            HttpContext.Session.SetInt32(K_Attempts, 0);
            HttpContext.Session.SetInt32(K_Verified, 0);
            HttpContext.Session.SetInt32(K_Used, 0);

            // Send email
            var emailBody = $@"
            <p>Hello {user.Username},</p>
            <p>Your password reset verification code is: <b>{otp}</b></p>
            <p>The code is valid for 10 minutes. If you do not request it, please ignore this email.</p>";
            await _emailService.SendEmailAsync(user.Email, "Password Reset Verification Code", emailBody);
            return RedirectToAction(nameof(VerifyOtp));
        }


        // B2: Nhap OTP
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var uid = HttpContext.Session.GetInt32(K_UserId);   
            var exp = GetExpireUtc();

            if(uid == null || exp == null || UtcNow() > exp || HttpContext.Session.GetInt32(K_Used) == 1)
            {
                ClearPwResetSession();
                TempData["ErrorMessage"] = "Invalid or expired password reset session. Please try again.";
                return RedirectToAction(nameof(ForgotPassword));

            }

            return View(new VerifyOtpRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(VerifyOtpRequest req)
        {
            if(!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please enter OTP code.";
                return View(req);
            }

            var uid = HttpContext.Session.GetInt32(K_UserId);
            var exp = GetExpireUtc();
            var used = HttpContext.Session.GetInt32(K_Used) == 1;
            if (uid == null || exp == null || UtcNow() > exp || used)
            {
                ClearPwResetSession();
                TempData["ErrorMessage"] = "Invalid or expired password reset session. Please try again.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var attempts = HttpContext.Session.GetInt32(K_Attempts) ?? 0;
            if (attempts >= 5)
            {
                ClearPwResetSession();
                TempData["ErrorMessage"] = "You have entered the wrong code too many times. Please request a new code.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var storedHash = HttpContext.Session.GetString(K_OtpHash) ?? "";
            var inputHash = HashOtp(req.Otp);

            if (!FixedEqHex(storedHash, inputHash))
            {
                HttpContext.Session.SetInt32(K_Attempts, attempts + 1);
                TempData["ErrorMessage"] = $"The OTP code is incorrect. You have {Math.Max(0, 5 - (attempts + 1))} more attempts.";
                return View(req);
            }

            // Correct OTP -> mark verified
            HttpContext.Session.SetInt32(K_Verified, 1);
            HttpContext.Session.SetString(K_Expire, UtcNow().AddMinutes(5).Ticks.ToString()); // thêm 5 phút để đổi pass

            TempData["SuccessMessage"] = "OTP authentication successful. Please reset password.";
            return RedirectToAction(nameof(ResetPassword));

        }

        // B3 : Dat lai mat khau
        [HttpGet]
        public IActionResult ResetPassword()
        {
            var verified = HttpContext.Session.GetInt32(K_Verified) == 1;
            var exp = GetExpireUtc();
            var used = HttpContext.Session.GetInt32(K_Used) == 1;

            if (!verified || exp == null || UtcNow() > exp || used)
            {
                ClearPwResetSession();
                TempData["ErrorMessage"] = "Invalid or expired password reset session. Please try again.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest req)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please check the information again.";
                return View(req);
            }

            var verified = HttpContext.Session.GetInt32(K_Verified) == 1;
            var used = HttpContext.Session.GetInt32(K_Used) == 1;
            var exp = GetExpireUtc();
            var userId = HttpContext.Session.GetInt32(K_UserId);

            if (!verified || used || exp == null || UtcNow() > exp || userId == null)
            {
                ClearPwResetSession();
                TempData["ErrorMessage"] = "Invalid or expired password reset session. Please try again.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null)
            {
                ClearPwResetSession();
                TempData["ErrorMessage"] = "User account not found. Please try again.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            user.PasswordHash = PasswordHasherUtil.HashPassword(req.NewPassword);
            await _context.SaveChangesAsync();

            // Send email
            var emailBody = $@"
            <p>Hello {user.Username},</p>
            <p>Your password was reset successfully.</p>
            <p>Now you can login your account with your new password. Thank you for using our services</p>";
            await _emailService.SendEmailAsync(user.Email, "Password Reset Successfully", emailBody);

            HttpContext.Session.SetInt32(K_Used, 1); // mark used
            ClearPwResetSession();
            return RedirectToAction(nameof(Index));
        }

        // Helpers
        private static string GenerateOtp6()
        {
            Span<byte> buf = stackalloc byte[4];
            RandomNumberGenerator.Fill(buf);
            uint val = BitConverter.ToUInt32(buf) % 1_000_000u;
            return val.ToString("D6");
        }

        private static string HashOtp(string otp)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(otp));
            return Convert.ToHexString(bytes); // upper hex
        }

        private static bool FixedEqHex(string hexA, string hexB)
        {
            var a = Convert.FromHexString(hexA);
            var b = Convert.FromHexString(hexB);
            return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
        }

        private static DateTime UtcNow() => DateTime.UtcNow;

        private DateTime? GetExpireUtc()
        {
            var s = HttpContext.Session.GetString(K_Expire);
            return long.TryParse(s, out var ticks) ? new DateTime(ticks, DateTimeKind.Utc) : null;
        }

        private void ClearPwResetSession()
        {
            HttpContext.Session.Remove(K_UserId);
            HttpContext.Session.Remove(K_Email);
            HttpContext.Session.Remove(K_OtpHash);
            HttpContext.Session.Remove(K_Expire);
            HttpContext.Session.Remove(K_Attempts);
            HttpContext.Session.Remove(K_Verified);
            HttpContext.Session.Remove(K_Used);
        }

        private string GenerateUniqueUsernameFromEmail(string email)
        {
            var baseName = email.Split('@')[0];
            var candidate = baseName;
            int i = 1;
            while(_context.Users.Any(u => u.Username == candidate))
            {
                candidate = $"{baseName}{i++}";
            }
            return candidate;
        }
    }
}
