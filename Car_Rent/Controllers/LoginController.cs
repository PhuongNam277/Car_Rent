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

namespace Car_Rent.Controllers
{
    public class LoginController : Controller
    {
        private readonly CarRentalDbContext _context;

        public LoginController(CarRentalDbContext context)
        { _context = context; }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            // PHẢI Include Role để lấy RoleName
            var user = await _context.Users
                                     .Include(u => u.Role)
                                     .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            // Chặn tài khoản bị block
            if (user.IsBlocked)
            {
                ModelState.AddModelError("", "Your account has been blocked. Please contact support.");
                return View(model);
            }

            // Verify password
            if (!PasswordHasherUtil.VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
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

            // TẠO CLAIMS ĐẦY ĐỦ (phải có ClaimTypes.Role)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, roleName),        // QUAN TRỌNG cho [Authorize(Roles="...")]
                new Claim("UserId", user.UserId.ToString())  // anh đang dùng ở OnValidatePrincipal
                // có thể thêm Email/FullName nếu cần
            };

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);

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

        [Authorize(AuthenticationSchemes = "MyCookieAuth")] // hoặc Roles="Admin"
        [HttpGet("/debug/whoami")]
        public IActionResult WhoAmI()
        {
            var lines = User.Claims.Select(c => $"{c.Type} = {c.Value}");
            return Content(string.Join(Environment.NewLine, lines), "text/plain");
        }
    } 
}
