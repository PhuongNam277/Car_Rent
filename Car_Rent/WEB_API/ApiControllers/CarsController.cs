using Car_Rent.DTOs;
using Car_Rent.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.WEB_API.ApiControllers
{
    [Route("api/cars")]
    [ApiController]
    public class CarsController : ControllerBase
    {
        private readonly CarRentalDbContext _context;

        public CarsController(CarRentalDbContext context)
        {
            _context = context;
        }

        // GET: api/CarsApi
        [HttpGet]
        public async Task<IActionResult> GetCars()
        {
            var cars = await _context.Cars
                .Include(c => c.Category)
                .ToListAsync();

            var carDtos = cars.Select(c => new CarDto
            {
                CarId = c.CarId,
                CarName = c.CarName,
                CategoryId = c.CategoryId,
                CategoryName = c.Category.CategoryName,
                Model = c.Model,
                LicensePlate = c.LicensePlate,
                RentalPricePerDay = c.RentalPricePerDay,
                BaseLocationId = c.BaseLocationId,
                Brand = c.Brand
                
            });

            // return JSON
            return Ok(carDtos);
        }

        // POST: api/Cars
        [HttpPost]
        public async Task<IActionResult> CreateCar([FromBody] CarDto carDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var car = new Car
            {
                CarName = carDto.CarName,
                CategoryId = carDto.CategoryId,
                LicensePlate = carDto.LicensePlate,
                RentalPricePerDay = carDto.RentalPricePerDay,
                Model = carDto.Model,
                BaseLocationId = carDto.BaseLocationId,
                Brand = carDto.Brand
                
            };

            _context.Cars.Add(car);
            await _context.SaveChangesAsync();

            // Trả về CreatedAtAction để client biết được xe nào mới tạo
            return CreatedAtAction(nameof(GetCars), new { id = car.CarId }, carDto);
        }

        // PUT: api/Cars/5 
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCar(int id, [FromBody] CarDto carDto)
        {
            if (id != carDto.CarId) return BadRequest("Id is not correct");

            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();

            car.CarName = carDto.CarName;
            car.RentalPricePerDay = carDto.RentalPricePerDay;
            car.Model = carDto.Model;
            car.LicensePlate = carDto.LicensePlate;
            car.CategoryId = carDto.CategoryId;
            car.Brand = carDto.Brand;
            car.BaseLocationId = carDto.BaseLocationId;

            _context.Cars.Update(car);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Cars/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCar(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
