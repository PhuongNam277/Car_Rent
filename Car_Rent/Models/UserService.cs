using Car_Rent.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Models
{
    public class UserService : IUserService
    {
        private readonly CarRentalDbContext _context;

        public UserService(CarRentalDbContext context)
        {
            _context = context;
        }

        // Lưu người dùng vào cơ sở dữ liệu
        public async Task SaveUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        // Lấy người dùng theo email
        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
