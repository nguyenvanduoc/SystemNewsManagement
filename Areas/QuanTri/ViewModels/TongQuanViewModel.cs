using ThaiBeer.Core.Domain.Entities;

namespace ThaiBeer.Areas.QuanTri.ViewModels;

public class TongQuanViewModel
{
    public int TongSanPham { get; set; }
    public int TongDanhMuc { get; set; }
    public int TongBaiViet { get; set; }
    public int TongLienHeMoiChuaXuLy { get; set; }

    public IReadOnlyList<SanPham> SanPhamMoi { get; set; } = new List<SanPham>();
    public IReadOnlyList<LienHe> LienHeMoiNhat { get; set; } = new List<LienHe>();
}
