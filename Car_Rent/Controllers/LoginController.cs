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
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

                if (user != null)
                {

                    // Verify (new PBKDF2 format or SHA-256 old format)
                    if (PasswordHasherUtil.VerifyPassword(model.Password, user.PasswordHash))
                    {
                        // If it is legacy or need to up iterations then rehash and update
                        if(PasswordHasherUtil.NeedsRehash(user.PasswordHash))
                        {
                            user.PasswordHash = PasswordHasherUtil.HashPassword(model.Password);
                            await _context.SaveChangesAsync();
                        }

                        // Create claims
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Username),
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

        // Action Denied
        public IActionResult AccessDenied()
        {
            return View();
        }
    } 
}
