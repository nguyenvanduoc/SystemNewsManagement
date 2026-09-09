using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThaiBeer.Infrastructure.Data;

namespace ThaiBeer.Areas.QuanTri.Controllers;

[Area("QuanTri")]
[Route("quan-tri/lien-he")]
public class LienHeController : Controller
{
    private readonly ThaiBeerDbContext _context;

    public LienHeController(ThaiBeerDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(bool? daXuLy)
    {
        var query = _context.LienHes.AsQueryable();

        if (daXuLy.HasValue)
        {
            query = query.Where(l => l.DaXuLy == daXuLy.Value);
        }

        var danhSach = await query.OrderByDescending(l => l.Id).ToListAsync();
        ViewBag.DaXuLy = daXuLy;
        return View(danhSach);
    }

    [HttpPost("chuyen-trang-thai/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChuyenTrangThai(int id)
    {
        var lh = await _context.LienHes.FindAsync(id);
        if (lh != null)
        {
            lh.DaXuLy = !lh.DaXuLy;
            lh.NgayCapNhat = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["ThongBaoThanhCong"] = "Cập nhật trạng thái liên hệ thành công!";
        }
        return RedirectToAction(nameof(Index));
    }
}
