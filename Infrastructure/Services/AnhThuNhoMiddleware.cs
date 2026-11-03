using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace ThaiBeer.Infrastructure.Services;

/// <summary>
/// Ảnh thu nhỏ theo yêu cầu cho ảnh upload: GET /uploads/....webp?w=480
/// - Chỉ nhận các chiều rộng trong danh sách cho phép (chống tạo vô hạn file).
/// - Tạo 1 lần bằng ImageSharp, lưu vào App_Data/anh-thu-nho, các lần sau trả file có sẵn.
/// - Ảnh gốc nhỏ hơn chiều rộng yêu cầu thì không phóng to (vẫn nén lại WebP).
/// Không có ?w= thì đi tiếp tới UseStaticFiles như bình thường.
/// </summary>
public class AnhThuNhoMiddleware
{
    public static readonly int[] ChieuRongChoPhep = { 320, 480, 640, 960 };
    private static readonly string[] _duoiChoPhep = { ".webp", ".jpg", ".jpeg", ".png" };
    private const int ThoiGianCacheGiay = 60 * 60 * 24 * 30; // 30 ngày

    private readonly RequestDelegate _next;
    private readonly string _webRoot;
    private readonly string _thuMucCache;
    private readonly ILogger<AnhThuNhoMiddleware> _logger;

    public AnhThuNhoMiddleware(RequestDelegate next, IWebHostEnvironment env, ILogger<AnhThuNhoMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _webRoot = Path.GetFullPath(!string.IsNullOrWhiteSpace(env.WebRootPath) ? env.WebRootPath : Path.Combine(env.ContentRootPath, "wwwroot"));
        _thuMucCache = Path.Combine(env.ContentRootPath, "App_Data", "anh-thu-nho");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) ||
            path == null || !path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(context.Request.Query["w"], out var w) || Array.IndexOf(ChieuRongChoPhep, w) < 0 ||
            !_duoiChoPhep.Contains(Path.GetExtension(path).ToLowerInvariant()))
        {
            await _next(context);
            return;
        }

        // Chống path traversal: file gốc phải nằm trong wwwroot/uploads
        var tuongDoi = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fileGoc = Path.GetFullPath(Path.Combine(_webRoot, tuongDoi));
        if (!fileGoc.StartsWith(Path.Combine(_webRoot, "uploads") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(fileGoc))
        {
            await _next(context);
            return;
        }

        var fileThuNho = Path.Combine(_thuMucCache, w.ToString(), Path.ChangeExtension(tuongDoi, ".webp"));
        var thoiGianGoc = File.GetLastWriteTimeUtc(fileGoc);

        if (!File.Exists(fileThuNho) || File.GetLastWriteTimeUtc(fileThuNho) < thoiGianGoc)
        {
            try
            {
                await TaoAnhThuNhoAsync(fileGoc, fileThuNho, w);
            }
            catch (Exception ex)
            {
                // Lỗi tạo ảnh thu nhỏ: trả ảnh gốc, không làm hỏng trang
                _logger.LogWarning(ex, "Không tạo được ảnh thu nhỏ {Path} w={W}", path, w);
                await _next(context);
                return;
            }
        }

        var thongTin = new FileInfo(fileThuNho);
        var etag = $"\"{thongTin.LastWriteTimeUtc.Ticks:x}-{thongTin.Length:x}\"";
        context.Response.Headers.CacheControl = $"public,max-age={ThoiGianCacheGiay}";
        context.Response.Headers.ETag = etag;
        if (context.Request.Headers.IfNoneMatch == etag)
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }

        context.Response.ContentType = "image/webp";
        context.Response.ContentLength = thongTin.Length;
        if (HttpMethods.IsHead(context.Request.Method))
            return;
        await context.Response.SendFileAsync(fileThuNho);
    }

    private static async Task TaoAnhThuNhoAsync(string fileGoc, string fileThuNho, int w)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(fileThuNho)!);
        using var image = await Image.LoadAsync(fileGoc);
        if (image.Width > w)
        {
            image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(w, 0), Mode = ResizeMode.Max, Sampler = KnownResamplers.Lanczos3 }));
        }

        // Ghi ra file tạm rồi đổi tên: an toàn khi nhiều request cùng tạo 1 ảnh
        var fileTam = fileThuNho + "." + Guid.NewGuid().ToString("N") + ".tmp";
        await image.SaveAsWebpAsync(fileTam, new WebpEncoder { Quality = 78, FileFormat = WebpFileFormatType.Lossy, Method = WebpEncodingMethod.Level4 });
        File.Move(fileTam, fileThuNho, overwrite: true);
    }
}

/// <summary>Sinh URL ảnh thu nhỏ trong view: @AnhThuNho.Url(sp.HinhAnhWebP, 480)</summary>
public static class AnhThuNho
{
    /// <summary>Ảnh trong /uploads/ -> thêm ?w=; ảnh khác (ảnh tĩnh, link ngoài) giữ nguyên.</summary>
    public static string? Url(string? url, int chieuRong)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) || url.Contains('?'))
            return url;
        return $"{url}?w={chieuRong}";
    }

    /// <summary>Chuỗi srcset gồm các cỡ thu nhỏ, dùng kèm sizes="..." để trình duyệt tự chọn cỡ phù hợp màn hình.</summary>
    public static string? SrcSet(string? url, params int[] cacCo)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) || url.Contains('?'))
            return null;
        return string.Join(", ", cacCo.Select(w => $"{url}?w={w} {w}w"));
    }
}
