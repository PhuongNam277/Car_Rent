using Car_Rent.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Car_Rent.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CarRentalDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Thêm dịch vụ session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(5); // Thời gian hết hạn session
    options.Cookie.HttpOnly = true; // Đảm bảo cookie chỉ được truy cập từ server
    options.Cookie.IsEssential = true; // Bắt buộc cookie, ngay cả khi người dùng không đồng ý
});

//Them phan user service để làm các công việc như gửi email, vv
builder.Services.AddScoped<IUserService, UserService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Đăng ký dịch vụ IEmailSender
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<EmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession(); // Thêm dòng này để sử dụng session

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Main}/{action=Index}/{id?}");

app.Run();
