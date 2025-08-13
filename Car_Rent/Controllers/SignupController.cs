using System.Security.Cryptography;
using System.Text;
using Car_Rent.Models;
using Microsoft.AspNetCore.Mvc;
using Car_Rent.Interfaces;
using System.Text.Json;

namespace Car_Rent.Controllers
{
    public class SignupController : Controller
    {
        private readonly IUserService _userService;
        private readonly EmailService _emailService;

        // Inject IUserService và EmailService vào constructor
        public SignupController(IUserService userService, EmailService emailService)
        {
            _userService = userService;

            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // Action POST để nhận dữ liệu từ form đăng ký và lưu vào cơ sở dữ liệu
        [HttpPost]
        public async Task<IActionResult> Index(string fullname, string email, string password, string username)
        {
            // Kiểm tra xem người dùng đã tồn tại chưa
            var existingUser = await _userService.GetUserByEmailAsync(email);
            if (existingUser != null)
            {
                TempData["ExistedUsername"] = "Username existed, please try with different username!";

                return View();
            }

            // Ma hoa mat khau
            string hashedPassword = HashPassword(password);

            // Tạo đối tượng User và lưu vào cơ sở dữ liệu
            var user = new User
            {
                FullName = fullname,
                Username = username,
                Email = email,
                RoleId = 2, // Mặc định là User, có thể thay đổi nếu cần
                PasswordHash = hashedPassword // Mã hóa password nếu cần
            };

            

            // Tao va luu ma OTP vao session
            var otp = GenerateOtp();
            HttpContext.Session.SetString("OTPCode", otp);
            HttpContext.Session.SetString("OTPEmail", email);

            // Serialize User thanh JSON và lưu vào session
            HttpContext.Session.SetString("PendingUser", JsonSerializer.Serialize(user));

            // Gửi email xác nhận cho người dùng
            var emailBody = $"Your OTP code is: {otp}";
            await _emailService.SendEmailAsync(email, "Verify your account", emailBody);

            return RedirectToAction("ConfirmOtp", new { email });
        }

        // Action để người dùng nhập mã OTP
        [HttpGet]
        public IActionResult ConfirmOtp(string email)
        {
            return View(new ConfirmOtpModel { Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOtp(string email, string otp)
        {
            // Kiem tra ma OTP tu session
            var storedOtp = HttpContext.Session.GetString("OTPCode");
            var storedEmail = HttpContext.Session.GetString("OTPEmail");

            if(storedOtp == otp &&  storedEmail == email)
            {
                // Lấy thông tin người dùng từ session và lưu vào cơ sở dữ liệu
                var userJson = HttpContext.Session.GetString("PendingUser");
                if (userJson != null)
                {
                    var user = JsonSerializer.Deserialize<User>(userJson);

                    await _userService.SaveUserAsync(user);

                    // Xóa thông tin người dùng tạm thời khỏi session
                    HttpContext.Session.Remove("PendingUser");
                }
                // Xoa otp ra khoi session
                HttpContext.Session.Remove("OTPCode");
                HttpContext.Session.Remove("OTPEmail");

                // Hiển thị alert
                TempData["SuccessMessage"] = "Sign up successfully, please login to continue!";


                // Thực hiện hành động đăng ký thành công
                return RedirectToAction("Index", "Login");
            }
            else
            {
                ModelState.AddModelError("Otp", "Invalid OTP code");
                return View();
            }
        }

        private string GenerateOtp()
        {
            // Tạo mã OTP ngẫu nhiên (hoặc có thể dùng một thư viện để tạo OTP)
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}
