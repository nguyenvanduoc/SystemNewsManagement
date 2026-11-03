using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThaiBeer.Core.Domain.Entities;

/// <summary>
/// Một phiên truy cập của khách trên website (tính từ lúc vào tới khi rời đi / không hoạt động quá 30 phút).
/// Thời gian lưu theo UTC.
/// </summary>
[Table("PhienTruyCaps")]
public class PhienTruyCap
{
    public long Id { get; set; }

    /// <summary>Mã phiên do trình duyệt sinh (GUID), hết hạn sau 30 phút không hoạt động.</summary>
    [Required, MaxLength(36), Column(TypeName = "varchar(36)")]
    public string MaPhien { get; set; } = string.Empty;

    /// <summary>Mã khách ẩn danh lưu lâu dài trên trình duyệt (GUID) để đếm khách duy nhất / khách quay lại.</summary>
    [Required, MaxLength(36), Column(TypeName = "varchar(36)")]
    public string MaKhach { get; set; } = string.Empty;

    /// <summary>True nếu đây là phiên đầu tiên của mã khách này.</summary>
    public bool KhachMoi { get; set; }

    [MaxLength(45), Column(TypeName = "varchar(45)")]
    public string? DiaChiIp { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>Máy tính / Điện thoại / Máy tính bảng.</summary>
    [MaxLength(30)]
    public string ThietBi { get; set; } = string.Empty;

    [MaxLength(50)]
    public string HeDieuHanh { get; set; } = string.Empty;

    [MaxLength(50)]
    public string TrinhDuyet { get; set; } = string.Empty;

    /// <summary>Tỉnh/thành theo IP, hoặc "Nước ngoài" / "Không xác định".</summary>
    [MaxLength(100)]
    public string TinhThanh { get; set; } = string.Empty;

    /// <summary>Nguồn truy cập: tên miền giới thiệu (google.com, facebook.com...) hoặc "Truy cập trực tiếp".</summary>
    [MaxLength(200)]
    public string NguonTruyCap { get; set; } = string.Empty;

    /// <summary>Trang đầu tiên khách vào.</summary>
    [MaxLength(500)]
    public string TrangVao { get; set; } = string.Empty;

    [MaxLength(20), Column(TypeName = "varchar(20)")]
    public string? KichThuocManHinh { get; set; }

    public DateTime BatDau { get; set; }

    public DateTime HoatDongCuoi { get; set; }

    /// <summary>Tổng thời gian ở lại (giây) = tổng thời gian xem thực tế (tab đang hiển thị) của các trang.</summary>
    public int ThoiGianOGiay { get; set; }

    public int SoTrangXem { get; set; }

    public virtual ICollection<LuotXemTrang> LuotXemTrangs { get; set; } = new List<LuotXemTrang>();
}
