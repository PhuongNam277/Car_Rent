using Car_Rent.Interfaces;
using Car_Rent.Models;
using Microsoft.AspNetCore.Mvc;
namespace Car_Rent.Controllers
{
    public class RegisterController : Controller
    {
        private readonly IUserService _userService;
        private readonly EmailService _emailService;

        // Inject IUserService và EmailService vào constructor
        public RegisterController(IUserService userService, EmailService emailService)
        {
            _userService = userService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Action POST để nhận dữ liệu từ form đăng ký và lưu vào cơ sở dữ liệu
        [HttpPost]
        public async Task<IActionResult> Register(string email, string password)
        {
            // Kiểm tra xem người dùng đã tồn tại chưa
            var existingUser = await _userService.GetUserByEmailAsync(email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
                Console.WriteLine("Email đã được sử dụng");
                return View("Index", "Contact");
            }

            // Tạo đối tượng User và lưu vào cơ sở dữ liệu
            var user = new User
            {
                FullName = "kawk22",
                Username = "ok33",
                Email = email,
                PasswordHash = password // Mã hóa password nếu cần
            };

            await _userService.SaveUserAsync(user);

            // Gửi email xác nhận cho người dùng
            var otp = GenerateOtp(); // Hàm tạo mã OTP ngẫu nhiên
            var emailBody = $"Mã OTP của bạn là: {otp}";
            await _emailService.SendEmailAsync(email, "Xác thực tài khoản", emailBody);

            return RedirectToAction("ConfirmOtp", new { email });
        }

        // Action để người dùng nhập mã OTP
        [HttpGet]
        public IActionResult ConfirmOtp(string email)
        {
            return View(new ConfirmOtpModel { Email = email });
        }

        [HttpPost]
        public IActionResult ConfirmOtp(string email, string otp)
        {
            // Kiểm tra mã OTP và xác thực người dùng
            if (IsValidOtp(otp)) // Hàm kiểm tra mã OTP
            {
                // Thực hiện hành động đăng ký thành công
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("Otp", "Mã OTP không hợp lệ.");
                return View();
            }
        }

        private string GenerateOtp()
        {
            // Tạo mã OTP ngẫu nhiên (hoặc có thể dùng một thư viện để tạo OTP)
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private bool IsValidOtp(string otp)
        {
            // Kiểm tra mã OTP (có thể lưu OTP trong session hoặc cơ sở dữ liệu)
            return otp == "123456"; // Giả sử mã OTP này là 123456
        }
    }
}
