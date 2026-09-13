using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using ThaiBeer.Core.Interfaces;

namespace ThaiBeer.Infrastructure.Services;

public class WebPOptimizerService : IImageOptimizerService
{
    private readonly IWebHostEnvironment _environment;

    public WebPOptimizerService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> ToiUuVaLuuWebPAsync(
        IFormFile tapTin,
        string thuMucCon = "san-pham",
        int? chieuRongToiDa = 1200,
        int chatLuong = 82)
    {
        if (tapTin == null || tapTin.Length == 0)
        {
            throw new ArgumentException("Tập tin tải lên không hợp lệ.");
        }

        // Định dạng thư mục theo năm/tháng để tránh quá tải số file trong 1 folder
        var now = DateTime.UtcNow;
        var subPath = Path.Combine("uploads", thuMucCon, now.Year.ToString(), now.Month.ToString("D2"));
        var thuMucVatLy = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), subPath);

        if (!Directory.Exists(thuMucVatLy))
        {
            Directory.CreateDirectory(thuMucVatLy);
        }

        var tenTapTinMoi = $"{Guid.NewGuid():N}.webp";
        var duongDanVatLyDayDu = Path.Combine(thuMucVatLy, tenTapTinMoi);

        await using var inputStream = tapTin.OpenReadStream();
        using var image = await Image.LoadAsync(inputStream);

        // Tự động thu nhỏ nếu kích thước ảnh lớn hơn chieuRongToiDa
        if (chieuRongToiDa.HasValue && image.Width > chieuRongToiDa.Value)
        {
            var tyLe = (double)chieuRongToiDa.Value / image.Width;
            var chieuCaoMoi = (int)(image.Height * tyLe);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(chieuRongToiDa.Value, chieuCaoMoi),
                Mode = ResizeMode.Max
            }));
        }

        // Nén và xuất ra WebP
        var webpEncoder = new WebpEncoder
        {
            Quality = chatLuong,
            FileFormat = WebpFileFormatType.Lossy
        };

        await using var outputStream = new FileStream(duongDanVatLyDayDu, FileMode.Create);
        await image.SaveAsWebpAsync(outputStream, webpEncoder);

        // Trả về đường dẫn URL tương đối (dùng dấu / chuẩn web)
        return "/" + Path.Combine(subPath, tenTapTinMoi).Replace('\\', '/');
    }

    public void XoaHinhAnh(string? duongDanTuongDoi)
    {
        if (string.IsNullOrWhiteSpace(duongDanTuongDoi))
            return;

        // Chỉ xóa các ảnh do người dùng upload trong thư mục uploads/
        var normalized = duongDanTuongDoi.Trim().Replace('\\', '/').TrimStart('/');
        if (!normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var duongDanVatLy = Path.Combine(webRoot, normalized.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(duongDanVatLy))
            {
                File.Delete(duongDanVatLy);
            }
        }
        catch
        {
            // Bỏ qua lỗi xóa file cũ để không gián đoạn luồng nghiệp vụ
        }
    }
}
