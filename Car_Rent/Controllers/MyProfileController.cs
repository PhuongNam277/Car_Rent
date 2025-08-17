using Car_Rent.Models;
using Car_Rent.ViewModels.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Car_Rent.Security;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Car_Rent.Controllers
{
    [Authorize(AuthenticationSchemes = "MyCookieAuth")]
    [AutoValidateAntiforgeryToken]
    public class MyProfileController : Controller
    {
        private readonly CarRentalDbContext _context;
        private readonly EmailService _emailService;
        private readonly string _avatarFolder;

        public MyProfileController(CarRentalDbContext context, EmailService emailService, IWebHostEnvironment env)
        {
            _context = context;
            _emailService = emailService;
            _avatarFolder = Path.Combine(env.WebRootPath, "uploads", "avatars");
            if (!Directory.Exists(_avatarFolder))
            {
                Directory.CreateDirectory(_avatarFolder);
            }
        }

        // GET: /MyProfile
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return RedirectToAction("Index", "Login");

            var vm = new ProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleName = user.Role?.RoleName ?? "User", // Mặc định là User nếu không có vai trò
                CreatedDate = user.CreatedDate,
                AvatarUrl = GetAvatarUrl(user.UserId)
            };

            ViewBag.ProfileUpdateRequest = new ProfileUpdateRequest
            {
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber
            };

            ViewBag.ChangePasswordRequest = new ChangePasswordRequest();
            ViewBag.RequestEmailChangeViewModel = new RequestEmailChangeViewModel();

            return View(vm);
        }

        // POST: /MyProfile/Update
        [HttpPost]
        public async Task<IActionResult> Update([Bind(Prefix = "updateReq")] ProfileUpdateRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            user.FullName = request.FullName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

            // Handle avatar (no DB column needed). Save as /wwwroot/updates/avatars/{userId}.jpg
            if (request.Avatar != null && request.Avatar.Length > 0)
            {
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                var ext = Path.GetExtension(request.Avatar.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext))
                {
                    TempData["Error"] = "Avatar must be an image (.jpg, .jpeg, .png, .webp, .gif)";
                    return RedirectToAction(nameof(Index));
                }

                if (request.Avatar.Length > 2 * 1024 * 1024)
                {
                    TempData["Error"] = "Avatar size must be <= 2MB.";
                    return RedirectToAction(nameof(Index));
                }

                // Deleting old files of this user (any ext)
                DeleteExistingAvatarFiles(user.UserId);

                // Save new avatar
                var fileName = $"{user.UserId}{ext}";
                var savePath = Path.Combine(_avatarFolder, fileName);
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await request.Avatar.CopyToAsync(stream);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Succes"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /MyProfile/ChangePassword
        [HttpPost]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = "pwdReq")] ChangePasswordRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please check your inputs";
                return RedirectToAction(nameof(Index));
            }

            if (request.NewPassword == request.CurrentPassword)
            {
                TempData["Error"] = "New password must be different from current password.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Verify current password
            if (!PasswordHasherUtil.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                TempData["Error"] = "Current password is incorrect.";
                return RedirectToAction(nameof(Index));
            }

            // Hash new password and update (PBKDF2)
            user.PasswordHash = PasswordHasherUtil.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(Index));

        }

        // POST: /MyProfile/RequestEmailChange
        [HttpPost]
        public async Task<IActionResult> RequestEmailChange([Bind(Prefix = "emailReq")] RequestEmailChangeViewModel request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            // Chặn cứng Gmail-only (case-insensitive)
            var pattern = @"^[^@\s]+@gmail\.com$";
            if (!Regex.IsMatch(request.NewEmail ?? "", pattern, RegexOptions.IgnoreCase))
            {
                TempData["Error"] = "Only @gmail.com emails are allowed.";
                return RedirectToAction(nameof(Index));
            }

            var newEmail = request.NewEmail.Trim().ToLowerInvariant();

            // Check dupliate email
            var existed = await _context.Users.AnyAsync(u => u.Email.ToLower() == newEmail);
            if (existed)
            {
                TempData["Error"] = "This email is already in use.";
                return RedirectToAction(nameof(Index));
            }

            // Generate OTP and save to session
            var otp = GenerateOtp();
            HttpContext.Session.SetString("EmailChange:OTP", otp);
            HttpContext.Session.SetString("EmailChange:NewEmail", newEmail);
            HttpContext.Session.SetString("EmailChange:UserId", userId.Value.ToString());
            HttpContext.Session.SetString("EmailChange:ExpiryUtc", DateTime.UtcNow.AddMinutes(10).ToString("O"));

            await _emailService.SendEmailAsync(newEmail, "Confirm your new email", $"Your OTP code is: {otp}");

            TempData["Success"] = "A confirmation email has been sent to your new email address. Please check your inbox.";
            return RedirectToAction(nameof(ConfirmEmailChange));
        }

        // GET: /MyProfile/ConfirmEmailChange
        [HttpGet]
        public IActionResult ConfirmEmailChange()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            var newEmail = HttpContext.Session.GetString("EmailChange:NewEmail");
            if (string.IsNullOrEmpty(newEmail))
            {
                TempData["Error"] = "No email change request found.";
                return RedirectToAction(nameof(Index));
            }

            return View(new ConfirmEmailChangeViewModel { NewEmail = newEmail });
        }

        // POST: /MyProfile/ConfirmEmailChange
        [HttpPost]
        public async Task<IActionResult> ConfirmEmailChange(ConfirmEmailChangeViewModel request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Index", "Login");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid OTP or email.";
                return RedirectToAction(nameof(ConfirmEmailChange));
            }

            var storedOtp = HttpContext.Session.GetString("EmailChange:OTP");
            var storedEmail = HttpContext.Session.GetString("EmailChange:NewEmail");
            var storedUserId = HttpContext.Session.GetString("EmailChange:UserId");
            var expiryRaw = HttpContext.Session.GetString("EmailChange:ExpiryUtc");

            if (storedOtp == null || storedEmail == null || storedUserId == null || expiryRaw == null)
            {
                TempData["Error"] = "No pending request";
                return RedirectToAction(nameof(Index));
            }

            if (!int.TryParse(storedUserId, out var targetUserId) || targetUserId != userId.Value)
            {
                TempData["Error"] = "Invalid request";
                return RedirectToAction(nameof(Index));
            }

            if (!DateTime.TryParse(expiryRaw, out var expiryUtc) || DateTime.UtcNow > expiryUtc)
            {
                TempData["Error"] = "OTP expired. Please request a new one.";
                ClearEmailChangeSession();
                return RedirectToAction(nameof(Index));
            }

            if (!string.Equals(storedEmail, request.NewEmail.Trim(), StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(storedOtp, request.Otp))
            {
                TempData["Error"] = "Incorrect OTP";
                return RedirectToAction(nameof(ConfirmEmailChange));
            }

            // All checks passed, update email
            var user = await _context.Users.FirstOrDefaultAsync(u  => u.UserId == userId.Value);
            if(user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            user.Email = storedEmail;
            await _context.SaveChangesAsync();
            ClearEmailChangeSession();

            TempData["Success"] = "Email changed successfully.";
            return RedirectToAction(nameof(Index));
        }


        // Helper methods
        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst("UserId")?.Value;
            return int.TryParse(claim, out var id) ? id : (int?)null;
        }

        private static string GenerateOtp()
        {
            var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            // 6-digit OTP
            var number = BitConverter.ToUInt32(bytes, 0) % 900000 + 100000; // Ensure 6 digits
            return number.ToString();
        }

        private void DeleteExistingAvatarFiles(int userId)
        {
            var patterns = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            foreach (var ext in patterns)
            {
                var path = Path.Combine(_avatarFolder, $"{userId}{ext}");
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
        }

        private string? GetAvatarUrl (int userId)
        {
            var patterns = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            foreach (var ext in patterns)
            {
                var rel = $"uploads/avatars/{userId}{ext}";
                var abs = Path.Combine(_avatarFolder, $"{userId}{ext}");
                if (System.IO.File.Exists(abs)) return rel;
            }
            return null; // No avatar found
        }

        private void ClearEmailChangeSession()
        {
            HttpContext.Session.Remove("EmailChange:OTP");
            HttpContext.Session.Remove("EmailChange:NewEmail");
            HttpContext.Session.Remove("EmailChange:UserId");
            HttpContext.Session.Remove("EmailChange:ExpiryUtc");
        }
    }
}
