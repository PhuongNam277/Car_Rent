using Car_Rent.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        // GET: Blog
        public async Task<IActionResult> AdminIndex()
        {
            var carRentalDbContext = _context.Blogs.Include(u => u.Author);
            return View(await carRentalDbContext.ToListAsync());
        }

        // GET: Blog/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blog = await _context.Blogs
                .Include(b => b.Author)
                .FirstOrDefaultAsync(m => m.BlogId == id);

            if (blog == null)
            {
                return NotFound();
            }

            return View(blog);
        }

        // GET: Blog/Create
        public IActionResult Create()
        {
            ViewData["AuthorId"] = new SelectList(_context.Users, "UserId", "FullName");
            return View(new Blog());
        }

        // POST: Blog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BlogId, Title, Content, ImageUrl, AuthorId, PublishedDate, Status")] Blog blog)
        {
            if (ModelState.IsValid)
            {
                

                blog.PublishedDate = DateTime.Now;
                _context.Add(blog);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(AdminIndex));
            }

            ViewData["AuthorId"] = new SelectList(_context.Users, "UserId", "FullName", blog.AuthorId);
            return View(blog);
        }

        // GET: Blog/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blog = await _context.Blogs.FindAsync(id);

            if (blog == null)
            {
                return NotFound();
            }

            ViewData["AuthorId"] = new SelectList(_context.Users, "UserId", "FullName", blog.AuthorId);
            return View(blog);
        }

        // POST: Blog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BlogId, Title, Content, ImageUrl, AuthorId, PublishedDate, Status")] Blog blog)
        {
            if (id != blog.BlogId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(blog);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BlogExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(AdminIndex));
            }
            ViewData["AuthorId"] = new SelectList(_context.Users, "UserId", "FullName", blog.AuthorId);
            return View(blog);
        }

        // GET: Blog/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var blog = await _context.Blogs
                .Include(b => b.Author)
                .FirstOrDefaultAsync(m => m.BlogId == id);
            if (blog == null)
            {
                return NotFound();
            }
            return View(blog);
        }

        // POST: Blog/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var blog = await _context.Blogs.FindAsync(id);
            if (blog != null)
            {
                _context.Blogs.Remove(blog);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(AdminIndex));
        }

        private bool BlogExists(int id)
        {
            return _context.Blogs.Any(e => e.BlogId == id);
        }
    }
}
