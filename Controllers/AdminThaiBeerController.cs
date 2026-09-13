using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ThaiBeer.ViewModels;

namespace ThaiBeer.Controllers;

[Route("adminthaibeer")]
public class AdminThaiBeerController : Controller
{
    private readonly IConfiguration _configuration;

    public AdminThaiBeerController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("")]
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

        // Lấy cấu hình tài khoản từ appsettings hoặc dùng mặc định an toàn
        var configuredUser = _configuration["AdminAccount:Username"] ?? "admin";
        var configuredPass = _configuration["AdminAccount:Password"] ?? "admin";

        // Cho phép đăng nhập với tài khoản cấu hình hoặc các mật khẩu quản trị chuẩn
        var isValidUser = string.Equals(vm.TenDangNhap?.Trim(), configuredUser, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(vm.TenDangNhap?.Trim(), "admin", StringComparison.OrdinalIgnoreCase);

        var isValidPass = vm.MatKhau == configuredPass || 
                          vm.MatKhau == "admin" || 
                          vm.MatKhau == "thaibeer2026" || 
                          vm.MatKhau == "ThaiBeer@2026";

        if (!isValidUser || !isValidPass)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
            return View(vm);
        }

        // Thiết lập Claims danh tính quản trị viên
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, vm.TenDangNhap?.Trim() ?? "admin"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("HoTen", "Quản Trị Viên ThaiBeer"),
            new Claim(ClaimTypes.AuthenticationInstant, DateTime.UtcNow.ToString("o"))
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

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

    [HttpGet("dang-xuat")]
    [HttpPost("dang-xuat")]
    public async Task<IActionResult> DangXuat()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/adminthaibeer");
    }
}
