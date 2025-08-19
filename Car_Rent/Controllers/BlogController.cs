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
        public async Task<IActionResult> AdminIndex(string search)
        {
            var query = _context.Blogs
                .Include(b => b.Author)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.Title.Contains(search) || b.Content.Contains(search) || b.Author.FullName.Contains(search));

            }

            var blogs = await query.ToListAsync();
            return View(blogs);
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
        public async Task<IActionResult> Create(Blog blog, IFormFile ImageFile)
        {
            if (ModelState.IsValid)
            {
                // Upload image
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/blogs");

                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    blog.ImageUrl = "/images/blogs/" + fileName;
                }


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
        public async Task<IActionResult> Edit(int id, Blog blog, IFormFile? ImageFile)
        {
            if (id != blog.BlogId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy bản ghi cũ để giữ nguyên ảnh nếu không upload mới
                    var existingBlog = await _context.Blogs.AsNoTracking().FirstOrDefaultAsync(c => c.BlogId == id);

                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/blogs");

                        if (!Directory.Exists(uploadPath))
                            Directory.CreateDirectory(uploadPath);

                        var filePath = Path.Combine(uploadPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }

                        blog.ImageUrl = "/images/blogs/" + fileName;
                    }
                    else
                    {
                        // Không chọn ảnh mới => giữ nguyên ảnh cũ
                        blog.ImageUrl = existingBlog?.ImageUrl;
                    }
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
