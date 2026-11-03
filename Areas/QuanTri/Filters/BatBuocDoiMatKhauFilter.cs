using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ThaiBeer.Infrastructure.Services;

namespace ThaiBeer.Areas.QuanTri.Filters;

/// <summary>
/// Nếu tài khoản đang đăng nhập có cờ PhaiDoiMatKhau = true (tài khoản mới tạo / vừa được đặt lại mật khẩu),
/// mọi trang trong khu vực Quản trị đều chuyển hướng về trang Đổi mật khẩu cho tới khi đổi xong.
/// </summary>
public class BatBuocDoiMatKhauFilter : IActionFilter
{
    public const string DuongDanDoiMatKhau = "/quan-tri/tai-khoan/doi-mat-khau";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!string.Equals(context.RouteData.Values["area"] as string, "QuanTri", StringComparison.OrdinalIgnoreCase))
            return;

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
            return;

        if (user.FindFirstValue(TaiKhoanService.ClaimPhaiDoiMatKhau) != "true")
            return;

        var controller = context.RouteData.Values["controller"] as string;
        var action = context.RouteData.Values["action"] as string;
        if (controller == "TaiKhoan" && action == "DoiMatKhau")
            return;

        context.Result = new RedirectResult(DuongDanDoiMatKhau);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
