using Car_Rent.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Controllers
{
    public class TeamController : Controller
    {
        private readonly CarRentalDbContext _context;
        public TeamController(CarRentalDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            ViewData["ActivePage"] = "Pages";
            // Lay ds team profession
            var teamEntities =  await _context.Users.ToListAsync();

            var teamViewModel = new TeamViewModel
            {
                Teams = teamEntities
            };




            return View(teamViewModel);
        }
    }
}
