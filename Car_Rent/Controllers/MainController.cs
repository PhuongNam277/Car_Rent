using Car_Rent.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Controllers
{
    public class MainController : Controller
    {
        private readonly CarRentalDbContext _context;

        public MainController(CarRentalDbContext context)
        {
            _context = context;
        }

        // Get: Main/Index

        public async Task<IActionResult> Index()
        {

            // Lay danh sach blog tu db
            var blogEntities = await _context.Blogs
                .Include(b => b.Author).ToListAsync();

            // Lay list team profession
            var teamEntities = await _context.Users.ToListAsync();

            // Lay ds comment

            var commentEntities = await _context.Comments.ToListAsync();

            // Lay ds user
            var authorEntities = await _context.Users.ToListAsync();

            // Lay ds review
            var reviewEntities = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Car)
                .ToListAsync();

            var carEntities = await _context.Cars
                .Include(c => c.Category) // Include the Category navigation property
                .ToListAsync();

            var viewModel = new IndexViewModel
            {
                Cars = carEntities,
                Blogs = blogEntities,
                Authors = authorEntities.Cast<User>().ToList(),
                Comments = commentEntities,
                Teams = teamEntities,
                Reviews = reviewEntities
            };

            // 4. Tra ve view
            return View(viewModel);
        }
    }
}
