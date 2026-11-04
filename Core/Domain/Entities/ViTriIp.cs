using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThaiBeer.Core.Domain.Entities;

/// <summary>
/// Kết quả tra vị trí 1 địa chỉ IP qua dịch vụ ipinfo.io (chính xác cấp tỉnh hơn dữ liệu offline).
/// Lưu lại để mỗi IP chỉ tra 1 lần trong 30 ngày.
/// </summary>
[Table("ViTriIps")]
public class ViTriIp
{
    [Key, MaxLength(45), Column(TypeName = "varchar(45)")]
    public string DiaChiIp { get; set; } = string.Empty;

    /// <summary>Nhãn hiển thị ở cột Khu vực: tên tỉnh, "Mỹ", "Máy chủ Amazon (Mỹ)"...</summary>
    [Required, MaxLength(100)]
    public string TinhThanh { get; set; } = string.Empty;

    [MaxLength(2), Column(TypeName = "varchar(2)")]
    public string? MaNuoc { get; set; }

    /// <summary>Tên vùng gốc nguồn dữ liệu trả về (để đối chiếu).</summary>
    [MaxLength(100)]
    public string? VungGoc { get; set; }

    /// <summary>Nhà mạng / tổ chức sở hữu IP, vd "AS45899 VNPT Corp".</summary>
    [MaxLength(200)]
    public string? ToChuc { get; set; }

    /// <summary>IP thuộc máy chủ thuê (AWS, Google Cloud...) - thường là robot, không phải người thật.</summary>
    public bool LaMayChu { get; set; }

    public DateTime NgayTra { get; set; }
}
