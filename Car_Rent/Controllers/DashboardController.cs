using Car_Rent.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ISalesService _svc;
        public DashboardController(ISalesService svc) => _svc = svc;

        [HttpGet("sales")]
        public async Task<IActionResult> GetSales()
        {
            return Ok(await _svc.GetDashboardSalesAsync());
        }
    }
}
