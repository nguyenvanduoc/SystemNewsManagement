using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ThaiBeer.Core.Domain.Common;

namespace ThaiBeer.Core.Domain.Entities;

[Table("BannerQuangCaos")]
[Index(nameof(TrangThaiHoatDong), nameof(ThuTuHienThi), Name = "IX_BannerQuangCaos_TrangThai_ThuTu")]
public class BannerQuangCao : BaseEntity
{
    [Required(ErrorMessage = "Tiêu đề banner không được để trống")]
    [MaxLength(200)]
    public string TieuDe { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? PhuDe { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập đường dẫn hình ảnh WebP")]
    [MaxLength(500)]
    [Column(TypeName = "varchar(500)")]
    public string HinhAnhWebP { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column(TypeName = "varchar(500)")]
    public string? HinhAnhMobileWebP { get; set; }

    [MaxLength(500)]
    [Column(TypeName = "varchar(500)")]
    public string? LienKetUrl { get; set; }

    [MaxLength(100)]
    public string? TextNutBam { get; set; } = "Khám Phá Ngay";

    public int ThuTuHienThi { get; set; } = 0;

    public bool TrangThaiHoatDong { get; set; } = true;
}
