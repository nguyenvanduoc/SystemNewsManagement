using Microsoft.AspNetCore.Mvc;
using ThaiBeer.Core.Domain.Entities;
using ThaiBeer.Infrastructure.Data;
using ThaiBeer.ViewModels;

namespace ThaiBeer.Controllers;

[Route("lien-he")]
public class LienHeController : Controller
{
    private readonly ThaiBeerDbContext _context;

    public LienHeController(ThaiBeerDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View(new LienHeGuiYeuCauViewModel());
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LienHeGuiYeuCauViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var lienHe = new LienHe
        {
            HoTen = vm.HoTen.Trim(),
            SoDienThoai = vm.SoDienThoai.Trim(),
            Email = vm.Email?.Trim(),
            DiaChiKhuVuc = vm.DiaChiKhuVuc?.Trim(),
            LoaiHopTac = vm.LoaiHopTac,
            NoiDungGhiChu = vm.NoiDungGhiChu?.Trim(),
            DaXuLy = false,
            NgayTao = DateTime.UtcNow
        };

        _context.LienHes.Add(lienHe);
        await _context.SaveChangesAsync();

        TempData["GuiThanhCong"] = "Cảm ơn quý đối tác! Yêu cầu của bạn đã được tiếp nhận. Đội ngũ đại diện ThaiBeer sẽ liên hệ trực tiếp trong vòng 24 giờ.";
        return RedirectToAction(nameof(Index));
    }
}
