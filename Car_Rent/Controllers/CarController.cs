using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Car_Rent.Models;
using Car_Rent.ViewModels.Car;
using Car_Rent.Infrastructure.MultiTenancy;

namespace Car_Rent.Controllers
{
    public class CarController : Controller
    {
        private readonly CarRentalDbContext _context;
        private readonly ITenantProvider _tenant;
        private readonly IBranchScopeProvider _branch;

        public CarController(CarRentalDbContext context, ITenantProvider tenant, IBranchScopeProvider branch)
        {
            _context = context;
            _tenant = tenant;
            _branch = branch;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] CarFilterVM filters)
        {
            ViewData["ActivePage"] = "Pages";
            // Base query (áp dụng tenant & vehicle type)
            // Nếu chọn tenantId trên marketplace, dùng IgnoreQueryFilters() rồi lọc theo TenantId
            // nếu không thì để nguyên query (lọc theo tenant hiện hành nhờ GlobalQueryFilters)

            IQueryable<Car> qBase = _context.Cars.AsNoTracking().IgnoreQueryFilters().Include(c => c.Category);

            if (filters.TenantId.HasValue)
            {
                // Bỏ ép kiểu ở đây
                qBase = qBase.Where(c => c.TenantId == filters.TenantId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.VehicleType))
                qBase = qBase.Where(c => c.VehicleType == filters.VehicleType);

            // Trạng thái 
            var q = qBase.Where(c => c.Status == null || c.Status == "Available" || c.Status == "Rented");

            // Filters
            if (filters.CategoryId.HasValue)
                q = q.Where(c => c.CategoryId == filters.CategoryId.Value);

            if (filters.PriceMin.HasValue)
                q = q.Where(c => c.RentalPricePerDay >= filters.PriceMin.Value);

            if (filters.PriceMax.HasValue)
                q = q.Where(c => c.RentalPricePerDay <= filters.PriceMax.Value);

            if (filters.Seats.HasValue)
                q = q.Where(c => c.SeatNumber == filters.Seats.Value);

            if (!string.IsNullOrWhiteSpace(filters.Transmission))
                q = q.Where(c => c.TransmissionType == filters.Transmission);

            if(!string.IsNullOrWhiteSpace(filters.Energy))
                q = q.Where(c => c.EnergyType == filters.Energy);

            // Sort
            q = filters.SortBy switch
            {
                "PriceDesc" => q.OrderByDescending(c => c.RentalPricePerDay),
                "NameAsc" => q.OrderBy(c => c.CarName),
                "NameDesc" => q.OrderByDescending(c => c.CarName),
                "Newest" => q.OrderBy(c => c.SellDate).ThenByDescending(c => c.CarId),
                _        => q.OrderBy(c => c.RentalPricePerDay)
            };

            // Count before paging
            var total = await q.CountAsync();

            // Paging
            var skip = (Math.Max(1, filters.Page) - 1) * Math.Max(1, filters.PageSize);
            var items = await q
                .Skip(skip)
                .Take(filters.PageSize)
                .Select(c => new CarListItemVM
                {
                    CarId = c.CarId,
                    CarName = c.CarName,
                    Brand = c.Brand,
                    ImageUrl = c.ImageUrl,
                    RentalPricePerDay = c.RentalPricePerDay,
                    SeatNumber = c.SeatNumber,
                    EnergyType = c.EnergyType,
                    EngineType = c.EngineType,
                    TransmissionType = c.TransmissionType,
                    Status = c.Status ?? "Available",
                    CategoryId = c.CategoryId,
                    CategoryName = c.Category != null ? c.Category.CategoryName : "",
                    TenantId = c.TenantId
                })
                .ToListAsync();

            // Quan trọng: tabs category theo bối cảnh
            // Nếu tập danh mục hợp lệ thao VehicleType và override/ẩn theo Tenant nếu có
            // Nếu có bảng TenantCategories: áp dụng ẩn/đổi tên/sort
            // Nếu không có: chỉ lọc theo VehicleType & IsActive

            var vt = filters.VehicleType ?? "Car";

            // Đếm số xe theo category trong bối cảnh hiện tại (tenant + vehicleType + filters phụ)
            var counts = await qBase
                .GroupBy(c => new {c.CategoryId})
                .Select(g => new {g.Key.CategoryId, Count = g.Count()})
                .ToListAsync();

            // Lấy categories theo VehicleType (+ áp override nếu có)
            List<(int CategoryId, string Name, int Sort)> catList;

            if (_context.Model.FindEntityType(typeof(TenantCategory)) != null && filters.TenantId.HasValue)
            {
                var tenantId = filters.TenantId.Value;

                var cats = await (
                    from c in _context.Categories.AsNoTracking()
                    join tc in _context.TenantCategories.AsNoTracking().Where(x => x.TenantId == tenantId)
                        on c.CategoryId equals tc.CategoryId into gj
                    from tc in gj.DefaultIfEmpty()
                    where c.IsActive && c.VehicleType == vt && (tc == null || tc.IsHidden == false)
                    select new
                    {
                        c.CategoryId,
                        Name = (tc != null && tc.DisplayNameOverride != null) ? tc.DisplayNameOverride : c.CategoryName,
                        Sort = (tc != null && tc.SortOrderOverride != null) ? tc.SortOrderOverride.Value : c.SortOrder
                    }).ToListAsync();

                catList = cats.Select(x => (x.CategoryId, x.Name, x.Sort)).ToList(); // Có thể lỗi
            }
            else
            {
                var cats = await _context.Categories.AsNoTracking()
                    .Where(c => c.IsActive && c.VehicleType == vt)
                    .Select(c => new { c.CategoryId, Name = c.CategoryName, Sort = c.SortOrder })
                    .ToListAsync();

                catList = cats.Select(x => (x.CategoryId, x.Name, x.Sort)).ToList();
            }

            var countDict = counts.ToDictionary(x => x.CategoryId, x => x.Count);
            var catsVM = catList
                .Select(x => (x.CategoryId, x.Name, Count: countDict.TryGetValue(x.CategoryId, out var n) ? n : 0, x.Sort))
                .OrderBy(x => x.Sort).ThenBy(x => x.Name)
                .ToList();

            // Danh sách các doanh nghiệp
            var tenantQuery = _context.Cars.IgnoreQueryFilters().AsNoTracking();
            if(!string.IsNullOrEmpty(filters.VehicleType))
            {
                tenantQuery = tenantQuery.Where(c => c.VehicleType == filters.VehicleType);

            }

            var tenants = await tenantQuery
                .GroupBy(c => c.TenantId)
                .Select(g => new { TenantId = g.Key, Count = g.Count() })
                .Join(_context.Tenants.AsNoTracking(),
                        carGroup => carGroup.TenantId,
                        tenant => tenant.TenantId,
                        (carGroup, tenant) => new { tenant.TenantId, tenant.Name, carGroup.Count })
                .Where(x => x.Count > 0)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var vm = new CarIndexVM
            {
                Filters = filters,
                Cars = items,
                TotalItems = total,
                Categories = catsVM.Select(x => (x.CategoryId, x.Name, x.Count)).ToList(),
                Tenants = tenants.Select(x => (x.TenantId, x.Name, x.Count)).ToList()
            };

            return View(vm);
        }

        // Quick view (partial)
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var car = await _context.Cars.AsNoTracking()
                .Include(c => c.Category)
                .Include(c => c.BaseLocation)
                .FirstOrDefaultAsync(c => c.CarId == id);

            if (car == null) { return NotFound(); }

            var vm = new CarListItemVM
            {
                CarId = car.CarId,
                CarName = car.CarName,
                Brand = car.Brand,
                ImageUrl = car.ImageUrl,
                RentalPricePerDay = car.RentalPricePerDay,
                SeatNumber = car.SeatNumber,
                EnergyType = car.EnergyType,
                EngineType = car.EngineType,
                TransmissionType = car.TransmissionType,
                Status = car.Status ?? "Available",
                CategoryId = car.CategoryId,
                CategoryName = car.Category?.CategoryName ?? ""
            };

            return PartialView("_CarQuickView", vm);
        }

        // Compare (partial) - nhận tối đa 3 id
        [HttpGet]
        public async Task<IActionResult> ComparePartial([FromQuery] int[] ids)
        {
            ids = ids?.Distinct().Take(3).ToArray() ?? Array.Empty<int>();
            var cars = await _context.Cars.AsNoTracking()
                .Include(c => c.Category)
                .Where(c => ids.Contains(c.CarId))
                .Select(c => new CarListItemVM
                {
                    CarId = c.CarId,
                    CarName = c.CarName,
                    Brand = c.Brand,
                    ImageUrl = c.ImageUrl,
                    RentalPricePerDay = c.RentalPricePerDay,
                    SeatNumber = c.SeatNumber,
                    EnergyType = c.EnergyType,
                    EngineType = c.EngineType,
                    TransmissionType = c.TransmissionType,
                    Status = c.Status ?? "Available",
                    CategoryId = c.CategoryId,
                    CategoryName = c.Category != null ? c.Category.CategoryName : ""
                })
                .ToListAsync();

            return PartialView("_CarCompare", cars);
        }

        // GET: Car
        public async Task<IActionResult> AdminIndex(string? search, string? sortBy = "NameAsc", int page = 1, int pageSize = 10)
        {
            // Ghi nhớ tham số tìm kiếm và sắp xếp
            ViewData["CurrentSearch"] = search;
            ViewBag.SortBy = sortBy;

            // Clamp cơ bản
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 5, 100);

            // Khởi tạo base query
            IQueryable<Car> baseQ;

            if (User.IsInRole("Admin"))
            {
                // Admin: Thấy tất cả xe, không lọc theo Tenant hay Branch
                baseQ = _context.Cars
                    .AsNoTracking()
                    .Include(c => c.Category);
            }
            else
            {

                // Nếu staff mà chưa có branch_id -> bắt chọn
                if (_branch.IsBranchScoped && !_branch.BranchId.HasValue)
                    return RedirectToAction("SelectBranch", "Admin", new { returnUrl = Url.Action("AdminIndex") });

                // Áp dụng lọc Tenant và Branch
                baseQ = _context.Cars
                    .AsNoTracking()
                    .Include(c => c.Category)
                    .ForTenant(_tenant.TenantId)
                    .ForBranch(_branch.BranchId, "BaseLocationId");
            }

            // Logic tìm kiếm (Áp dụng cho cả Admin và Non-Admin)
            if (!string.IsNullOrEmpty(search))
            {
                var s = search.Trim();
                baseQ = baseQ.Where(c => c.CarName.Contains(s)
                || c.Brand.Contains(s)
                || c.Model.Contains(s)
                || c.LicensePlate.Contains(s)
                || c.RentalPricePerDay.ToString().Contains(s));
            }

            // Logic Sắp xếp (Áp dụng cho cả Admin và Non-Admin)
            baseQ = sortBy switch
            {
                "NameDesc" => baseQ.OrderByDescending(c => c.CarName),
                "RentalPriceAsc" => baseQ.OrderBy(c => c.RentalPricePerDay),
                "RentalPriceDesc" => baseQ.OrderByDescending(c => c.RentalPricePerDay),
                _ => baseQ.OrderBy(c => c.CarName)
            };

            // Logic Phân trang (Áp dụng cho cả Admin và Non-Admin)
            var totalItems = await baseQ.CountAsync();
            var items = await baseQ
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();


            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;

            return View(items);
        }

        // GET: Car/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars 
                .Include(c => c.Category)
                .FirstOrDefaultAsync(m => m.CarId == id);
            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        // GET: Car/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");

            var locQ = _context.Locations.AsNoTracking();
            if(_tenant?.TenantId > 0) locQ = locQ.Where(l => l.TenantId == _tenant.TenantId);
            if(_branch?.IsBranchScoped == true && _branch.BranchId.HasValue)
                locQ = locQ.Where(l => l.LocationId == _branch.BranchId.Value);

            ViewData["BaseLocationId"] = new SelectList(locQ.OrderBy(l => l.Name), "LocationId", "Name");
            return View();
        }

        // POST: Car/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Car car, IFormFile ImageFile)
        {

            // Validate BaseLocation thuộc về tenant/branch hiện tại
            var validLocation = await _context.Locations.AsNoTracking()
                .AnyAsync(l => l.LocationId == car.BaseLocationId
                            && (_tenant == null || l.TenantId == _tenant.TenantId)
                            && (_branch == null || !_branch.IsBranchScoped || !_branch.BranchId.HasValue || l.LocationId == _branch.BranchId));

            if (!validLocation)
                ModelState.AddModelError(nameof(car.BaseLocationId), "Base location is invalid for current tenant/branch.");

            if(!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", car.CategoryId);
                var locQ = _context.Locations.AsNoTracking();
                if (_tenant?.TenantId > 0) locQ = locQ.Where(l => l.TenantId == _tenant.TenantId);
                if (_branch?.IsBranchScoped == true && _branch.BranchId.HasValue) locQ = locQ.Where(l => l.LocationId == _branch.BranchId.Value);
                ViewData["BaseLocationId"] = new SelectList(locQ.OrderBy(l => l.Name), "LocationId", "Name", car.BaseLocationId);
                return View(car);
            }

            // Gán Tenant/Branch
            if(_tenant?.TenantId > 0) car.TenantId = _tenant.TenantId;
            if(_branch?.IsBranchScoped == true && _branch.BranchId.HasValue)
                car.BaseLocationId = _branch.BranchId.Value;

            // Upload ảnh
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/cars");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                car.ImageUrl = "/images/cars/" + fileName;
            }

            _context.Add(car);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AdminIndex));
        }

        // GET: Car/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var car = await _context.Cars.FindAsync(id);

            if (car == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", car.CategoryId);

            // Lọc location theo tenant/branch hiện tại để không lộ dữ liệu chéo
            var locQ = _context.Locations.AsNoTracking();
            if (_tenant?.TenantId > 0) locQ = locQ.Where(l => l.TenantId == _tenant.TenantId);
            if (_branch?.IsBranchScoped == true && _branch.BranchId.HasValue)
                locQ = locQ.Where(l => l.LocationId == _branch.BranchId.Value);

            ViewData["BaseLocationId"] = new SelectList(
                locQ.OrderBy(l => l.Name).ToList(),
                "LocationId", "Name", car.BaseLocationId
            );

            return View(car);
        }

        // POST: Car/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Car car, IFormFile? ImageFile)
        {
            if (id != car.CarId)
            {
                return NotFound();
            }

            // Lấy bản ghi TRACKET để không overpost nhầm TenantId/ khóa ngoại
            var existing = await _context.Cars.FirstOrDefaultAsync(c => c.CarId == id);
            if (existing == null) return NotFound();

            // Validate BaseLocation nằm trong phạm vi được phép
            var validLocation = await _context.Locations.AsNoTracking() 
                .AnyAsync(l => l.LocationId == car.BaseLocationId
                            && (_tenant == null || l.TenantId == _tenant.TenantId)
                            && (_branch == null || !_branch.IsBranchScoped || !_branch.BranchId.HasValue || l.LocationId == _branch.BranchId));
            if (!validLocation)
                ModelState.AddModelError(nameof(car.BaseLocationId), "Base location is invalid for current tenant/branch.");

            if (!ModelState.IsValid)
            {
                // Repopulate dropdowns khi ModelState invalid
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", car.CategoryId);

                var locQ = _context.Locations.AsNoTracking();        
                if (_tenant?.TenantId > 0) locQ = locQ.Where(l => l.TenantId == _tenant.TenantId);
                if (_branch?.IsBranchScoped == true && _branch.BranchId.HasValue)
                    locQ = locQ.Where(l => l.LocationId == _branch.BranchId.Value);
                ViewData["BaseLocationId"] = new SelectList(locQ.OrderBy(l => l.Name), "LocationId", "Name", car.BaseLocationId);

                return View(car);
            }

            try
            {
                // Chỉ gán các field được phép sửa - không đụng tới TenandId/khóa hệ thống
                existing.CarName = car.CarName;
                existing.Model = car.Model;
                existing.CategoryId = car.CategoryId;
                existing.LicensePlate = car.LicensePlate;
                existing.RentalPricePerDay = car.RentalPricePerDay;
                existing.Brand = car.Brand;
                existing.BaseLocationId = car.BaseLocationId;
                existing.Status = car.Status;
                existing.SeatNumber = car.SeatNumber;
                existing.DistanceTraveled = car.DistanceTraveled;
                existing.EnergyType = car.EnergyType;
                existing.EngineType = car.EngineType;
                existing.SellDate = car.SellDate;
                existing.TransmissionType = car.TransmissionType;
                existing.VehicleType = car.VehicleType;

                // Upload ảnh nếu có
                if (ImageFile is { Length: > 0 })
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/cars");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
                    var filePath = Path.Combine(uploadPath, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await ImageFile.CopyToAsync(stream);
                    existing.ImageUrl = "/images/cars/" + fileName;
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Cars.AnyAsync(e => e.CarId == id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(AdminIndex));
        }

        // GET: Car/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars
                .Include(c => c.Category)
                .FirstOrDefaultAsync(m => m.CarId == id);
            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        // POST: Car/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car != null)
            {
                _context.Cars.Remove(car);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AdminIndex));
        }

        private bool CarExists(int id)
        {
            return _context.Cars.Any(e => e.CarId == id);
        }
    }
}
