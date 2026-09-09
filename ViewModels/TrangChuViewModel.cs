using ThaiBeer.Core.Domain.Entities;

namespace ThaiBeer.ViewModels;

public class TrangChuViewModel
{
    public IReadOnlyList<BannerQuangCao> DanhSachBanner { get; set; } = new List<BannerQuangCao>();
    public IReadOnlyList<DanhMuc> DanhSachDanhMuc { get; set; } = new List<DanhMuc>();
    public IReadOnlyList<SanPham> SanPhamNoiBat { get; set; } = new List<SanPham>();
    public IReadOnlyList<BaiViet> TinTucNoiBat { get; set; } = new List<BaiViet>();
}
