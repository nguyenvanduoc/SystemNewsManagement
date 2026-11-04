using System.Net;
using Microsoft.EntityFrameworkCore;
using ThaiBeer.Core.Domain.Entities;
using ThaiBeer.Infrastructure.Data;

namespace ThaiBeer.Infrastructure.Services;

public record YeuCauGhiNhanLuotXem(string MaKhach, string MaPhien, string DuongDan, string? TieuDe, string? NguonGioiThieu, string? ManHinh);

public record YeuCauCapNhatThoiGian(long Id, string MaPhien, int Giay);

/// <summary>
/// Ghi nhận lượt xem trang và thời gian ở lại do script /js/thong-ke.js gửi lên.
/// </summary>
public class ThongKeTruyCapService
{
    /// <summary>Không hoạt động quá thời gian này thì tính là phiên mới (giống Google Analytics).</summary>
    public static readonly TimeSpan HetHanPhien = TimeSpan.FromMinutes(30);

    /// <summary>Giới hạn thời gian ở lại 1 trang (chống số liệu ảo khi để tab mở cả ngày).</summary>
    public const int ThoiGianToiDaMotTrangGiay = 4 * 60 * 60;

    private static readonly string[] _duongDanBoQua = { "/quan-tri", "/adminthaibeer", "/api/" };

    private readonly ThaiBeerDbContext _context;
    private readonly IpViTriService _ipViTri;
    private readonly HangDoiViTriIp _hangDoiViTri;

    public ThongKeTruyCapService(ThaiBeerDbContext context, IpViTriService ipViTri, HangDoiViTriIp hangDoiViTri)
    {
        _context = context;
        _ipViTri = ipViTri;
        _hangDoiViTri = hangDoiViTri;
    }

    /// <summary>Ghi 1 lượt xem trang, tạo phiên mới nếu cần. Trả về Id lượt xem (null nếu bỏ qua: bot, dữ liệu sai...).</summary>
    public async Task<long?> GhiNhanLuotXemAsync(YeuCauGhiNhanLuotXem yc, IPAddress? ip, string? userAgent, string? tenMienWebsite)
    {
        if (!Guid.TryParse(yc.MaKhach, out _) || !Guid.TryParse(yc.MaPhien, out _))
            return null;

        var (duongDan, utmSource) = TachDuongDan(yc.DuongDan);
        if (duongDan == null || _duongDanBoQua.Any(x => duongDan.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
            return null;

        var ua = userAgent?.Length > 500 ? userAgent[..500] : userAgent;
        var tb = ThietBiParser.PhanTich(ua);
        if (tb.LaBot)
            return null;

        var now = DateTime.UtcNow;
        var phien = await _context.PhienTruyCaps
            .Where(p => p.MaPhien == yc.MaPhien)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        if (phien == null || phien.MaKhach != yc.MaKhach || now - phien.HoatDongCuoi > HetHanPhien)
        {
            var khachMoi = !await _context.PhienTruyCaps.AnyAsync(p => p.MaKhach == yc.MaKhach);
            var chuoiIp = (ip?.IsIPv4MappedToIPv6 == true ? ip.MapToIPv4() : ip)?.ToString();

            // Khu vực: ưu tiên kết quả ipinfo.io đã tra trước đó (chính xác hơn); chưa có thì dùng DB-IP offline
            // rồi đưa IP vào hàng đợi để tiến trình nền tra lại và sửa sau vài giây.
            var hanLuu = DateTime.UtcNow - ViTriIpNenService.HanLuu;
            var daTra = chuoiIp == null ? null : await _context.ViTriIps.AsNoTracking()
                .Where(v => v.DiaChiIp == chuoiIp && v.NgayTra >= hanLuu)
                .Select(v => v.TinhThanh).FirstOrDefaultAsync();
            if (daTra == null) _hangDoiViTri.Them(chuoiIp);
            phien = new PhienTruyCap
            {
                MaPhien = yc.MaPhien,
                MaKhach = yc.MaKhach,
                KhachMoi = khachMoi,
                DiaChiIp = chuoiIp,
                UserAgent = ua,
                ThietBi = tb.ThietBi,
                HeDieuHanh = tb.HeDieuHanh,
                TrinhDuyet = tb.TrinhDuyet,
                TinhThanh = daTra ?? _ipViTri.TraTinhThanh(ip),
                NguonTruyCap = XacDinhNguon(utmSource, yc.NguonGioiThieu, tenMienWebsite),
                TrangVao = duongDan,
                KichThuocManHinh = Cat(yc.ManHinh, 20),
                BatDau = now,
                HoatDongCuoi = now
            };
            _context.PhienTruyCaps.Add(phien);
        }

        var luot = new LuotXemTrang
        {
            PhienTruyCap = phien,
            DuongDan = duongDan,
            TieuDe = Cat(yc.TieuDe, 300),
            ThoiDiem = now
        };
        _context.LuotXemTrangs.Add(luot);
        phien.SoTrangXem++;
        phien.HoatDongCuoi = now;

        await _context.SaveChangesAsync();
        return luot.Id;
    }

    /// <summary>Cập nhật thời gian xem thực tế (giây, cộng dồn) của 1 lượt xem; chỉ tăng, không giảm.</summary>
    public async Task CapNhatThoiGianAsync(YeuCauCapNhatThoiGian yc)
    {
        var giay = Math.Clamp(yc.Giay, 0, ThoiGianToiDaMotTrangGiay);
        var luot = await _context.LuotXemTrangs
            .Include(l => l.PhienTruyCap)
            .FirstOrDefaultAsync(l => l.Id == yc.Id);

        // Bắt buộc khớp mã phiên: người ngoài không sửa được số liệu của lượt xem khác
        if (luot?.PhienTruyCap == null || luot.PhienTruyCap.MaPhien != yc.MaPhien || giay <= luot.ThoiGianOGiay)
            return;

        var tang = giay - luot.ThoiGianOGiay;
        luot.ThoiGianOGiay = giay;
        luot.PhienTruyCap.ThoiGianOGiay += tang;
        luot.PhienTruyCap.HoatDongCuoi = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private static (string? DuongDan, string? UtmSource) TachDuongDan(string? giaTri)
    {
        if (string.IsNullOrWhiteSpace(giaTri) || giaTri[0] != '/' || giaTri.StartsWith("//"))
            return (null, null);

        var viTriHoi = giaTri.IndexOf('?');
        var duongDan = viTriHoi >= 0 ? giaTri[..viTriHoi] : giaTri;
        string? utm = null;
        if (viTriHoi >= 0)
        {
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(giaTri[viTriHoi..]);
            if (query.TryGetValue("utm_source", out var v) && !string.IsNullOrWhiteSpace(v))
                utm = v.ToString();
        }
        return (Cat(duongDan, 500), utm);
    }

    /// <summary>Quy nguồn truy cập về tên dễ đọc: Google, Facebook, Zalo... hoặc tên miền; "Truy cập trực tiếp" nếu không có.</summary>
    private static string XacDinhNguon(string? utmSource, string? referrer, string? tenMienWebsite)
    {
        if (!string.IsNullOrWhiteSpace(utmSource))
            return Cat(utmSource.Trim().ToLowerInvariant(), 200)!;

        if (string.IsNullOrWhiteSpace(referrer) || !Uri.TryCreate(referrer, UriKind.Absolute, out var uri))
            return "Truy cập trực tiếp";

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.")) host = host[4..];
        if (!string.IsNullOrEmpty(tenMienWebsite) && host == tenMienWebsite.ToLowerInvariant().Replace("www.", ""))
            return "Truy cập trực tiếp";

        if (host.Contains("google.")) return "Google";
        if (host.Contains("facebook.com") || host == "fb.com" || host.EndsWith("fb.me")) return "Facebook";
        if (host.Contains("zalo")) return "Zalo";
        if (host.Contains("coccoc.com")) return "Cốc Cốc";
        if (host.Contains("bing.com")) return "Bing";
        if (host.Contains("youtube.com") || host == "youtu.be") return "YouTube";
        if (host.Contains("tiktok.com")) return "TikTok";
        if (host.Contains("instagram.com")) return "Instagram";
        return Cat(host, 200)!;
    }

    private static string? Cat(string? s, int max) => s == null ? null : (s.Length > max ? s[..max] : s);
}
