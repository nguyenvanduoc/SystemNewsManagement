using ThaiBeer.Core.Domain.Entities;

namespace ThaiBeer.ViewModels;

public class SanPhamChiTietViewModel
{
    public SanPham SanPham { get; set; } = null!;
    public IReadOnlyList<SanPham> SanPhamLienQuan { get; set; } = new List<SanPham>();
}
