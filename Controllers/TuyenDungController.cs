using Microsoft.AspNetCore.Mvc;
using ThaiBeer.Core.Domain.Entities;
using ThaiBeer.Infrastructure.Data;
using ThaiBeer.ViewModels;

namespace ThaiBeer.Controllers;

[Route("tuyen-dung")]
public class TuyenDungController : Controller
{
    private readonly ThaiBeerDbContext _context;

    public TuyenDungController(ThaiBeerDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View(new UngTuyenViewModel());
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(UngTuyenViewModel vm)
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
            DiaChiKhuVuc = vm.TinhThanh.Trim(),
            LoaiHopTac = $"[Ứng Tuyển] {vm.ViTriUngTuyen}",
            NoiDungGhiChu = string.IsNullOrWhiteSpace(vm.KinhNghiem)
                ? $"Ứng viên đăng ký vị trí: {vm.ViTriUngTuyen} tại {vm.TinhThanh}"
                : $"[Vị trí: {vm.ViTriUngTuyen} - Địa bàn: {vm.TinhThanh}]\n{vm.KinhNghiem.Trim()}",
            DaXuLy = false,
            NgayTao = DateTime.UtcNow
        };

        _context.LienHes.Add(lienHe);
        await _context.SaveChangesAsync();

        TempData["NopHoSoThanhCong"] = "Hồ sơ ứng tuyển của bạn đã được chuyển thành công đến Phòng Nhân sự ThaiBeer. Chuyên viên tuyển dụng sẽ liên hệ phỏng vấn trong vòng 24 - 48 giờ làm việc.";
        return Redirect("/tuyen-dung#form-ung-tuyen");
    }
}
