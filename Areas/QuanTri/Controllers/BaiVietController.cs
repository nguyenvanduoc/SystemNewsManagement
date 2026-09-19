using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThaiBeer.Areas.QuanTri.ViewModels;
using ThaiBeer.Core.Common;
using ThaiBeer.Core.Domain.Entities;
using ThaiBeer.Core.Interfaces;
using ThaiBeer.Infrastructure.Data;

namespace ThaiBeer.Areas.QuanTri.Controllers;

[Authorize]
[Area("QuanTri")]
[Route("quan-tri/bai-viet")]
public class BaiVietController : Controller
{
    private readonly ThaiBeerDbContext _context;
    private readonly IImageOptimizerService _imageOptimizer;
    private readonly ICacheInvalidatorService _cacheInvalidator;

    public BaiVietController(
        ThaiBeerDbContext context,
        IImageOptimizerService imageOptimizer,
        ICacheInvalidatorService cacheInvalidator)
    {
        _context = context;
        _imageOptimizer = imageOptimizer;
        _cacheInvalidator = cacheInvalidator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? tuKhoa, bool? noiBat, bool? trangThai)
    {
        var query = _context.BaiViets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var tuKhoaTrim = tuKhoa.Trim();
            query = query.Where(b => b.TieuDe.Contains(tuKhoaTrim) || (b.TomTat != null && b.TomTat.Contains(tuKhoaTrim)));
        }

        if (noiBat.HasValue)
        {
            query = query.Where(b => b.NoiBat == noiBat.Value);
        }

        if (trangThai.HasValue)
        {
            query = query.Where(b => b.TrangThaiHoatDong == trangThai.Value);
        }

        var danhSach = await query.OrderByDescending(b => b.NgayTao).ToListAsync();

        ViewBag.TuKhoa = tuKhoa;
        ViewBag.NoiBat = noiBat;
        ViewBag.TrangThai = trangThai;

        return View(danhSach);
    }

    [HttpGet("them-moi")]
    public IActionResult ThemMoi()
    {
        var vm = new BaiVietUpsertViewModel();
        return View(vm);
    }

    [HttpPost("them-moi")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemMoi(BaiVietUpsertViewModel vm)
    {
        if ((vm.TapTinHinhAnh == null || vm.TapTinHinhAnh.Length == 0) && string.IsNullOrWhiteSpace(vm.HinhAnhHienTai))
        {
            ModelState.AddModelError(nameof(vm.TapTinHinhAnh), "Vui lòng chọn hoặc tải lên ảnh đại diện cho bài viết.");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        // Tự động tạo slug nếu để trống
        var slug = string.IsNullOrWhiteSpace(vm.DuongDanSlug)
            ? SlugHelper.TaoSlug(vm.TieuDe)
            : SlugHelper.TaoSlug(vm.DuongDanSlug);

        // Đảm bảo slug là duy nhất
        var slugDaTonTai = await _context.BaiViets.AnyAsync(b => b.DuongDanSlug == slug);
        if (slugDaTonTai)
        {
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
        }

        // Nén và lưu ảnh WebP
        string hinhAnhWebPUrl = string.Empty;
        if (vm.TapTinHinhAnh != null && vm.TapTinHinhAnh.Length > 0)
        {
            hinhAnhWebPUrl = await _imageOptimizer.ToiUuVaLuuWebPAsync(vm.TapTinHinhAnh, "bai-viet", 1200, 82);
        }
        else if (!string.IsNullOrWhiteSpace(vm.HinhAnhHienTai))
        {
            hinhAnhWebPUrl = vm.HinhAnhHienTai.Trim();
        }

        var baiViet = new BaiViet
        {
            TieuDe = vm.TieuDe.Trim(),
            DuongDanSlug = slug,
            TomTat = vm.TomTat?.Trim(),
            NoiDungHtml = vm.NoiDungHtml,
            HinhAnhWebP = hinhAnhWebPUrl,
            TacGia = string.IsNullOrWhiteSpace(vm.TacGia) ? "ThaiBeer Ban Biên Tập" : vm.TacGia.Trim(),
            NoiBat = vm.NoiBat,
            TrangThaiHoatDong = vm.TrangThaiHoatDong,
            LuotXem = 0,
            NgayTao = DateTime.UtcNow
        };

        _context.BaiViets.Add(baiViet);
        await _context.SaveChangesAsync();

        // Xóa cache tin tức và trang chủ
        await _cacheInvalidator.LamMoiTinTucAsync();
        await _cacheInvalidator.LamMoiTrangChuAsync();

        TempData["ThongBaoThanhCong"] = $"Đã thêm mới bài viết '{baiViet.TieuDe}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("chinh-sua/{id:int}")]
    public async Task<IActionResult> ChinhSua(int id)
    {
        var baiViet = await _context.BaiViets.FindAsync(id);
        if (baiViet == null)
        {
            TempData["ThongBaoLoi"] = "Không tìm thấy bài viết yêu cầu.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new BaiVietUpsertViewModel
        {
            Id = baiViet.Id,
            TieuDe = baiViet.TieuDe,
            DuongDanSlug = baiViet.DuongDanSlug,
            TomTat = baiViet.TomTat,
            NoiDungHtml = baiViet.NoiDungHtml,
            HinhAnhHienTai = baiViet.HinhAnhWebP,
            TacGia = baiViet.TacGia,
            NoiBat = baiViet.NoiBat,
            TrangThaiHoatDong = baiViet.TrangThaiHoatDong
        };

        return View(vm);
    }

    [HttpPost("chinh-sua/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, BaiVietUpsertViewModel vm)
    {
        if (id != vm.Id)
        {
            return BadRequest();
        }

        var baiViet = await _context.BaiViets.FindAsync(id);
        if (baiViet == null)
        {
            TempData["ThongBaoLoi"] = "Bài viết không tồn tại hoặc đã bị xóa.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            vm.HinhAnhHienTai = baiViet.HinhAnhWebP;
            return View(vm);
        }

        // Tự động chuẩn hóa slug
        var slug = string.IsNullOrWhiteSpace(vm.DuongDanSlug)
            ? SlugHelper.TaoSlug(vm.TieuDe)
            : SlugHelper.TaoSlug(vm.DuongDanSlug);

        // Kiểm tra slug trùng lặp ngoại trừ chính bài viết hiện tại
        var slugDaTonTai = await _context.BaiViets.AnyAsync(b => b.DuongDanSlug == slug && b.Id != id);
        if (slugDaTonTai)
        {
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
        }

        // Cập nhật ảnh nếu có file mới
        if (vm.TapTinHinhAnh != null && vm.TapTinHinhAnh.Length > 0)
        {
            var oldImage = baiViet.HinhAnhWebP;
            baiViet.HinhAnhWebP = await _imageOptimizer.ToiUuVaLuuWebPAsync(vm.TapTinHinhAnh, "bai-viet", 1200, 82);

            // Xóa ảnh cũ nếu là file trong thư mục uploads
            if (!string.IsNullOrWhiteSpace(oldImage) && oldImage.StartsWith("/uploads/"))
            {
                _imageOptimizer.XoaHinhAnh(oldImage);
            }
        }
        else if (!string.IsNullOrWhiteSpace(vm.HinhAnhHienTai))
        {
            baiViet.HinhAnhWebP = vm.HinhAnhHienTai.Trim();
        }

        baiViet.TieuDe = vm.TieuDe.Trim();
        baiViet.DuongDanSlug = slug;
        baiViet.TomTat = vm.TomTat?.Trim();
        baiViet.NoiDungHtml = vm.NoiDungHtml;
        baiViet.TacGia = string.IsNullOrWhiteSpace(vm.TacGia) ? "ThaiBeer Ban Biên Tập" : vm.TacGia.Trim();
        baiViet.NoiBat = vm.NoiBat;
        baiViet.TrangThaiHoatDong = vm.TrangThaiHoatDong;
        baiViet.NgayCapNhat = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Xóa cache
        await _cacheInvalidator.LamMoiTinTucAsync();
        await _cacheInvalidator.LamMoiTrangChuAsync();

        TempData["ThongBaoThanhCong"] = $"Đã cập nhật bài viết '{baiViet.TieuDe}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("xoa/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var baiViet = await _context.BaiViets.FindAsync(id);
        if (baiViet == null)
        {
            TempData["ThongBaoLoi"] = "Bài viết không tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        // Xóa file ảnh đại diện nếu có trong uploads
        if (!string.IsNullOrWhiteSpace(baiViet.HinhAnhWebP) && baiViet.HinhAnhWebP.StartsWith("/uploads/"))
        {
            _imageOptimizer.XoaHinhAnh(baiViet.HinhAnhWebP);
        }

        _context.BaiViets.Remove(baiViet);
        await _context.SaveChangesAsync();

        // Xóa cache
        await _cacheInvalidator.LamMoiTinTucAsync();
        await _cacheInvalidator.LamMoiTrangChuAsync();

        TempData["ThongBaoThanhCong"] = $"Đã xóa bài viết '{baiViet.TieuDe}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("chuyen-noi-bat/{id:int}")]
    public async Task<IActionResult> ChuyenNoiBat(int id)
    {
        var baiViet = await _context.BaiViets.FindAsync(id);
        if (baiViet == null) return Json(new { success = false, message = "Không tìm thấy bài viết" });

        baiViet.NoiBat = !baiViet.NoiBat;
        baiViet.NgayCapNhat = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _cacheInvalidator.LamMoiTinTucAsync();
        await _cacheInvalidator.LamMoiTrangChuAsync();

        return Json(new { success = true, noiBat = baiViet.NoiBat });
    }

    [HttpPost("chuyen-trang-thai/{id:int}")]
    public async Task<IActionResult> ChuyenTrangThai(int id)
    {
        var baiViet = await _context.BaiViets.FindAsync(id);
        if (baiViet == null) return Json(new { success = false, message = "Không tìm thấy bài viết" });

        baiViet.TrangThaiHoatDong = !baiViet.TrangThaiHoatDong;
        baiViet.NgayCapNhat = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _cacheInvalidator.LamMoiTinTucAsync();
        await _cacheInvalidator.LamMoiTrangChuAsync();

        return Json(new { success = true, trangThai = baiViet.TrangThaiHoatDong });
    }
}
