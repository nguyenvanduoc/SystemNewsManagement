using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ThaiBeer.Core.Domain.Common;

namespace ThaiBeer.Core.Domain.Entities;

/// <summary>
/// Tài khoản đăng nhập trang Quản trị.
/// Mật khẩu KHÔNG lưu dạng chữ thường: chỉ lưu chuỗi băm PBKDF2 (HMAC-SHA512, salt ngẫu nhiên)
/// sinh bởi PasswordHasher của ASP.NET Core Identity.
/// </summary>
[Table("NguoiDungs")]
public class NguoiDung : BaseEntity
{
    [Required, MaxLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string TenDangNhap { get; set; } = string.Empty;

    /// <summary>Chuỗi băm mật khẩu (đã gồm salt + số vòng lặp). Không bao giờ chứa mật khẩu gốc.</summary>
    [Required, MaxLength(500)]
    [Column(TypeName = "varchar(500)")]
    public string MatKhauHash { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string HoTen { get; set; } = string.Empty;

    [MaxLength(150)]
    [Column(TypeName = "varchar(150)")]
    public string? Email { get; set; }

    /// <summary>QuanTriVien (toàn quyền, quản lý tài khoản) hoặc BienTapVien (chỉ quản lý nội dung).</summary>
    [Required, MaxLength(30)]
    [Column(TypeName = "varchar(30)")]
    public string VaiTro { get; set; } = VaiTroNguoiDung.BienTapVien;

    public bool TrangThaiHoatDong { get; set; } = true;

    /// <summary>Bắt buộc đổi mật khẩu ở lần đăng nhập kế tiếp (tài khoản mới tạo / vừa đặt lại mật khẩu).</summary>
    public bool PhaiDoiMatKhau { get; set; } = true;

    /// <summary>Số lần nhập sai mật khẩu liên tiếp (dùng để khóa tạm thời chống dò mật khẩu).</summary>
    public int SoLanDangNhapSai { get; set; }

    /// <summary>Thời điểm (UTC) hết khóa tạm thời. Null = không bị khóa.</summary>
    public DateTime? KhoaDenLuc { get; set; }

    public DateTime? LanDangNhapCuoi { get; set; }

    /// <summary>
    /// Mã bảo mật (security stamp). Đổi mỗi khi đổi mật khẩu / khóa tài khoản,
    /// mọi phiên đăng nhập (cookie) cũ mang mã khác sẽ bị đăng xuất tự động.
    /// </summary>
    [Required, MaxLength(64)]
    [Column(TypeName = "varchar(64)")]
    public string MaBaoMat { get; set; } = Guid.NewGuid().ToString("N");
}
