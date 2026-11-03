using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThaiBeer.Areas.QuanTri.ViewModels;
using ThaiBeer.Core.Domain.Common;
using ThaiBeer.Core.Interfaces;
using ThaiBeer.Infrastructure.Data;
using ThaiBeer.Infrastructure.Services;

namespace ThaiBeer.Areas.QuanTri.Controllers;

[Authorize]
[Area("QuanTri")]
[Route("quan-tri/tai-khoan")]
public class TaiKhoanController : Controller
{
    private readonly ThaiBeerDbContext _context;
    private readonly ITaiKhoanService _taiKhoanService;

    public TaiKhoanController(ThaiBeerDbContext context, ITaiKhoanService taiKhoanService)
    {
        _context = context;
        _taiKhoanService = taiKhoanService;
    }

    private int IdHienTai => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ===================== DANH SÁCH TÀI KHOẢN (chỉ Quản trị viên) =====================

    [HttpGet("")]
    [Authorize(Roles = VaiTroNguoiDung.QuanTriVien)]
    public async Task<IActionResult> Index()
    {
        var danhSach = await _context.NguoiDungs.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        ViewBag.IdHienTai = IdHienTai;
        return View(danhSach);
    }

    // ===================== THÊM TÀI KHOẢN =====================

    [HttpGet("them-moi")]
    [Authorize(Roles = VaiTroNguoiDung.QuanTriVien)]
    public IActionResult ThemMoi() => View(new TaiKhoanThemMoiViewModel());

    [HttpPost("them-moi")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = VaiTroNguoiDung.QuanTriVien)]
    public async Task<IActionResult> ThemMoi(TaiKhoanThemMoiViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        // Tài khoản mới luôn phải tự đổi mật khẩu ở lần đăng nhập đầu
        var (ok, loi) = await _taiKhoanService.TaoTaiKhoanAsync(vm.TenDangNhap, vm.MatKhau, vm.HoTen, vm.Email, vm.VaiTro, phaiDoiMatKhau: true);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, loi!);
            return View(vm);
        }

        TempData["ThongBaoThanhCong"] = $"Đã tạo tài khoản '{vm.TenDangNhap.Trim().ToLowerInvariant()}'. Người dùng sẽ được yêu cầu đổi mật khẩu khi đăng nhập lần đầu.";
        return RedirectToAction(nameof(Index));
    }

    // ===================== ĐẶT LẠI MẬT KHẨU CHO NGƯỜI KHÁC =====================

    [HttpGet("dat-lai-mat-khau/{id:int}")]
    [Authorize(Roles = VaiTroNguoiDung.QuanTriVien)]
    public async Task<IActionResult> DatLaiMatKhau(int id)
    {
        var nd = await _context.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (nd == null)
            return NotFound();

        return View(new DatLaiMatKhauViewModel { Id = nd.Id, TenDangNhap = nd.TenDangNhap });
    }

    [HttpPost("dat-lai-mat-khau/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = VaiTroNguoiDung.QuanTriVien)]
    public async Task<IActionResult> DatLaiMatKhau(int id, DatLaiMatKhauViewModel vm)
    {
        if (id != vm.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(vm);

        var (ok, loi) = await _taiKhoanService.DatLaiMatKhauAsync(id, vm.MatKhauMoi);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, loi!);
            return View(vm);
        }

        if (id == IdHienTai)
        {
            // Tự đặt lại mật khẩu của chính mình => phiên hiện tại hết hạn, đăng nhập lại
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/adminthaibeer");
        }

        TempData["ThongBaoThanhCong"] = $"Đã đặt lại mật khẩu cho '{vm.TenDangNhap}'. Mọi phiên đăng nhập cũ của tài khoản này đã bị đăng xuất.";
        return RedirectToAction(nameof(Index));
    }

    // ===================== KHÓA / KÍCH HOẠT TÀI KHOẢN =====================

    [HttpPost("doi-trang-thai/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = VaiTroNguoiDung.QuanTriVien)]
    public async Task<IActionResult> DoiTrangThai(int id)
    {
        var nd = await _context.NguoiDungs.FindAsync(id);
        if (nd == null)
            return NotFound();

        if (id == IdHienTai)
        {
            TempData["ThongBaoLoi"] = "Không thể vô hiệu hóa chính tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        if (nd.TrangThaiHoatDong && nd.VaiTro == VaiTroNguoiDung.QuanTriVien)
        {
            var soQuanTriConLai = await _context.NguoiDungs.CountAsync(x =>
                x.Id != id && x.TrangThaiHoatDong && x.VaiTro == VaiTroNguoiDung.QuanTriVien);
            if (soQuanTriConLai == 0)
            {
                TempData["ThongBaoLoi"] = "Phải còn ít nhất 1 Quản trị viên đang hoạt động.";
                return RedirectToAction(nameof(Index));
            }
        }

        nd.TrangThaiHoatDong = !nd.TrangThaiHoatDong;
        nd.SoLanDangNhapSai = 0;
        nd.KhoaDenLuc = null;
        nd.MaBaoMat = Guid.NewGuid().ToString("N"); // đăng xuất ngay mọi phiên của tài khoản này
        nd.NgayCapNhat = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["ThongBaoThanhCong"] = nd.TrangThaiHoatDong
            ? $"Đã kích hoạt lại tài khoản '{nd.TenDangNhap}'."
            : $"Đã vô hiệu hóa tài khoản '{nd.TenDangNhap}'.";
        return RedirectToAction(nameof(Index));
    }

    // ===================== MỞ KHÓA TẠM (do nhập sai mật khẩu nhiều lần) =====================

    [HttpPost("mo-khoa/{id:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = VaiTroNguoiDung.QuanTriVien)]
    public async Task<IActionResult> MoKhoa(int id)
    {
        var nd = await _context.NguoiDungs.FindAsync(id);
        if (nd == null)
            return NotFound();

        nd.SoLanDangNhapSai = 0;
        nd.KhoaDenLuc = null;
        nd.NgayCapNhat = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["ThongBaoThanhCong"] = $"Đã mở khóa tài khoản '{nd.TenDangNhap}'.";
        return RedirectToAction(nameof(Index));
    }

    // ===================== TỰ ĐỔI MẬT KHẨU (mọi tài khoản) =====================

    [HttpGet("doi-mat-khau")]
    public IActionResult DoiMatKhau()
    {
        return View(new DoiMatKhauViewModel
        {
            BatBuoc = User.FindFirstValue(TaiKhoanService.ClaimPhaiDoiMatKhau) == "true"
        });
    }

    [HttpPost("doi-mat-khau")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiMatKhau(DoiMatKhauViewModel vm)
    {
        vm.BatBuoc = User.FindFirstValue(TaiKhoanService.ClaimPhaiDoiMatKhau) == "true";
        if (!ModelState.IsValid)
            return View(vm);

        // Lấy thuộc tính phiên hiện tại (ghi nhớ đăng nhập, hạn dùng) TRƯỚC khi đổi mã bảo mật
        var phienHienTai = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        var (ok, loi) = await _taiKhoanService.DoiMatKhauAsync(IdHienTai, vm.MatKhauHienTai, vm.MatKhauMoi);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, loi!);
            return View(vm);
        }

        // Mã bảo mật đã đổi => cấp lại cookie mới cho phiên hiện tại; các thiết bị khác bị đăng xuất
        var nd = await _context.NguoiDungs.AsNoTracking().FirstAsync(x => x.Id == IdHienTai);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            _taiKhoanService.TaoPrincipal(nd),
            phienHienTai.Properties ?? new AuthenticationProperties());

        TempData["ThongBaoThanhCong"] = "Đổi mật khẩu thành công! Các thiết bị khác đang đăng nhập tài khoản này đã bị đăng xuất.";
        return Redirect("/quan-tri");
    }
}
