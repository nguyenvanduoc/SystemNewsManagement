using System.Globalization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ThaiBeer.Areas.QuanTri.Filters;

/// <summary>
/// Hiển thị số theo kiểu Việt Nam (2.520 · 39,7%) khi render view của controller được gắn attribute này.
/// Chỉ áp dụng lúc render kết quả, không đổi culture của cả website (tránh ảnh hưởng form nhập số thập phân).
/// </summary>
public class DinhDangVietNamAttribute : ResultFilterAttribute
{
    private static readonly CultureInfo _vi = CultureInfo.GetCultureInfo("vi-VN");

    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var cuCulture = CultureInfo.CurrentCulture;
        var cuUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = _vi;
        CultureInfo.CurrentUICulture = _vi;
        try
        {
            await next();
        }
        finally
        {
            CultureInfo.CurrentCulture = cuCulture;
            CultureInfo.CurrentUICulture = cuUiCulture;
        }
    }
}
