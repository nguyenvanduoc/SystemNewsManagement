using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using ThaiBeer.Core.Interfaces;
using ThaiBeer.Infrastructure.Data;
using ThaiBeer.Infrastructure.Repositories;
using ThaiBeer.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Chuỗi kết nối SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost\\MSSQLSERVER01;Database=ThaiBeerDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

// EF Core 8 (Phân hệ Quản trị & Schema Migration)
builder.Services.AddDbContext<ThaiBeerDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    });
});

// Dapper Factory (Storefront High-Performance Read)
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(connectionString));

// Đăng ký Repository & Services
builder.Services.AddScoped<ISanPhamReadRepository, DapperSanPhamReadRepository>();
builder.Services.AddScoped<IImageOptimizerService, WebPOptimizerService>();
builder.Services.AddScoped<ICacheInvalidatorService, CacheInvalidatorService>();

// 2. Nén phản hồi HTTP (Brotli + Gzip) - Tối ưu FCP & TTFB < 100ms
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "image/svg+xml",
        "image/webp",
        "application/javascript",
        "text/css",
        "application/json"
    });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest; // Tối ưu CPU trên hosting IIS
});

// 3. Cơ chế Output Caching trên RAM (ASP.NET Core 8)
builder.Services.AddOutputCache(options =>
{
    // Chính sách cơ sở mặc định
    options.AddBasePolicy(b => b.Cache());

    // Chính sách cho Trang Chủ (15 phút, gắn tag 'trang_chu_tag')
    options.AddPolicy("TrangChuCache", policy => policy
        .Expire(TimeSpan.FromMinutes(15))
        .Tag("trang_chu_tag"));

    // Chính sách cho Danh mục sản phẩm (30 phút, thay đổi theo phân trang và slug)
    options.AddPolicy("SanPhamCatalogCache", policy => policy
        .Expire(TimeSpan.FromMinutes(30))
        .SetVaryByQuery("trang")
        .SetVaryByRouteValue("slug")
        .Tag("san_pham_tag"));

    // Chính sách cho Chi tiết sản phẩm (60 phút)
    options.AddPolicy("SanPhamChiTietCache", policy => policy
        .Expire(TimeSpan.FromMinutes(60))
        .SetVaryByRouteValue("slug")
        .Tag("san_pham_tag"));

    // Chính sách cho Tin tức
    options.AddPolicy("TinTucCache", policy => policy
        .Expire(TimeSpan.FromMinutes(30))
        .SetVaryByRouteValue("slug")
        .Tag("tin_tuc_tag"));
});

// 4. Cấu hình MVC Controllers & Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Khởi tạo Database & Dữ liệu mẫu ban đầu (Auto Migration/Seed)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ThaiBeerDbContext>();
        await DbInitializer.KhoiTaoDuLieuMauAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi tự động khởi tạo cơ sở dữ liệu mẫu ThaiBeer.");
    }
}

// 5. Cấu hình HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();

// Phục vụ tệp tĩnh với Cache-Control 30 ngày trên client
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 60 * 60 * 24 * 30; // 30 ngày
        ctx.Context.Response.Headers.Append("Cache-Control", $"public,max-age={durationInSeconds}");
    }
});

app.UseRouting();

// Output Caching trên RAM
app.UseOutputCache();

app.UseAuthorization();

// Routing: Phân hệ Quản trị (Areas/QuanTri)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=TongQuan}/{action=Index}/{id?}");

// Routing: Storefront Mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=TrangChu}/{action=Index}/{id?}");

app.Run();
