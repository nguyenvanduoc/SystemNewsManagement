using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using ThaiBeer.Core.Interfaces;

namespace ThaiBeer.Controllers;

public class TinTucController : Controller
{
    private readonly ISanPhamReadRepository _readRepo;

    public TinTucController(ISanPhamReadRepository readRepo)
    {
        _readRepo = readRepo;
    }

    [HttpGet("tin-tuc")]
    [OutputCache(PolicyName = "TinTucCache")]
    public async Task<IActionResult> Index()
    {
        var tinTuc = await _readRepo.LayTinTucNoiBatAsync(10);
        return View(tinTuc);
    }

    [HttpGet("tin-tuc/{slug}")]
    public async Task<IActionResult> ChiTiet(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var baiViet = await _readRepo.LayChiTietBaiVietAsync(slug);
        if (baiViet == null) return NotFound();

        // Tăng lượt xem bài viết trong cột LuotXem của bảng BaiViets
        baiViet.LuotXem = await _readRepo.TangLuotXemBaiVietAsync(baiViet.Id);

        var tinMoi = await _readRepo.LayTinTucNoiBatAsync(8);
        ViewBag.TinLienQuan = tinMoi.Where(t => t.Id != baiViet.Id).Take(5).ToList();

        return View(baiViet);
    }
}
