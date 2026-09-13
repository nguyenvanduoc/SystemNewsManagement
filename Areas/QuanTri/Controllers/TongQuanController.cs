using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThaiBeer.Areas.QuanTri.ViewModels;
using ThaiBeer.Infrastructure.Data;

namespace ThaiBeer.Areas.QuanTri.Controllers;

[Authorize]
[Area("QuanTri")]
[Route("quan-tri")]
public class TongQuanController : Controller
{
    private readonly ThaiBeerDbContext _context;

    public TongQuanController(ThaiBeerDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    [HttpGet("tong-quan")]
    public async Task<IActionResult> Index()
    {
        var vm = new TongQuanViewModel
        {
            TongSanPham = await _context.SanPhams.CountAsync(),
            TongDanhMuc = await _context.DanhMucs.CountAsync(),
            TongBaiViet = await _context.BaiViets.CountAsync(),
            TongLienHeMoiChuaXuLy = await _context.LienHes.CountAsync(l => !l.DaXuLy),
            SanPhamMoi = await _context.SanPhams
                .Include(s => s.DanhMuc)
                .OrderByDescending(s => s.Id)
                .Take(5)
                .ToListAsync(),
            LienHeMoiNhat = await _context.LienHes
                .OrderByDescending(l => l.Id)
                .Take(5)
                .ToListAsync()
        };

        return View(vm);
    }
}
