using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using ThaiBeer.Core.Interfaces;
using ThaiBeer.ViewModels;

namespace ThaiBeer.Controllers;

public class SanPhamController : Controller
{
    private readonly ISanPhamReadRepository _readRepo;

    public SanPhamController(ISanPhamReadRepository readRepo)
    {
        _readRepo = readRepo;
    }

    [HttpGet("san-pham")]
    [HttpGet("danh-muc/{slug}")]
    [OutputCache(PolicyName = "SanPhamCatalogCache")]
    public async Task<IActionResult> Index(string? slug, string? tuKhoa, string? sapXep, int trang = 1)
    {
        if (trang < 1) trang = 1;
        const int soLuongMoiTrang = 12;

        var danhMucs = await _readRepo.LayDanhSachDanhMucHoatDongAsync();
        var danhMucHienTai = string.IsNullOrWhiteSpace(slug)
            ? null
            : danhMucs.FirstOrDefault(d => d.DuongDanSlug.Equals(slug, StringComparison.OrdinalIgnoreCase));

        var sanPhams = await _readRepo.LayDanhSachTheoDanhMucSlugAsync(slug ?? string.Empty, trang, soLuongMoiTrang, tuKhoa, sapXep);
        var tongSo = await _readRepo.DemSoLuongTheoDanhMucSlugAsync(slug ?? string.Empty, tuKhoa);

        var vm = new DanhMucSanPhamViewModel
        {
            DanhMucHienTai = danhMucHienTai,
            TatCaDanhMuc = danhMucs,
            DanhSachSanPham = sanPhams,
            TongSoSanPham = tongSo,
            TrangHienTai = trang,
            SoLuongMoiTrang = soLuongMoiTrang,
            TuKhoa = tuKhoa,
            SapXep = sapXep
        };

        return View(vm);
    }

    [HttpGet("san-pham/{slug}")]
    public async Task<IActionResult> ChiTiet(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var sanPham = await _readRepo.LayChiTietTheoSlugAsync(slug);
        if (sanPham == null)
            return NotFound();

        // Tăng lượt xem trong cột LuotXem của bảng SanPhams khi người dùng click xem chi tiết
        sanPham.LuotXem = await _readRepo.TangLuotXemSanPhamAsync(sanPham.Id);

        var sanPhamLienQuan = await _readRepo.LaySanPhamLienQuanAsync(sanPham.DanhMucId, sanPham.Id, 4);

        var vm = new SanPhamChiTietViewModel
        {
            SanPham = sanPham,
            SanPhamLienQuan = sanPhamLienQuan
        };

        return View(vm);
    }
}
