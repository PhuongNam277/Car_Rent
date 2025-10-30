using System.Security.Claims;
using Car_Rent.Hubs;
using Car_Rent.Infrastructure.MultiTenancy;
using Car_Rent.Interfaces;
using Car_Rent.Models;
using Car_Rent.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();

// Branch: đọc branch_id từ claim (nếu có)
builder.Services.AddScoped<IBranchScopeProvider, HttpBranchScopeProvider>();

builder.Services.AddAuthorization(o =>
{
    //// 1. Platform Admin: Chỉ Admin, không yêu cầu tenant_id
    //o.AddPolicy("PlatformAdmin", p =>
    //    p.RequireAuthenticatedUser().RequireRole("Admin")
    //);

    // 2. Owner (có thể cho admin, tạm thời ko cho)
    o.AddPolicy("TenantOwner", p =>
        p.RequireAuthenticatedUser().RequireClaim("tenant_id")
        .RequireAssertion(_context => _context.User.IsInRole("Owner")) // || _context.User.IsInRole("Admin"))
    );

    // 3. Nhân viên thao tác nghiệp vụ trong tenant ( không phải khu quản trị)
    o.AddPolicy("TenantStaffOwner", p =>
        p.RequireAuthenticatedUser()
        .RequireClaim("tenant_id")
        .RequireAssertion(_context => _context.User.IsInRole("Staff") || _context.User.IsInRole("Owner") || _context.User.IsInRole("Admin"))
    );

    // THÊM POLICY MỚI - Cho phép cả hai
    o.AddPolicy("PlatformAdminOrTenantOwner", p =>
        p.RequireAuthenticatedUser()
        .RequireAssertion(ctx =>
        {
            // Platform Admin: có role Admin KHÔNG CẦN tenant_id
            var isPlatformAdmin = ctx.User.IsInRole("Admin") &&
                                  !ctx.User.HasClaim(c => c.Type == "tenant_id");

            // Tenant Owner: có role Owner/Admin VÀ CÓ tenant_id
            var isTenantOwner = (ctx.User.IsInRole("Owner") || ctx.User.IsInRole("Admin")) &&
                                ctx.User.HasClaim(c => c.Type == "tenant_id");

            return isPlatformAdmin || isTenantOwner;
        })
    );

});

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

    })
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = builder.Configuration["GoogleKeys:ClientId"]!;
        options.ClientSecret = builder.Configuration["GoogleKeys:ClientSecret"]!;
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;
        options.Scope.Add("email");
        options.Scope.Add("profile");

        // Map Json về claim chuẩn .NET để lấy Name/Email nhanh gọn trong callback
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
        options.ClaimActions.MapCustomJson("urn:google:picture", json =>
        {
            try { return json.GetProperty("picture").GetString(); }
            catch { return null; }
        });
    });

// (tuỳ chọn) Policy chỉ cho Admin thao tác
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("Role", "Admin"));
});

builder.Services.AddScoped<ISalesService, SalesService>();


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

// Đặt Policy "HasTenant" và áp dụng cho các controller nghiệp vụ
//builder.Services.AddAuthorization(o =>
//{
//    o.AddPolicy("HasTenant", p => p.RequireClaim("tenant_id"));
//});

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

// middleware tự nhận diện end-user (nếu k có tenant_id) => IsEndUser = true
app.Use(async (ctx, next) =>
{
    var tp = ctx.RequestServices.GetRequiredService<ITenantProvider>();
    if (tp is Car_Rent.Infrastructure.MultiTenancy.HttpTenantProvider httpTp)
    {
        var hasTenantClaim = ctx.User?.HasClaim(c => c.Type == "tenant_id") == true;
        var isRoleUser = ctx.User?.IsInRole("User") == true;
        var isAnon = !(ctx.User?.Identity?.IsAuthenticated ?? false);

        // End-user = không chọn tenant (không có claim) hoặc là khách (Role=User) hoặc anonymous
        httpTp.IsEndUser = !hasTenantClaim || isRoleUser || isAnon;
    }
    await next();
});

// Cau hinh map controller
app.MapControllers();

// Map Hub
app.MapHub<ChatHub>("/hubs/chat");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Main}/{action=Index}/{id?}");

app.Run();
