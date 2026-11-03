using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using ThaiBeer.Core.Interfaces;
using ThaiBeer.ViewModels;

namespace ThaiBeer.Controllers;

[Route("adminthaibeer")]
public class AdminThaiBeerController : Controller
{
    private readonly ITaiKhoanService _taiKhoanService;
    private readonly ILogger<AdminThaiBeerController> _logger;

    public AdminThaiBeerController(ITaiKhoanService taiKhoanService, ILogger<AdminThaiBeerController> logger)
    {
        _taiKhoanService = taiKhoanService;
        _logger = logger;
    }

    [HttpGet("")]
    [OutputCache(NoStore = true)] // trang có antiforgery token, không được cache dùng chung
    public IActionResult DangNhap([FromQuery] string? returnUrl)
    {
        // Nếu đã đăng nhập thì chuyển thẳng vào trang Quản trị
        if (User.Identity?.IsAuthenticated == true)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return Redirect("/quan-tri");
        }

        var vm = new DangNhapAdminViewModel
        {
            ReturnUrl = returnUrl
        };

        return View(vm);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangNhap(DangNhapAdminViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        // Xác thực bằng bảng NguoiDungs (mật khẩu đã băm), không còn mật khẩu viết cứng trong code
        var ketQua = await _taiKhoanService.DangNhapAsync(vm.TenDangNhap, vm.MatKhau);

        switch (ketQua.TrangThai)
        {
            case TrangThaiDangNhap.BiKhoaTam:
                var conLai = Math.Max(1, (int)Math.Ceiling((ketQua.KhoaDenLuc!.Value - DateTime.UtcNow).TotalMinutes));
                _logger.LogWarning("Tài khoản {TenDangNhap} bị khóa tạm do nhập sai mật khẩu nhiều lần. IP: {IP}", vm.TenDangNhap, HttpContext.Connection.RemoteIpAddress);
                ModelState.AddModelError(string.Empty, $"Tài khoản tạm khóa do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau {conLai} phút.");
                return View(vm);

            case TrangThaiDangNhap.BiVoHieuHoa:
                ModelState.AddModelError(string.Empty, "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");
                return View(vm);

            case TrangThaiDangNhap.SaiThongTin:
                _logger.LogWarning("Đăng nhập thất bại với tên {TenDangNhap}. IP: {IP}", vm.TenDangNhap, HttpContext.Connection.RemoteIpAddress);
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View(vm);
        }

        var principal = _taiKhoanService.TaoPrincipal(ketQua.NguoiDung!);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = vm.GhiNhoDangNhap,
            ExpiresUtc = vm.GhiNhoDangNhap ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
        {
            return Redirect(vm.ReturnUrl);
        }

        return Redirect("/quan-tri");
    }

    /// <summary>
    /// Đã đăng nhập nhưng không đủ vai trò (vd. Biên tập viên mở trang Quản lý tài khoản).
    /// Không trỏ về trang đăng nhập vì trang đó sẽ đẩy ngược lại returnUrl => vòng lặp chuyển hướng.
    /// </summary>
    [HttpGet("khong-co-quyen")]
    public IActionResult KhongCoQuyen()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Redirect("/adminthaibeer");

        TempData["ThongBaoLoi"] = "Tài khoản của bạn không có quyền truy cập chức năng này.";
        return Redirect("/quan-tri");
    }

    [HttpGet("dang-xuat")]
    [HttpPost("dang-xuat")]
    public async Task<IActionResult> DangXuat()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/adminthaibeer");
    }
}
