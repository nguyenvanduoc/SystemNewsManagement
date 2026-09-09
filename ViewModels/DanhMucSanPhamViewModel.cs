using ThaiBeer.Core.Domain.Entities;

namespace ThaiBeer.ViewModels;

public class DanhMucSanPhamViewModel
{
    public DanhMuc? DanhMucHienTai { get; set; }
    public IReadOnlyList<DanhMuc> TatCaDanhMuc { get; set; } = new List<DanhMuc>();
    public IReadOnlyList<SanPham> DanhSachSanPham { get; set; } = new List<SanPham>();
    public int TongSoSanPham { get; set; }
    public int TrangHienTai { get; set; } = 1;
    public int SoLuongMoiTrang { get; set; } = 12;
    public int TongSoTrang => (int)Math.Ceiling((double)TongSoSanPham / SoLuongMoiTrang);
}
