using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace ThaiBeer.Infrastructure.Services;

/// <summary>
/// Tra địa chỉ IP -> tỉnh/thành Việt Nam, chạy hoàn toàn offline (không gọi dịch vụ bên ngoài).
/// Dữ liệu: App_Data/ip-vn.csv.gz (DB-IP Lite, CC BY 4.0), nạp vào RAM 1 lần (~1 MB), tra bằng tìm kiếm nhị phân.
/// Đăng ký Singleton.
/// </summary>
public class IpViTriService
{
    public const string NuocNgoai = "Nước ngoài";
    public const string KhongXacDinh = "Không xác định";
    public const string VietNamChuaRoTinh = "Việt Nam (chưa rõ tỉnh)";

    private readonly Lazy<DuLieu> _duLieu;

    public IpViTriService(IWebHostEnvironment env, ILogger<IpViTriService> logger)
    {
        var duongDan = Path.Combine(env.ContentRootPath, "App_Data", "ip-vn.csv.gz");
        _duLieu = new Lazy<DuLieu>(() => NapDuLieu(duongDan, logger), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Trả về tên tỉnh/thành, "Nước ngoài", hoặc "Không xác định" (IP nội bộ / lỗi).</summary>
    public string TraTinhThanh(IPAddress? ip)
    {
        if (ip == null)
            return KhongXacDinh;

        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();

        if (LaIpNoiBo(ip))
            return KhongXacDinh;

        var dl = _duLieu.Value;
        if (!dl.CoDuLieu)
            return KhongXacDinh;

        string? ketQua = ip.AddressFamily == AddressFamily.InterNetwork
            ? Tim(dl.V4BatDau, dl.V4KetThuc, dl.V4Ten, ToUInt32(ip))
            : Tim(dl.V6BatDau, dl.V6KetThuc, dl.V6Ten, ToUInt128(ip));

        if (ketQua == null)
            return NuocNgoai;
        return ketQua.Length == 0 ? VietNamChuaRoTinh : ketQua;
    }

    private static string? Tim<T>(T[] batDau, T[] ketThuc, string[] ten, T giaTri) where T : IComparable<T>
    {
        // Tìm dải cuối cùng có batDau <= giaTri
        int lo = 0, hi = batDau.Length - 1, viTri = -1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            if (batDau[mid].CompareTo(giaTri) <= 0) { viTri = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        return viTri >= 0 && giaTri.CompareTo(ketThuc[viTri]) <= 0 ? ten[viTri] : null;
    }

    private static bool LaIpNoiBo(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254);
        }
        return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal;
    }

    private static uint ToUInt32(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }

    private static UInt128 ToUInt128(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        UInt128 v = 0;
        foreach (var x in b) v = (v << 8) | x;
        return v;
    }

    private sealed class DuLieu
    {
        public bool CoDuLieu;
        public uint[] V4BatDau = Array.Empty<uint>(), V4KetThuc = Array.Empty<uint>();
        public string[] V4Ten = Array.Empty<string>();
        public UInt128[] V6BatDau = Array.Empty<UInt128>(), V6KetThuc = Array.Empty<UInt128>();
        public string[] V6Ten = Array.Empty<string>();
    }

    private static DuLieu NapDuLieu(string duongDan, ILogger logger)
    {
        var dl = new DuLieu();
        if (!File.Exists(duongDan))
        {
            logger.LogWarning("Không tìm thấy dữ liệu IP tỉnh thành tại {DuongDan}. Thống kê sẽ ghi khu vực là 'Không xác định'.", duongDan);
            return dl;
        }

        try
        {
            var v4b = new List<uint>(); var v4k = new List<uint>(); var v4t = new List<string>();
            var v6b = new List<UInt128>(); var v6k = new List<UInt128>(); var v6t = new List<string>();
            var tenDungChung = new Dictionary<string, string>(); // dùng chung chuỗi tên tỉnh để tiết kiệm RAM

            using var fs = File.OpenRead(duongDan);
            using var gz = new GZipStream(fs, CompressionMode.Decompress);
            using var reader = new StreamReader(gz);
            string? dong;
            while ((dong = reader.ReadLine()) != null)
            {
                if (dong.Length == 0 || dong[0] == '#') continue;
                var p = dong.Split(',', 4);
                if (p.Length < 4) continue;
                if (!tenDungChung.TryGetValue(p[3], out var ten)) { ten = p[3]; tenDungChung[ten] = ten; }

                if (p[0] == "4")
                {
                    v4b.Add(uint.Parse(p[1], NumberStyles.HexNumber));
                    v4k.Add(uint.Parse(p[2], NumberStyles.HexNumber));
                    v4t.Add(ten);
                }
                else
                {
                    v6b.Add(UInt128.Parse(p[1], NumberStyles.HexNumber));
                    v6k.Add(UInt128.Parse(p[2], NumberStyles.HexNumber));
                    v6t.Add(ten);
                }
            }

            dl.V4BatDau = v4b.ToArray(); dl.V4KetThuc = v4k.ToArray(); dl.V4Ten = v4t.ToArray();
            dl.V6BatDau = v6b.ToArray(); dl.V6KetThuc = v6k.ToArray(); dl.V6Ten = v6t.ToArray();
            dl.CoDuLieu = true;
            logger.LogInformation("Đã nạp {V4} dải IPv4 và {V6} dải IPv6 Việt Nam cho thống kê truy cập.", dl.V4BatDau.Length, dl.V6BatDau.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi đọc dữ liệu IP tỉnh thành {DuongDan}.", duongDan);
            dl = new DuLieu();
        }
        return dl;
    }
}
