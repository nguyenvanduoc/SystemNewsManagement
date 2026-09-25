using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ThaiBeer.Core.Domain.Common;

namespace ThaiBeer.Core.Domain.Entities;

[Table("HinhAnhSanPhams")]
public class HinhAnhSanPham : BaseEntity
{
    [Required]
    public int SanPhamId { get; set; }

    [Required]
    [MaxLength(500)]
    [Column(TypeName = "varchar(500)")]
    public string DuongDanWebP { get; set; } = string.Empty;

    public int ThuTu { get; set; } = 1;

    [ForeignKey(nameof(SanPhamId))]
    public virtual SanPham? SanPham { get; set; }
}
