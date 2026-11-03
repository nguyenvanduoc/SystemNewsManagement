using System.Text.RegularExpressions;

namespace ThaiBeer.Infrastructure.Services;

public record ThongTinThietBi(string ThietBi, string HeDieuHanh, string TrinhDuyet, bool LaBot);

/// <summary>
/// Nhận diện loại thiết bị, hệ điều hành, trình duyệt từ chuỗi User-Agent (đủ dùng cho thống kê, không cần thư viện ngoài).
/// Thứ tự kiểm tra quan trọng: trình duyệt nhúng (Zalo, Facebook) và Cốc Cốc/Edge/Opera phải kiểm trước Chrome/Safari.
/// </summary>
public static class ThietBiParser
{
    public const string MayTinh = "Máy tính";
    public const string DienThoai = "Điện thoại";
    public const string MayTinhBang = "Máy tính bảng";

    private static readonly Regex _bot = new(
        @"bot|crawl|spider|slurp|facebookexternalhit|facebookcatalog|preview|headless|lighthouse|pagespeed|monitor|curl|wget|python-requests|httpclient|go-http|java/|okhttp|scrapy|phantomjs|bingpreview|zgrab",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static ThongTinThietBi PhanTich(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua))
            return new ThongTinThietBi("Không xác định", "Không xác định", "Không xác định", true);

        var laBot = _bot.IsMatch(ua);

        // --- Hệ điều hành ---
        string heDieuHanh =
            ua.Contains("Windows", StringComparison.OrdinalIgnoreCase) ? "Windows" :
            ua.Contains("iPhone") || ua.Contains("iPod") ? "iOS" :
            ua.Contains("iPad") ? "iPadOS" :
            ua.Contains("Android", StringComparison.OrdinalIgnoreCase) ? "Android" :
            ua.Contains("CrOS") ? "ChromeOS" :
            ua.Contains("Mac OS X") || ua.Contains("Macintosh") ? "macOS" :
            ua.Contains("Linux", StringComparison.OrdinalIgnoreCase) ? "Linux" :
            "Khác";

        // --- Loại thiết bị ---
        string thietBi;
        if (ua.Contains("iPad") || ua.Contains("Tablet", StringComparison.OrdinalIgnoreCase) ||
            (heDieuHanh == "Android" && !ua.Contains("Mobile")))
            thietBi = MayTinhBang;
        else if (ua.Contains("Mobi", StringComparison.OrdinalIgnoreCase) || ua.Contains("iPhone") || ua.Contains("iPod") ||
                 heDieuHanh == "Android")
            thietBi = DienThoai;
        else
            thietBi = MayTinh;

        if (heDieuHanh == "iPadOS") heDieuHanh = "iOS";

        // --- Trình duyệt ---
        string trinhDuyet =
            ua.Contains("Zalo", StringComparison.OrdinalIgnoreCase) ? "Zalo" :
            ua.Contains("FBAN") || ua.Contains("FBAV") || ua.Contains("FB_IAB") ? "Facebook" :
            ua.Contains("Instagram") ? "Instagram" :
            ua.Contains("TikTok", StringComparison.OrdinalIgnoreCase) || ua.Contains("BytedanceWebview") ? "TikTok" :
            ua.Contains("coc_coc_browser") || ua.Contains("CocCoc") ? "Cốc Cốc" :
            ua.Contains("Edg/") || ua.Contains("EdgA/") || ua.Contains("EdgiOS/") ? "Edge" :
            ua.Contains("OPR/") || ua.Contains("Opera") ? "Opera" :
            ua.Contains("SamsungBrowser") ? "Samsung Internet" :
            ua.Contains("Firefox/") || ua.Contains("FxiOS/") ? "Firefox" :
            ua.Contains("Chrome/") || ua.Contains("CriOS/") ? "Chrome" :
            ua.Contains("Safari/") ? "Safari" :
            "Khác";

        return new ThongTinThietBi(thietBi, heDieuHanh, trinhDuyet, laBot);
    }
}
