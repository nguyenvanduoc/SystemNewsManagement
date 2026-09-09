using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ThaiBeer.Core.Domain.Common;

namespace ThaiBeer.Core.Domain.Entities;

[Table("BaiViets")]
[Index(nameof(DuongDanSlug), IsUnique = true, Name = "IX_BaiViets_DuongDanSlug")]
[Index(nameof(TrangThaiHoatDong), nameof(NgayTao), Name = "IX_BaiViets_TrangThai_NgayTao")]
public class BaiViet : BaseEntity
{
    [Required(ErrorMessage = "Tiêu đề bài viết không được để trống")]
    [MaxLength(300, ErrorMessage = "Tiêu đề tối đa 300 ký tự")]
    public string TieuDe { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug không được để trống")]
    [MaxLength(300)]
    [Column(TypeName = "varchar(300)")]
    public string DuongDanSlug { get; set; } = string.Empty;

    [MaxLength(600)]
    public string? TomTat { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? NoiDungHtml { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn hình ảnh WebP")]
    [MaxLength(500)]
    [Column(TypeName = "varchar(500)")]
    public string HinhAnhWebP { get; set; } = string.Empty;

    [MaxLength(100)]
    public string TacGia { get; set; } = "ThaiBeer Ban Biên Tập";

    public int LuotXem { get; set; } = 0;

    public bool NoiBat { get; set; } = false;

    public bool TrangThaiHoatDong { get; set; } = true;
}
