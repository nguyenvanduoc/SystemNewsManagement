using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ThaiBeer.Infrastructure.Services;

namespace ThaiBeer.Controllers;

/// <summary>
/// API công khai nhận dữ liệu thống kê từ script /js/thong-ke.js trên trang khách.
/// Không ghi nhận khi quản trị viên đang đăng nhập (để số liệu không lẫn lượt xem nội bộ).
/// </summary>
[ApiController]
[Route("api/thong-ke")]
[IgnoreAntiforgeryToken]
[EnableRateLimiting(ChinhSachGioiHan)]
public class ThongKeApiController : ControllerBase
{
    public const string ChinhSachGioiHan = "ThongKeTruyCap";

    private readonly ThongKeTruyCapService _thongKe;

    public ThongKeApiController(ThongKeTruyCapService thongKe)
    {
        _thongKe = thongKe;
    }

    /// <summary>POST /api/thong-ke/luot-xem — ghi 1 lượt xem trang, trả về { id } để gửi thời gian ở lại sau.</summary>
    [HttpPost("luot-xem")]
    public async Task<IActionResult> LuotXem([FromBody] YeuCauGhiNhanLuotXem yc)
    {
        if (User.Identity?.IsAuthenticated == true)
            return NoContent();

        var id = await _thongKe.GhiNhanLuotXemAsync(
            yc,
            HttpContext.Connection.RemoteIpAddress,
            Request.Headers.UserAgent.ToString(),
            Request.Host.Host);

        return id.HasValue ? Ok(new { id = id.Value }) : NoContent();
    }

    /// <summary>POST /api/thong-ke/thoi-gian — cập nhật số giây khách đã xem trang (gửi định kỳ và khi rời trang).</summary>
    [HttpPost("thoi-gian")]
    public async Task<IActionResult> ThoiGian([FromBody] YeuCauCapNhatThoiGian yc)
    {
        if (User.Identity?.IsAuthenticated == true)
            return NoContent();

        await _thongKe.CapNhatThoiGianAsync(yc);
        return NoContent();
    }
}
