using Project01_movie_lease_system.Models;
using Microsoft.EntityFrameworkCore;
using Project01_movie_lease_system.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Project01_movie_lease_system.Data;
using Project01_movie_lease_system.Service;
using QuestPDF.Infrastructure;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

var builder = WebApplication.CreateBuilder(args);

// 設置 QuestPDF 授權
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddControllersWithViews();

// 設定檔案上傳大小限制
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue; // 表單值長度限制
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500 MB
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// 設定 Kestrel 伺服器選項
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 500 MB
});

// 設定 IIS 選項
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 500 * 1024 * 1024; // 500 MB
});

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

// 設定 FFmpeg 路徑 - 需要指向包含 ffmpeg.exe 的目錄，而不是檔案本身
string ffmpegDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg");
string ffmpegExePath = Path.Combine(ffmpegDirectory, "ffmpeg.exe");
string ffprobeExePath = Path.Combine(ffmpegDirectory, "ffprobe.exe");

// 檢查 FFmpeg 目錄和執行檔案是否存在
if (!Directory.Exists(ffmpegDirectory))
{
    Directory.CreateDirectory(ffmpegDirectory);
    Console.WriteLine($"Created FFmpeg directory: {ffmpegDirectory}");
}

if (!System.IO.File.Exists(ffmpegExePath) || !System.IO.File.Exists(ffprobeExePath))
{
    Console.WriteLine($"FFmpeg executable not found at: {ffmpegExePath}");
    Console.WriteLine("Starting automatic FFmpeg download...");
    
    try
    {
        // 建立進度追蹤委託
        var progressHandler = new Progress<ProgressInfo>(progress =>
        {
            try
            {
                // 計算下載百分比
                double percentage = progress.TotalBytes > 0 ? 
                    (double)progress.DownloadedBytes / progress.TotalBytes * 100 : 0;
                
                var downloaded = FormatBytes(progress.DownloadedBytes);
                var total = FormatBytes(progress.TotalBytes);
                
                Console.WriteLine($"Downloading FFmpeg: {percentage:F1}% ({downloaded}/{total})");
                
                // 每 10% 顯示一次主要進度
                if (percentage % 10 < 1)
                {
                    Console.WriteLine($"Download progress: {percentage:F0}% complete");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Progress update error: {ex.Message}");
            }
        });

        // 下載最新版本的 FFmpeg
        Console.WriteLine("Downloading FFmpeg binaries...");
        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegDirectory, progressHandler);
        
        Console.WriteLine("FFmpeg download completed successfully!");
        
        // 驗證下載的檔案是否存在
        if (System.IO.File.Exists(ffmpegExePath) && System.IO.File.Exists(ffprobeExePath))
        {
            Console.WriteLine("FFmpeg and FFprobe executables verified.");
            FFmpeg.SetExecutablesPath(ffmpegDirectory);
        }
        else
        {
            Console.WriteLine("Warning: Some FFmpeg executables may be missing after download.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to download FFmpeg: {ex.Message}");
        Console.WriteLine("You can manually download FFmpeg from: https://www.gyan.dev/ffmpeg/builds/");
        Console.WriteLine("Extract ffmpeg.exe and ffprobe.exe to the ffmpeg folder.");
        
        // 嘗試使用系統 PATH 中的 FFmpeg
        try
        {
            FFmpeg.SetExecutablesPath("");
            Console.WriteLine("Attempting to use system FFmpeg from PATH...");
        }
        catch (Exception pathEx)
        {
            Console.WriteLine($"Failed to use system FFmpeg: {pathEx.Message}");
            Console.WriteLine("Video processing features will be unavailable.");
        }
    }
}
else
{
    // 設定 FFmpeg 路徑（指向目錄，不是檔案）
    FFmpeg.SetExecutablesPath(ffmpegDirectory);
    Console.WriteLine($"Using existing FFmpeg from directory: {ffmpegDirectory}");
}

// 輔助方法：格式化位元組大小
static string FormatBytes(long bytes)
{
    const long kb = 1024;
    const long mb = kb * 1024;
    const long gb = mb * 1024;
    
    if (bytes >= gb)
        return $"{bytes / (double)gb:F2} GB";
    else if (bytes >= mb)
        return $"{bytes / (double)mb:F2} MB";
    else if (bytes >= kb)
        return $"{bytes / (double)kb:F2} KB";
    else
        return $"{bytes} B";
}
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
