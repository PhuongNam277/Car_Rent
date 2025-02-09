using Car_Rent.Models;

namespace Car_Rent.Interfaces
{
    public interface IUserService
    {
        Task SaveUserAsync(User user); // Lưu thông tin người dùng vào cơ sở dữ liệu
        Task<User> GetUserByEmailAsync(string email); // Lấy thông tin người dùng theo email
    }
}
