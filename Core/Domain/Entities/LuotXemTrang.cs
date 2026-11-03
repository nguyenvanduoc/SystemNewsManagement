using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThaiBeer.Core.Domain.Entities;

/// <summary>Một lượt xem trang trong một phiên truy cập (UTC).</summary>
[Table("LuotXemTrangs")]
public class LuotXemTrang
{
    public long Id { get; set; }

    public long PhienTruyCapId { get; set; }

    [Required, MaxLength(500)]
    public string DuongDan { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? TieuDe { get; set; }

    public DateTime ThoiDiem { get; set; }

    /// <summary>Thời gian xem thực tế trang này (giây, chỉ tính lúc tab đang hiển thị).</summary>
    public int ThoiGianOGiay { get; set; }

    public virtual PhienTruyCap? PhienTruyCap { get; set; }
}
