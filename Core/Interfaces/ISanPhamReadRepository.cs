using ThaiBeer.Core.Domain.Entities;

namespace ThaiBeer.Core.Interfaces;

public interface ISanPhamReadRepository
{
    Task<IReadOnlyList<SanPham>> LayDanhSachTrangChuAsync(int soLuong = 8);
    Task<IReadOnlyList<SanPham>> LayDanhSachTheoDanhMucSlugAsync(string slug, int trang = 1, int soLuong = 12);
    Task<int> DemSoLuongTheoDanhMucSlugAsync(string slug);
    Task<SanPham?> LayChiTietTheoSlugAsync(string slug);
    Task<IReadOnlyList<SanPham>> LaySanPhamLienQuanAsync(int danhMucId, int sanPhamHienTaiId, int soLuong = 4);
    Task<IReadOnlyList<BannerQuangCao>> LayBannerTrangChuAsync();
    Task<IReadOnlyList<DanhMuc>> LayDanhSachDanhMucHoatDongAsync();
    Task<IReadOnlyList<BaiViet>> LayTinTucNoiBatAsync(int soLuong = 3);
    Task<BaiViet?> LayChiTietBaiVietAsync(string slug);
    Task TangLuotXemSanPhamAsync(int id);
}
