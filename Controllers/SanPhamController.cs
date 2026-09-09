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
    public async Task<IActionResult> Index(string? slug, int trang = 1)
    {
        if (trang < 1) trang = 1;
        const int soLuongMoiTrang = 12;

        var danhMucs = await _readRepo.LayDanhSachDanhMucHoatDongAsync();
        var danhMucHienTai = string.IsNullOrWhiteSpace(slug)
            ? null
            : danhMucs.FirstOrDefault(d => d.DuongDanSlug.Equals(slug, StringComparison.OrdinalIgnoreCase));

        var sanPhams = await _readRepo.LayDanhSachTheoDanhMucSlugAsync(slug ?? string.Empty, trang, soLuongMoiTrang);
        var tongSo = await _readRepo.DemSoLuongTheoDanhMucSlugAsync(slug ?? string.Empty);

        var vm = new DanhMucSanPhamViewModel
        {
            DanhMucHienTai = danhMucHienTai,
            TatCaDanhMuc = danhMucs,
            DanhSachSanPham = sanPhams,
            TongSoSanPham = tongSo,
            TrangHienTai = trang,
            SoLuongMoiTrang = soLuongMoiTrang
        };

        return View(vm);
    }

    [HttpGet("san-pham/{slug}")]
    [OutputCache(PolicyName = "SanPhamChiTietCache")]
    public async Task<IActionResult> ChiTiet(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var sanPham = await _readRepo.LayChiTietTheoSlugAsync(slug);
        if (sanPham == null)
            return NotFound();

        // Tăng lượt xem (chạy background không chặn render)
        _ = _readRepo.TangLuotXemSanPhamAsync(sanPham.Id);

        var sanPhamLienQuan = await _readRepo.LaySanPhamLienQuanAsync(sanPham.DanhMucId, sanPham.Id, 4);

        var vm = new SanPhamChiTietViewModel
        {
            SanPham = sanPham,
            SanPhamLienQuan = sanPhamLienQuan
        };

        return View(vm);
    }
}
