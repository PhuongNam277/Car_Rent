using System.Text;
using System.Security.Cryptography;
using Car_Rent.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

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
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

                if (user != null)
                {
                    string hashedPassword = HashPassword(model.Password);

                    if (user.PasswordHash == hashedPassword)
                    {
                        // Tạo danh tính người dùng
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Username),
                            //new Claim(ClaimTypes.Role, user.Role ?? "User"), // Mặc định là User nếu không có vai trò
                            new Claim("UserId", user.UserId.ToString())

                        };

                        var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                        var principal = new ClaimsPrincipal(identity);

                        // Đăng nhập người dùng
                        await HttpContext.SignInAsync("MyCookieAuth", principal);

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            // Nếu returnUrl là action POST thì tránh redirect
                            if (returnUrl.ToLower().Contains("/contact/send"))
                            {
                                return RedirectToAction("Index", "Contact");
                            }
                            return Redirect(returnUrl);
                        }



                        //HttpContext.Session.SetString("UserId", user.UserId.ToString());
                        //HttpContext.Session.SetString("Username", user.Username.ToString());

                        return RedirectToAction("Index", "Main");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            }

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");

            return RedirectToAction("Index", "Main");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        // Action Denied
        public IActionResult AccessDenied()
        {
            return View();
        }
    } 
}
