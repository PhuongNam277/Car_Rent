using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class FeatureController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Pages";
            return View();
        }

        [Authorize(AuthenticationSchemes = "MyCookieAuth")]
        public IActionResult DebugClaims()
        {
            var claims = User.Claims.Select(c => new
            {
                Type = c.Type,
                Value = c.Value
            }).ToList();

            return Json(claims);
        }
    }
}
