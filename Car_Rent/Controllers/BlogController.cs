using Car_Rent.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Controllers
{
    public class BlogController : Controller
    {
        private readonly CarRentalDbContext _context;

        public BlogController(CarRentalDbContext context)
        {
            _context = context;
        }



        public async Task<IActionResult> Index()
        {
            // Lay ds blog
            var blogs = await _context.Blogs.Include(b => b.Author).ToListAsync();

            // Lay ds comment
            var comments = await _context.Comments.ToListAsync();

            // Lay ds author

            // Lay ds user
            var authorEntities = await _context.Users.ToListAsync();

            var blogViewModels = new BlogViewModel
            {
                Blogs = blogs,
                Comments = comments,
                Authors = authorEntities
            };


            ViewData["ActivePage"] = "Blog";
            return View(blogViewModels);
        }
    }
}
