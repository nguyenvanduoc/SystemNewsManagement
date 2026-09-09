using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThaiBeer.Core.Common;
using ThaiBeer.Core.Domain.Entities;
using ThaiBeer.Core.Interfaces;
using ThaiBeer.Infrastructure.Data;

namespace ThaiBeer.Areas.QuanTri.Controllers;

[Area("QuanTri")]
[Route("quan-tri/danh-muc")]
public class DanhMucController : Controller
{
    private readonly ThaiBeerDbContext _context;
    private readonly ICacheInvalidatorService _cacheInvalidator;

    public DanhMucController(ThaiBeerDbContext context, ICacheInvalidatorService cacheInvalidator)
    {
        _context = context;
        _cacheInvalidator = cacheInvalidator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var danhSach = await _context.DanhMucs
            .Include(d => d.SanPhams)
            .OrderBy(d => d.ThuTuHienThi)
            .ToListAsync();

        return View(danhSach);
    }

    [HttpGet("them-moi")]
    public IActionResult ThemMoi()
    {
        return View(new DanhMuc());
    }

    [HttpPost("them-moi")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemMoi(DanhMuc danhMuc)
    {
        if (string.IsNullOrWhiteSpace(danhMuc.DuongDanSlug))
        {
            danhMuc.DuongDanSlug = SlugHelper.TaoSlug(danhMuc.TenDanhMuc);
        }

        var tonTai = await _context.DanhMucs.AnyAsync(d => d.DuongDanSlug == danhMuc.DuongDanSlug);
        if (tonTai)
        {
            ModelState.AddModelError(nameof(danhMuc.DuongDanSlug), "Đường dẫn slug này đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(danhMuc);
        }

        danhMuc.NgayTao = DateTime.UtcNow;
        _context.DanhMucs.Add(danhMuc);
        await _context.SaveChangesAsync();

        await _cacheInvalidator.LamMoiSanPhamAsync();

        TempData["ThongBaoThanhCong"] = "Thêm danh mục thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("chinh-sua/{id:int}")]
    public async Task<IActionResult> ChinhSua(int id)
    {
        var dm = await _context.DanhMucs.FindAsync(id);
        if (dm == null) return NotFound();
        return View(dm);
    }

    [HttpPost("chinh-sua/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, DanhMuc danhMuc)
    {
        if (id != danhMuc.Id) return BadRequest();

        if (string.IsNullOrWhiteSpace(danhMuc.DuongDanSlug))
        {
            danhMuc.DuongDanSlug = SlugHelper.TaoSlug(danhMuc.TenDanhMuc);
        }

        var tonTai = await _context.DanhMucs.AnyAsync(d => d.DuongDanSlug == danhMuc.DuongDanSlug && d.Id != id);
        if (tonTai)
        {
            ModelState.AddModelError(nameof(danhMuc.DuongDanSlug), "Đường dẫn slug này đã tồn tại.");
        }

        if (!ModelState.IsValid)
        {
            return View(danhMuc);
        }

        var existing = await _context.DanhMucs.FindAsync(id);
        if (existing == null) return NotFound();

        existing.TenDanhMuc = danhMuc.TenDanhMuc.Trim();
        existing.DuongDanSlug = danhMuc.DuongDanSlug;
        existing.MoTa = danhMuc.MoTa;
        existing.ThuTuHienThi = danhMuc.ThuTuHienThi;
        existing.TrangThaiHoatDong = danhMuc.TrangThaiHoatDong;
        existing.NgayCapNhat = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _cacheInvalidator.LamMoiSanPhamAsync();

        TempData["ThongBaoThanhCong"] = "Cập nhật danh mục thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("xoa/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var coSanPham = await _context.SanPhams.AnyAsync(s => s.DanhMucId == id);
        if (coSanPham)
        {
            TempData["ThongBaoLoi"] = "Không thể xóa danh mục đang chứa sản phẩm. Vui lòng chuyển hoặc xóa sản phẩm trước!";
            return RedirectToAction(nameof(Index));
        }

        var dm = await _context.DanhMucs.FindAsync(id);
        if (dm != null)
        {
            _context.DanhMucs.Remove(dm);
            await _context.SaveChangesAsync();
            await _cacheInvalidator.LamMoiSanPhamAsync();
            TempData["ThongBaoThanhCong"] = "Xóa danh mục thành công!";
        }

        return RedirectToAction(nameof(Index));
    }
}
