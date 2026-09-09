using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using ThaiBeer.Core.Interfaces;
using ThaiBeer.ViewModels;

namespace ThaiBeer.Controllers;

public class TrangChuController : Controller
{
    private readonly ISanPhamReadRepository _readRepo;

    public TrangChuController(ISanPhamReadRepository readRepo)
    {
        _readRepo = readRepo;
    }

    [HttpGet("")]
    [OutputCache(PolicyName = "TrangChuCache")]
    public async Task<IActionResult> Index()
    {
        var banners = await _readRepo.LayBannerTrangChuAsync();
        var danhMucs = await _readRepo.LayDanhSachDanhMucHoatDongAsync();
        var sanPhams = await _readRepo.LayDanhSachTrangChuAsync(8);
        var tinTuc = await _readRepo.LayTinTucNoiBatAsync(3);

        var viewModel = new TrangChuViewModel
        {
            DanhSachBanner = banners,
            DanhSachDanhMuc = danhMucs,
            SanPhamNoiBat = sanPhams,
            TinTucNoiBat = tinTuc
        };

        return View(viewModel);
    }
}
