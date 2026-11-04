using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThaiBeer.Core.Domain.Entities;
using ThaiBeer.Infrastructure.Data;

namespace ThaiBeer.Infrastructure.Services;

/// <summary>Hàng đợi IP cần tra lại vị trí (Singleton). Ghi nhận lượt xem chỉ đẩy IP vào đây rồi trả lời ngay.</summary>
public class HangDoiViTriIp
{
    private readonly Channel<string> _kenh = Channel.CreateBounded<string>(
        new BoundedChannelOptions(2000) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    public void Them(string? ip)
    {
        if (!string.IsNullOrEmpty(ip)) _kenh.Writer.TryWrite(ip);
    }

    public ChannelReader<string> Doc => _kenh.Reader;
}

/// <summary>
/// Chạy nền: tra vị trí IP bằng ipinfo.io (chính xác cấp tỉnh tốt hơn DB-IP với IP nhà mạng Việt Nam),
/// lưu bảng ViTriIps và sửa cột Khu vực của các phiên truy cập gần đây cùng IP.
/// Lỗi mạng / hết lượt tra thì giữ nguyên kết quả DB-IP, không ảnh hưởng website.
/// Cấu hình (appsettings.json, mục "ThongKe"): DungIpInfo (mặc định false), IpInfoToken (token miễn phí tại ipinfo.io, không bắt buộc).
/// ⚠️ Bật DungIpInfo = gửi địa chỉ IP của khách (dữ liệu cá nhân - Nghị định 13/2023) sang ipinfo.io (Mỹ):
///    cần ghi rõ trong chính sách bảo mật của website trước khi bật.
/// </summary>
public class ViTriIpNenService : BackgroundService
{
    public const string TenHttpClient = "ipinfo";
    public static readonly TimeSpan HanLuu = TimeSpan.FromDays(30);

    // Tổ chức sở hữu IP là nhà cung cấp máy chủ/đám mây -> thường là robot
    private static readonly string[] _mayChu =
    {
        "amazon", "aws", "google llc", "google cloud", "microsoft", "azure", "digitalocean", "ovh", "hetzner", "linode",
        "akamai connected cloud", "vultr", "choopa", "alibaba", "tencent", "oracle", "anthropic", "contabo", "leaseweb",
        "scaleway", "m247", "datacamp", "huawei cloud", "cloudflare warp",
    };

    private readonly HangDoiViTriIp _hangDoi;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<ViTriIpNenService> _logger;
    private readonly bool _batDung;
    private readonly string? _token;
    private DateTime _tamDungDen = DateTime.MinValue;

    public ViTriIpNenService(HangDoiViTriIp hangDoi, IServiceScopeFactory scopeFactory, IHttpClientFactory httpFactory,
        IConfiguration config, ILogger<ViTriIpNenService> logger)
    {
        _hangDoi = hangDoi;
        _scopeFactory = scopeFactory;
        _httpFactory = httpFactory;
        _logger = logger;
        _batDung = config.GetValue("ThongKe:DungIpInfo", false); // TẮT mặc định: chỉ bật khi chủ website đồng ý gửi IP khách cho ipinfo.io
        _token = config["ThongKe:IpInfoToken"];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_batDung) return;

        // Tra bù các IP của 30 ngày gần đây chưa có kết quả (chạy 1 lần khi khởi động)
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ThaiBeerDbContext>();
            var tu = DateTime.UtcNow.AddDays(-30);
            var ipChuaTra = await db.PhienTruyCaps.AsNoTracking()
                .Where(p => p.BatDau >= tu && p.DiaChiIp != null && !db.ViTriIps.Any(v => v.DiaChiIp == p.DiaChiIp))
                .Select(p => p.DiaChiIp!).Distinct().OrderBy(x => x).Take(1000).ToListAsync(stoppingToken);
            foreach (var ip in ipChuaTra) _hangDoi.Them(ip);
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex) { _logger.LogWarning(ex, "Không lấy được danh sách IP cần tra bù."); }

        await foreach (var ip in _hangDoi.Doc.ReadAllAsync(stoppingToken))
        {
            try
            {
                await XuLyAsync(ip, stoppingToken);
                await Task.Delay(300, stoppingToken); // nhịp nhẹ, tránh vượt giới hạn dịch vụ
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex) { _logger.LogWarning(ex, "Lỗi tra vị trí IP {Ip}", ip); }
        }
    }

    private async Task XuLyAsync(string ip, CancellationToken ct)
    {
        if (!IPAddress.TryParse(ip, out var diaChi) || !LaIpCongCong(diaChi) || DateTime.UtcNow < _tamDungDen)
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ThaiBeerDbContext>();

        var daCo = await db.ViTriIps.FirstOrDefaultAsync(v => v.DiaChiIp == ip, ct);
        if (daCo == null || DateTime.UtcNow - daCo.NgayTra > HanLuu)
        {
            var moi = await TraIpInfoAsync(ip, ct);
            if (moi == null) return;
            if (daCo == null) { db.ViTriIps.Add(moi); daCo = moi; }
            else { db.Entry(daCo).CurrentValues.SetValues(moi); }
            await db.SaveChangesAsync(ct);
        }

        // Sửa cột Khu vực cho các phiên 2 ngày gần đây của IP này (IP nhà mạng có thể đổi chủ theo thời gian)
        var tu = DateTime.UtcNow.AddDays(-2);
        var nhan = daCo.TinhThanh;
        await db.PhienTruyCaps
            .Where(p => p.DiaChiIp == ip && p.BatDau >= tu && p.TinhThanh != nhan)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.TinhThanh, nhan), ct);
    }

    private async Task<ViTriIp?> TraIpInfoAsync(string ip, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient(TenHttpClient);
        var url = $"https://ipinfo.io/{ip}/json" + (string.IsNullOrWhiteSpace(_token) ? "" : $"?token={Uri.EscapeDataString(_token)}");
        using var resp = await http.GetAsync(url, ct);
        if (resp.StatusCode == HttpStatusCode.TooManyRequests || resp.StatusCode == HttpStatusCode.Forbidden)
        {
            _tamDungDen = DateTime.UtcNow.AddHours(1);
            _logger.LogWarning("ipinfo.io từ chối ({Status}) - tạm dừng tra 1 giờ. Nên khai báo ThongKe:IpInfoToken (miễn phí 50.000 lượt/tháng).", (int)resp.StatusCode);
            return null;
        }
        if (!resp.IsSuccessStatusCode) return null;

        using var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var r = json.RootElement;
        string? Doc(string ten) => r.TryGetProperty(ten, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        if (r.TryGetProperty("bogon", out var bogon) && bogon.ValueKind == JsonValueKind.True) return null;

        var maNuoc = Doc("country");
        var vung = Doc("region");
        var toChuc = Doc("org");
        var laMayChu = toChuc != null && _mayChu.Any(k => toChuc.Contains(k, StringComparison.OrdinalIgnoreCase));

        string nhan;
        if (laMayChu)
            nhan = $"Máy chủ {TenToChucNgan(toChuc!)} ({TinhThanhVietNam.TenNuoc(maNuoc)})";
        else if (string.Equals(maNuoc, "VN", StringComparison.OrdinalIgnoreCase))
            nhan = TinhThanhVietNam.ChuanHoaTinh(vung) ?? TinhThanhVietNam.ChuanHoaTinh(Doc("city")) ?? IpViTriService.VietNamChuaRoTinh;
        else
            nhan = $"{IpViTriService.NuocNgoai} ({TinhThanhVietNam.TenNuoc(maNuoc)})";

        return new ViTriIp
        {
            DiaChiIp = ip,
            TinhThanh = nhan.Length > 100 ? nhan[..100] : nhan,
            MaNuoc = maNuoc?.Length == 2 ? maNuoc : null,
            VungGoc = vung?.Length > 100 ? vung[..100] : vung,
            ToChuc = toChuc?.Length > 200 ? toChuc[..200] : toChuc,
            LaMayChu = laMayChu,
            NgayTra = DateTime.UtcNow
        };
    }

    /// <summary>"AS16509 Amazon.com, Inc." -> "Amazon"</summary>
    private static string TenToChucNgan(string org)
    {
        var s = org.StartsWith("AS", StringComparison.OrdinalIgnoreCase) && org.Contains(' ') ? org[(org.IndexOf(' ') + 1)..] : org;
        s = s.Split(',', '.')[0].Trim();
        foreach (var duoi in new[] { " LLC", " Inc", " Ltd", " Corp", " Corporation", " Co", " GmbH", " Limited" })
            if (s.EndsWith(duoi, StringComparison.OrdinalIgnoreCase)) s = s[..^duoi.Length].Trim();
        return s.Length > 40 ? s[..40] : s;
    }

    private static bool LaIpCongCong(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip)) return false;
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return !(b[0] == 10 || b[0] == 0 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) ||
                     (b[0] == 169 && b[1] == 254) || (b[0] == 100 && b[1] >= 64 && b[1] <= 127));
        }
        return !(ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal);
    }
}
