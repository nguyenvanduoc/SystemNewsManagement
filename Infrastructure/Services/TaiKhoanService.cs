using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThaiBeer.Core.Domain.Common;
using ThaiBeer.Core.Domain.Entities;
using ThaiBeer.Core.Interfaces;
using ThaiBeer.Infrastructure.Data;

namespace ThaiBeer.Infrastructure.Services;

/// <summary>
/// Nghiệp vụ tài khoản quản trị: băm/kiểm tra mật khẩu, khóa tạm thời khi nhập sai, đổi/đặt lại mật khẩu.
/// Băm mật khẩu bằng PasswordHasher (ASP.NET Core Identity V3: PBKDF2-HMAC-SHA512, 100.000 vòng, salt 128-bit).
/// </summary>
public class TaiKhoanService : ITaiKhoanService
{
    public const string ClaimHoTen = "HoTen";
    public const string ClaimMaBaoMat = "MaBaoMat";
    public const string ClaimPhaiDoiMatKhau = "PhaiDoiMatKhau";

    /// <summary>Nhập sai quá số lần này thì tài khoản bị khóa tạm.</summary>
    public const int SoLanSaiToiDa = 5;
    public static readonly TimeSpan ThoiGianKhoa = TimeSpan.FromMinutes(15);
    public const int DoDaiMatKhauToiThieu = 8;

    private static readonly PasswordHasher<NguoiDung> _hasher = new();

    // Chuỗi băm giả: dùng khi tên đăng nhập không tồn tại để thời gian phản hồi giống hệt trường hợp sai mật khẩu
    // (tránh kẻ tấn công đoán được tên đăng nhập nào có thật dựa vào thời gian trả lời).
    private static readonly string _hashGia = _hasher.HashPassword(new NguoiDung(), Guid.NewGuid().ToString());

    private readonly ThaiBeerDbContext _context;

    public TaiKhoanService(ThaiBeerDbContext context)
    {
        _context = context;
    }

    public async Task<KetQuaDangNhap> DangNhapAsync(string tenDangNhap, string matKhau)
    {
        var ten = (tenDangNhap ?? string.Empty).Trim().ToLowerInvariant();
        var nd = await _context.NguoiDungs.FirstOrDefaultAsync(x => x.TenDangNhap == ten);

        if (nd == null)
        {
            _hasher.VerifyHashedPassword(new NguoiDung(), _hashGia, matKhau ?? string.Empty);
            return new KetQuaDangNhap(TrangThaiDangNhap.SaiThongTin);
        }

        if (!nd.TrangThaiHoatDong)
        {
            return new KetQuaDangNhap(TrangThaiDangNhap.BiVoHieuHoa);
        }

        if (nd.KhoaDenLuc.HasValue && nd.KhoaDenLuc.Value > DateTime.UtcNow)
        {
            return new KetQuaDangNhap(TrangThaiDangNhap.BiKhoaTam, KhoaDenLuc: nd.KhoaDenLuc);
        }

        var ketQua = _hasher.VerifyHashedPassword(nd, nd.MatKhauHash, matKhau ?? string.Empty);
        if (ketQua == PasswordVerificationResult.Failed)
        {
            nd.SoLanDangNhapSai++;
            if (nd.SoLanDangNhapSai >= SoLanSaiToiDa)
            {
                nd.KhoaDenLuc = DateTime.UtcNow.Add(ThoiGianKhoa);
                nd.SoLanDangNhapSai = 0;
                await _context.SaveChangesAsync();
                return new KetQuaDangNhap(TrangThaiDangNhap.BiKhoaTam, KhoaDenLuc: nd.KhoaDenLuc);
            }
            await _context.SaveChangesAsync();
            return new KetQuaDangNhap(TrangThaiDangNhap.SaiThongTin);
        }

        // Thuật toán băm được nâng cấp (vd. tăng số vòng lặp) -> băm lại theo chuẩn mới
        if (ketQua == PasswordVerificationResult.SuccessRehashNeeded)
        {
            nd.MatKhauHash = _hasher.HashPassword(nd, matKhau!);
        }

        nd.SoLanDangNhapSai = 0;
        nd.KhoaDenLuc = null;
        nd.LanDangNhapCuoi = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new KetQuaDangNhap(TrangThaiDangNhap.ThanhCong, nd);
    }

    public async Task<(bool ThanhCong, string? Loi)> TaoTaiKhoanAsync(
        string tenDangNhap, string matKhau, string hoTen, string? email, string vaiTro, bool phaiDoiMatKhau)
    {
        var ten = (tenDangNhap ?? string.Empty).Trim().ToLowerInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(ten, "^[a-z0-9._-]{3,50}$"))
            return (false, "Tên đăng nhập chỉ gồm chữ không dấu, số, dấu chấm, gạch dưới, gạch ngang (3-50 ký tự).");

        if (!VaiTroNguoiDung.HopLe(vaiTro))
            return (false, "Vai trò không hợp lệ.");

        var loiMatKhau = KiemTraDoManhMatKhau(matKhau);
        if (loiMatKhau != null)
            return (false, loiMatKhau);

        if (await _context.NguoiDungs.AnyAsync(x => x.TenDangNhap == ten))
            return (false, "Tên đăng nhập đã tồn tại.");

        var nd = new NguoiDung
        {
            TenDangNhap = ten,
            HoTen = string.IsNullOrWhiteSpace(hoTen) ? ten : hoTen.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            VaiTro = vaiTro,
            TrangThaiHoatDong = true,
            PhaiDoiMatKhau = phaiDoiMatKhau,
            MaBaoMat = Guid.NewGuid().ToString("N"),
            NgayTao = DateTime.UtcNow
        };
        nd.MatKhauHash = _hasher.HashPassword(nd, matKhau);

        _context.NguoiDungs.Add(nd);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool ThanhCong, string? Loi)> DoiMatKhauAsync(int nguoiDungId, string matKhauCu, string matKhauMoi)
    {
        var nd = await _context.NguoiDungs.FindAsync(nguoiDungId);
        if (nd == null || !nd.TrangThaiHoatDong)
            return (false, "Tài khoản không tồn tại hoặc đã bị vô hiệu hóa.");

        if (_hasher.VerifyHashedPassword(nd, nd.MatKhauHash, matKhauCu ?? string.Empty) == PasswordVerificationResult.Failed)
            return (false, "Mật khẩu hiện tại không đúng.");

        if (matKhauCu == matKhauMoi)
            return (false, "Mật khẩu mới phải khác mật khẩu hiện tại.");

        var loi = KiemTraDoManhMatKhau(matKhauMoi);
        if (loi != null)
            return (false, loi);

        GanMatKhauMoi(nd, matKhauMoi, phaiDoiMatKhau: false);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool ThanhCong, string? Loi)> DatLaiMatKhauAsync(int nguoiDungId, string matKhauMoi)
    {
        var nd = await _context.NguoiDungs.FindAsync(nguoiDungId);
        if (nd == null)
            return (false, "Tài khoản không tồn tại.");

        var loi = KiemTraDoManhMatKhau(matKhauMoi);
        if (loi != null)
            return (false, loi);

        GanMatKhauMoi(nd, matKhauMoi, phaiDoiMatKhau: true);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> PhienConHopLeAsync(int nguoiDungId, string? maBaoMat)
    {
        if (string.IsNullOrEmpty(maBaoMat))
            return false;

        return await _context.NguoiDungs.AsNoTracking()
            .AnyAsync(x => x.Id == nguoiDungId && x.TrangThaiHoatDong && x.MaBaoMat == maBaoMat);
    }

    public ClaimsPrincipal TaoPrincipal(NguoiDung nd)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, nd.Id.ToString()),
            new(ClaimTypes.Name, nd.TenDangNhap),
            new(ClaimTypes.Role, nd.VaiTro),
            new(ClaimHoTen, nd.HoTen),
            new(ClaimMaBaoMat, nd.MaBaoMat),
            new(ClaimPhaiDoiMatKhau, nd.PhaiDoiMatKhau ? "true" : "false"),
            new(ClaimTypes.AuthenticationInstant, DateTime.UtcNow.ToString("o"))
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public string? KiemTraDoManhMatKhau(string? matKhau)
    {
        if (string.IsNullOrEmpty(matKhau) || matKhau.Length < DoDaiMatKhauToiThieu)
            return $"Mật khẩu phải có ít nhất {DoDaiMatKhauToiThieu} ký tự.";
        if (matKhau.Length > 128)
            return "Mật khẩu tối đa 128 ký tự.";
        if (!matKhau.Any(char.IsLetter) || !matKhau.Any(char.IsDigit))
            return "Mật khẩu phải gồm cả chữ và số.";
        return null;
    }

    private static void GanMatKhauMoi(NguoiDung nd, string matKhauMoi, bool phaiDoiMatKhau)
    {
        nd.MatKhauHash = _hasher.HashPassword(nd, matKhauMoi);
        nd.PhaiDoiMatKhau = phaiDoiMatKhau;
        nd.SoLanDangNhapSai = 0;
        nd.KhoaDenLuc = null;
        nd.MaBaoMat = Guid.NewGuid().ToString("N"); // đăng xuất mọi phiên đăng nhập cũ
        nd.NgayCapNhat = DateTime.UtcNow;
    }
}
