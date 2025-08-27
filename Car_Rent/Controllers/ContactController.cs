using Car_Rent.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Controllers
{
    public class ContactController : Controller
    {
        private readonly CarRentalDbContext _context;

        public ContactController(CarRentalDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Contact";
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Send(Contact model)
        {
            if (ModelState.IsValid)
            {
                model.SubmittedDate = DateTime.Now;
                model.Status = "New"; // Mặc định trạng thái là "New"

                _context.Contacts.Add(model);
                await _context.SaveChangesAsync();

                // Gửi thành công → tạo thông báo
                TempData["SuccessMessage"] = "Your message has been sent successfully!";

                return RedirectToAction("Index");
            }
            // Gửi thất bại → tạo thông báo
            TempData["FailedMessage"] = "Your message failed to send !";
            return View("Index", model);
        }

        // GET: Contact/AdminIndex
        public async Task<IActionResult> AdminIndex(
            string? search,
            string? status,
            string? sortBy = "SubmittedDateDesc",
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Contacts.AsQueryable();

            // Filter theo từ khoá
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.Name.Contains(search) ||
                    c.Email.Contains(search) ||
                    (c.PhoneNumber != null && c.PhoneNumber.Contains(search)) ||
                    (c.Project != null && c.Project.Contains(search)) ||
                    (c.Subject != null && c.Subject.Contains(search)) ||
                    c.Message.Contains(search));
            }

            // Filter theo Status (New/InProgress/Resolved/Closed...) — tuỳ anh định nghĩa
            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.Status == status);
            }

            // Sort
            query = sortBy switch
            {
                "NameAsc" => query.OrderBy(c => c.Name),
                "NameDesc" => query.OrderByDescending(c => c.Name),

                "EmailAsc" => query.OrderBy(c => c.Email),
                "EmailDesc" => query.OrderByDescending(c => c.Email),

                "StatusAsc" => query.OrderBy(c => c.Status),
                "StatusDesc" => query.OrderByDescending(c => c.Status),

                "SubmittedDateAsc" => query.OrderBy(c => c.SubmittedDate),
                _ => query.OrderByDescending(c => c.SubmittedDate) // SubmittedDateDesc (mặc định)
            };

            // Pagination
            var total = await query.CountAsync();
            var items = await query
                .AsNoTracking()
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.SortBy = sortBy;
            ViewBag.TotalItems = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;

            return View(items);
        }

        // GET: Contact/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();

            var contact = await _context.Contacts
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ContactId == id);

            if (contact is null) return NotFound();

            return View(contact);
        }

        // GET: Contact/Create
        public IActionResult Create()
        {
            // Không có SelectList nào đặc biệt
            return View(new Contact { SubmittedDate = DateTime.Now, Status = "New" });
        }

        // POST: Contact/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contact contact)
        {
            if (!ModelState.IsValid)
            {
                return View(contact);
            }

            // Set mặc định nếu chưa có
            contact.SubmittedDate ??= DateTime.Now;
            contact.Status ??= "New";

            _context.Add(contact);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AdminIndex));
        }

        // GET: Contact/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();

            var contact = await _context.Contacts.FindAsync(id);
            if (contact is null) return NotFound();

            return View(contact);
        }

        // POST: Contact/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Contact contact)
        {
            if (id != contact.ContactId) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(contact);
            }

            try
            {
                // Giữ nguyên SubmittedDate cũ nếu form không bind (hoặc bị null khi post)
                var existing = await _context.Contacts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ContactId == id);

                if (existing is null) return NotFound();

                contact.SubmittedDate ??= existing.SubmittedDate;

                _context.Update(contact);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContactExists(id)) return NotFound();
                else throw;
            }

            return RedirectToAction(nameof(AdminIndex));
        }

        // GET: Contact/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();

            var contact = await _context.Contacts
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ContactId == id);

            if (contact is null) return NotFound();

            return View(contact);
        }

        // POST: Contact/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(AdminIndex));
        }

        // (Tuỳ chọn) cập nhật nhanh trạng thái — tiện cho Ajax/nút bấm trong bảng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact is null) return NotFound();

            contact.Status = status;
            await _context.SaveChangesAsync();

            // Có thể return Json cho Ajax, ở đây redirect đơn giản
            return RedirectToAction(nameof(AdminIndex));
        }

        private bool ContactExists(int id) => _context.Contacts.Any(e => e.ContactId == id);
    }
}

