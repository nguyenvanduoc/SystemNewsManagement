using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ThaiBeer.Core.Domain.Common;

namespace ThaiBeer.Core.Domain.Entities;

[Table("SanPhams")]
[Index(nameof(DuongDanSlug), IsUnique = true, Name = "IX_SanPhams_DuongDanSlug")]
[Index(nameof(DanhMucId), Name = "IX_SanPhams_DanhMucId")]
[Index(nameof(TrangThaiHoatDong), nameof(HienThiTrangChu), nameof(ThuTuHienThi), Name = "IX_SanPhams_LocTrangChu")]
public class SanPham : BaseEntity
{
    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    [MaxLength(250, ErrorMessage = "Tên sản phẩm tối đa 250 ký tự")]
    public string TenSanPham { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đường dẫn slug không được để trống")]
    [MaxLength(250)]
    [Column(TypeName = "varchar(250)")]
    public string DuongDanSlug { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Mô tả ngắn tối đa 500 ký tự")]
    public string? MoTaNgan { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? NoiDungChiTiet { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn hoặc tải lên hình ảnh WebP")]
    [MaxLength(500)]
    [Column(TypeName = "varchar(500)")]
    public string HinhAnhWebP { get; set; } = string.Empty;

    [MaxLength(1000)]
    [Column(TypeName = "varchar(1000)")]
    public string? DanhSachHinhAnhPhu { get; set; }

    [MaxLength(500)]
    [Column(TypeName = "varchar(500)")]
    public string? VideoGioiThieuUrl { get; set; }

    [Column(TypeName = "decimal(4,2)")]
    public decimal NongDoCon { get; set; } = 5.0m;

    public int DungTichMl { get; set; } = 330;

    [MaxLength(100)]
    public string XuatXu { get; set; } = "Thái Lan";

    [MaxLength(150)]
    public string QuyCachDongGoi { get; set; } = "Thùng 24 lon 330ml";

    public bool HienThiTrangChu { get; set; } = true;

    public bool TrangThaiHoatDong { get; set; } = true;

    public int ThuTuHienThi { get; set; } = 0;

    public int LuotXem { get; set; } = 0;

    // Khóa ngoại tới DanhMuc
    public int DanhMucId { get; set; }

    [ForeignKey(nameof(DanhMucId))]
    public virtual DanhMuc? DanhMuc { get; set; }

    // Danh sách tối đa 4 hình ảnh chi tiết đính kèm
    public virtual ICollection<HinhAnhSanPham> HinhAnhPhus { get; set; } = new List<HinhAnhSanPham>();
}
