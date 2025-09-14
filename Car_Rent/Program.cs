using Car_Rent.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Car_Rent.Interfaces;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.AspNetCore.Authentication;
using Car_Rent.Services;
using Car_Rent.Hubs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CarRentalDbContext>(
    options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Đăng ký dịch vụ Authentication
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options => 
    {
        options.Cookie.Name = "UserLoginCookie"; // Tên cookie
        options.LoginPath = "/Login/Index"; // Đường dẫn đến trang đăng nhập
        options.AccessDeniedPath = "/Login/AccessDenied"; // Đường dẫn đến trang từ chối truy cập
        options.Cookie.HttpOnly = true; // Đảm bảo cookie chỉ được truy cập từ server
        options.SlidingExpiration = true; // Kích hoạt tính năng gia hạn thời gian hết hạn cookie
        options.ExpireTimeSpan = TimeSpan.FromHours(2); // Thời gian hết hạn cookie
        options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var userIdStr = context.Principal?.FindFirst("UserId")?.Value;
                if (!int.TryParse(userIdStr, out var userId)) return;

                var db = context.HttpContext.RequestServices.GetRequiredService<CarRentalDbContext>();
                var isBlocked = await db.Users
                                        .Where(u => u.UserId == userId)
                                        .Select(u => u.IsBlocked)
                                        .FirstOrDefaultAsync();

                if (isBlocked)
                {
                    context.RejectPrincipal(); // Từ chối quyền truy cập nếu người dùng bị chặn
                    await context.HttpContext.SignOutAsync("MyCookieAuth"); // Đăng xuất người dùng
                }
            }
        };

    });

// (tuỳ chọn) Policy chỉ cho Admin thao tác
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("Role", "Admin"));
});

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();



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

// Dk SignalR
builder.Services.AddSignalR();

// Đăng ký dịch vụ IEmailSender
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<EmailService>();

// Dang ky dich vu ChatService
builder.Services.AddScoped<IChatService, ChatService>();

var app = builder.Build();

// Thực hiện migration tự động khi ứng dụng khởi động
//using (var scope = app.Services.CreateScope())
//{
//    var dbContext = scope.ServiceProvider.GetRequiredService<CarRentalDbContext>(); // Đổi thành CarRentalDbContext
//    dbContext.Database.Migrate();
//}

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

app.UseAuthentication(); // Thêm dòng này để sử dụng Authentication

app.UseAuthorization();

// Cau hinh map controller
app.MapControllers();

// Map Hub
app.MapHub<ChatHub>("/hubs/chat");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Main}/{action=Index}/{id?}");

app.Run();
