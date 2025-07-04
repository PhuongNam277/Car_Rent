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
            // 1. Lay danh sach xe tu db
            var carEntities = await _context.Cars.ToListAsync();

            // Lay danh sach blog tu db
            var blogEntities = await _context.Blogs
                .Include(b => b.Author).ToListAsync();

            // Lay ds comment

            var commentEntities = await _context.Comments.ToListAsync();

            // Lay ds user
            var authorEntities = await _context.Users.ToListAsync();



            // 2. Map sang carViewModel
            var carViewModels = carEntities.Select(car => new Car
            {
                CarId = car.CarId,
                CarName = car.CarName,
                SeatNumber = car.SeatNumber,
                SellDate = car.SellDate,
                DistanceTraveled = car.DistanceTraveled,
                Brand = car.Brand,
                Model = car.Model,
                LicensePlate = car.LicensePlate,
                RentalPricePerDay = car.RentalPricePerDay,
                ImageUrl = car.ImageUrl,
                EnergyType = car.EnergyType,
                EngineType = car.EngineType,
                CategoryId = car.CategoryId,
                Status = car.Status,
                TransmissionType = car.TransmissionType

            }).ToList();

            // 3. Truyen carViewModels vao view
            var viewModel = new IndexViewModel
            {
                Cars = carViewModels,
                Blogs = blogEntities,
                Authors = authorEntities.Cast<User>().ToList(),
                Comments = commentEntities
            };

            // 4. Tra ve view
            return View(viewModel);
        }
    }
}
