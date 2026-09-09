using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ThaiBeer.Core.Domain.Common;

namespace ThaiBeer.Core.Domain.Entities;

[Table("LienHes")]
[Index(nameof(DaXuLy), nameof(NgayTao), Name = "IX_LienHes_DaXuLy_NgayTao")]
public class LienHe : BaseEntity
{
    [Required(ErrorMessage = "Họ và tên không được để trống")]
    [MaxLength(150)]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    [MaxLength(20)]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [Column(TypeName = "varchar(20)")]
    public string SoDienThoai { get; set; } = string.Empty;

    [MaxLength(150)]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Column(TypeName = "varchar(150)")]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? DiaChiKhuVuc { get; set; }

    [MaxLength(100)]
    public string? LoaiHopTac { get; set; } = "Đại lý phân phối"; // Nhà hàng, Khách sạn, Đại lý F&B

    [MaxLength(1000)]
    public string? NoiDungGhiChu { get; set; }

    public bool DaXuLy { get; set; } = false;

    [MaxLength(500)]
    public string? GhiChuNoiBo { get; set; }
}
