using System.Security.Claims;
using Car_Rent.Models;
using Car_Rent.ViewModels.Blog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Car_Rent.ViewModels;

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
        public async Task<IActionResult> AdminIndex(string search, string? sortBy = "PublishedDateDesc", int page = 1, int pageSize = 10)
        {
            var query = _context.Blogs
                .Include(b => b.Author)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.Title.Contains(search) || b.Content.Contains(search) || b.Author.FullName.Contains(search));

            }

            // Sorting logic
            query = sortBy switch
            {
                "TitleAsc" => query.OrderBy(b => b.Title),
                "PublishedDateAsc" => query.OrderBy(b => b.PublishedDate),
                "TitleDesc" => query.OrderByDescending(b => b.Title),
                _ => query.OrderByDescending(b => b.PublishedDate)
            };

            // Pagination logic
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.SortBy = sortBy;
            ViewBag.TotalItems = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            return View(items);
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

        // Blog Details with comments
        [HttpGet]
        public async Task<IActionResult> BlogDetails(int id)
        {
            var blog = await _context.Blogs
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.BlogId == id && b.Status == "Published");

            if (blog == null) return NotFound();

            var comments = await _context.Comments
                .AsNoTracking()
                .Where(c => c.BlogId == id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentUserId = GetCurrentUserId();
            ViewBag.IsAdmin = IsAdmin();

            return View(new ViewModels.Blog.BlogDetailsViewModel
            {
                Blog = blog,
                Comments = comments,
                NewComment = new ViewModels.Blog.BlogCommentInput { BlogId = id }
            });
        }

        // Create comments
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // needs login to post comments
        public async Task<IActionResult> PostComment([Bind(Prefix ="NewComment")]BlogCommentInput input)
        {
            if (!ModelState.IsValid) return await ReloadDetailsWithModelState(input.BlogId, input);

            var uid = GetCurrentUserId();
            var username = await GetCurrentUsernameAsync();
            if(uid == null || string.IsNullOrWhiteSpace(username))
            {
                var returnUrl = Url.Action(nameof(BlogDetails), new {id = input.BlogId});
                return RedirectToAction("Index", "Login", new { returnUrl });
            }

            var displayName = input.IsAnonymous ? "Anonymous" : username;

            var comment = new Comment
            {
                BlogId = input.BlogId,
                UserId = (int)uid,
                AuthorName = displayName,
                IsAnonymous = input.IsAnonymous,
                Content = input.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(BlogDetails), new { id = input.BlogId });

        }

        // Edit comment
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditComment (int id)
        {
            var cmt = await _context.Comments.FirstOrDefaultAsync(c => c.CommentId == id);
            if (cmt == null) return NotFound();

            if (!CanManageComment(cmt)) return Forbid();

            var vm = new CommentEditInput
            {
                CommentId = cmt.CommentId,
                BlogId = cmt.BlogId,
                Content = cmt.Content
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EditComment(CommentEditInput input)
        {
            if (!ModelState.IsValid) return View(input);

            var cmt = await _context.Comments.FirstOrDefaultAsync(c => c.CommentId == input.CommentId);
            if (cmt == null) return NotFound();

            if (!CanManageComment(cmt)) return Forbid();

            cmt.Content = input.Content.Trim();
            cmt.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var url = Url.Action(nameof(BlogDetails), new { id = input.BlogId });
            return Redirect(url + $"#comment-{cmt.CommentId}");
        }

        // Delete comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int id, int blogId)
        {
            var cmt = await _context.Comments.FirstOrDefaultAsync(c => c.CommentId == id);
            if (cmt == null) return NotFound();

            if (!CanManageComment(cmt)) return Forbid();

            _context.Comments.Remove(cmt);
            await _context.SaveChangesAsync();

            var url = Url.Action(nameof(BlogDetails), new { id = blogId });
            return Redirect(url + "#comment");
        }

        // Helpers
        private async Task<IActionResult> ReloadDetailsWithModelState (int blogId, BlogCommentInput input)
        {
            var blog = await _context.Blogs
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.BlogId == blogId);
            if (blog == null) return NotFound();

            var comments = await _context.Comments
                .AsNoTracking()
                .Where(c => c.BlogId == blogId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentUserId = GetCurrentUserId();
            ViewBag.IsAdmin = IsAdmin();

            return View("BlogDetails", new ViewModels.Blog.BlogDetailsViewModel
            {
                Blog = blog,
                Comments = comments,
                NewComment = input
            });
        }
        
        private int? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");
            if (int.TryParse(raw, out var id)) return id;
            return null;
        }

        private async Task<string?> GetCurrentUsernameAsync()
        {
            var name = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("Username");
            if (!string.IsNullOrWhiteSpace(name)) return name;

            var idRaw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");
            if (int.TryParse(idRaw, out var uid))
            {
                return await _context.Users
                    .AsNoTracking()
                    .Where(u => u.UserId == uid)
                    .Select(u => u.Username)
                    .FirstOrDefaultAsync();
            }

            return null;
        
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin")
                || User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin")
                || User.HasClaim("IsAdmin", "true");
        }

        private bool CanManageComment(Comment c)
        {
            var uid = GetCurrentUserId();
            return IsAdmin() || (uid != null && c.UserId == uid);
        }







    }
}
