using Project01_movie_lease_system.Models;
using Microsoft.EntityFrameworkCore;
using Project01_movie_lease_system.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Project01_movie_lease_system.Data;
using Project01_movie_lease_system.Service;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 設置 QuestPDF 授權
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MovieDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";  // 沒登入時會跳轉到這
        options.LogoutPath = "/Home/"; // 登出路徑
        options.AccessDeniedPath = "/Account/AccessDenied"; // 權限不足時的頁面
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Cookie 有效時間 30 分鐘
        options.SlidingExpiration = true; // 若有操作會自動延長過期時間
    });

// 註冊所有Repository
builder.Services.AddScoped<MemberRepository>();
builder.Services.AddScoped<MovieRepository>();
builder.Services.AddScoped<LeaseRepository>();
builder.Services.AddScoped<ReviewRepository>();
builder.Services.AddScoped<AdminRepository>();
builder.Services.AddScoped<FileRepository>();
builder.Services.AddScoped<VideoRecordRepository>();
// 註冊郵件服務
builder.Services.AddTransient<IEmailService, EmailService>();
// 註冊上傳圖片設定
builder.Services.AddSingleton<MovieImageUploadSetting>(sp => 
    new MovieImageUploadSetting {
        // 可以從配置檔案中讀取設定
        UploadPath = builder.Configuration["MovieImageUploadSetting:UploadPath"] ?? "wwwroot/uploads/movies",
        AllowedExtensions = builder.Configuration.GetValue<string[]>("MovieImageUploadSetting:AllowedExtensions") ?? new[] { ".jpg", ".jpeg", ".png", ".gif" },
        MaxSizeInBytes = 5 * 1024 * 1024 // 5MB
    }
);


var app = builder.Build();

// 這裡放 Admin 初始化
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MovieDbContext>();

    if (!db.Admins.Any(a => a.Username == "admin"))
    {
        var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
        db.Admins.Add(new Admin
        {
            Username = "admin",
            PasswordHash = adminPasswordHash,
            Email = "admin@moviego.com",
            CreatedAt = DateTime.Now,
            Role = StaffRole.Admin
        });
        db.SaveChanges();
    }
}
// 插入電影假資料 seedData
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    MoviesSeedData.Initialize(services);
    FileCategorySeedData.Initialize(services);
}
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();// 強制使用 HTTPS 
app.UseRouting();

app.UseAuthorization(); // 啟用認證中介軟體

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
