using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ThaiBeer.Core.Domain.Common;

namespace ThaiBeer.Core.Domain.Entities;

[Table("DanhMucs")]
[Index(nameof(DuongDanSlug), IsUnique = true, Name = "IX_DanhMucs_DuongDanSlug")]
[Index(nameof(TrangThaiHoatDong), nameof(ThuTuHienThi), Name = "IX_DanhMucs_TrangThai_ThuTu")]
public class DanhMuc : BaseEntity
{
    [Required(ErrorMessage = "Tên danh mục không được để trống")]
    [MaxLength(150, ErrorMessage = "Tên danh mục tối đa 150 ký tự")]
    public string TenDanhMuc { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đường dẫn slug không được để trống")]
    [MaxLength(150)]
    [Column(TypeName = "varchar(150)")]
    public string DuongDanSlug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? MoTa { get; set; }

    [MaxLength(500)]
    [Column(TypeName = "varchar(500)")]
    public string? HinhAnhWebP { get; set; }

    public int ThuTuHienThi { get; set; } = 0;

    public bool TrangThaiHoatDong { get; set; } = true;

    [MaxLength(30)]
    [Column(TypeName = "varchar(30)")]
    public string? ToneMau { get; set; } = "#007A29";

    // Navigation Property
    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}
