using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThaiBeer.Areas.QuanTri.ViewModels;
using ThaiBeer.Core.Common;
using ThaiBeer.Core.Domain.Entities;
using ThaiBeer.Core.Interfaces;
using ThaiBeer.Infrastructure.Data;

namespace ThaiBeer.Areas.QuanTri.Controllers;

[Area("QuanTri")]
[Route("quan-tri/san-pham")]
public class SanPhamController : Controller
{
    private readonly ThaiBeerDbContext _context;
    private readonly IImageOptimizerService _imageOptimizer;
    private readonly ICacheInvalidatorService _cacheInvalidator;

    public SanPhamController(
        ThaiBeerDbContext context,
        IImageOptimizerService imageOptimizer,
        ICacheInvalidatorService cacheInvalidator)
    {
        _context = context;
        _imageOptimizer = imageOptimizer;
        _cacheInvalidator = cacheInvalidator;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? danhMucId, string? tuKhoa)
    {
        var query = _context.SanPhams
            .Include(s => s.DanhMuc)
            .AsNoTracking();

        if (danhMucId.HasValue && danhMucId.Value > 0)
        {
            query = query.Where(s => s.DanhMucId == danhMucId.Value);
        }

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            query = query.Where(s => s.TenSanPham.Contains(tuKhoa));
        }

        var danhSach = await query.OrderByDescending(s => s.Id).ToListAsync();

        ViewBag.DanhMucs = await _context.DanhMucs
            .OrderBy(d => d.ThuTuHienThi)
            .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.TenDanhMuc, Selected = (d.Id == danhMucId) })
            .ToListAsync();

        ViewBag.DanhMucHienTai = danhMucId;
        ViewBag.TuKhoa = tuKhoa;

        return View(danhSach);
    }

    [HttpGet("them-moi")]
    public async Task<IActionResult> ThemMoi()
    {
        var vm = new SanPhamUpsertViewModel
        {
            DanhSachDanhMuc = await LayDanhSachChonDanhMucAsync()
        };
        return View(vm);
    }

    [HttpPost("them-moi")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemMoi(SanPhamUpsertViewModel vm)
    {
        if (vm.TapTinHinhAnh == null || vm.TapTinHinhAnh.Length == 0)
        {
            ModelState.AddModelError(nameof(vm.TapTinHinhAnh), "Vui lòng tải lên ảnh đại diện cho sản phẩm.");
        }

        if (!ModelState.IsValid)
        {
            vm.DanhSachDanhMuc = await LayDanhSachChonDanhMucAsync();
            return View(vm);
        }

        // Tự động tạo slug nếu để trống
        var slug = string.IsNullOrWhiteSpace(vm.DuongDanSlug)
            ? SlugHelper.TaoSlug(vm.TenSanPham)
            : SlugHelper.TaoSlug(vm.DuongDanSlug);

        // Đảm bảo slug là duy nhất
        var slugDaTonTai = await _context.SanPhams.AnyAsync(s => s.DuongDanSlug == slug);
        if (slugDaTonTai)
        {
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
        }

        // Nén và lưu ảnh WebP
        string hinhAnhWebPUrl = string.Empty;
        if (vm.TapTinHinhAnh != null)
        {
            hinhAnhWebPUrl = await _imageOptimizer.ToiUuVaLuuWebPAsync(vm.TapTinHinhAnh, "san-pham", chieuRongToiDa: 1200, chatLuong: 82);
        }

        var sanPham = new SanPham
        {
            TenSanPham = vm.TenSanPham.Trim(),
            DuongDanSlug = slug,
            MoTaNgan = vm.MoTaNgan,
            NoiDungChiTiet = vm.NoiDungChiTiet,
            HinhAnhWebP = hinhAnhWebPUrl,
            VideoGioiThieuUrl = vm.VideoGioiThieuUrl,
            NongDoCon = vm.NongDoCon,
            DungTichMl = vm.DungTichMl,
            XuatXu = string.IsNullOrWhiteSpace(vm.XuatXu) ? "Thái Lan" : vm.XuatXu.Trim(),
            QuyCachDongGoi = vm.QuyCachDongGoi?.Trim() ?? string.Empty,
            HienThiTrangChu = vm.HienThiTrangChu,
            TrangThaiHoatDong = vm.TrangThaiHoatDong,
            ThuTuHienThi = vm.ThuTuHienThi,
            DanhMucId = vm.DanhMucId,
            NgayTao = DateTime.UtcNow
        };

        _context.SanPhams.Add(sanPham);
        await _context.SaveChangesAsync();

        // Xóa OutputCache để người dùng xem được ngay
        await _cacheInvalidator.LamMoiSanPhamAsync();

        TempData["ThongBaoThanhCong"] = "Thêm mới sản phẩm thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("chinh-sua/{id:int}")]
    public async Task<IActionResult> ChinhSua(int id)
    {
        var sp = await _context.SanPhams.FindAsync(id);
        if (sp == null)
        {
            return NotFound();
        }

        var vm = new SanPhamUpsertViewModel
        {
            Id = sp.Id,
            TenSanPham = sp.TenSanPham,
            DuongDanSlug = sp.DuongDanSlug,
            MoTaNgan = sp.MoTaNgan,
            NoiDungChiTiet = sp.NoiDungChiTiet,
            HinhAnhHienTai = sp.HinhAnhWebP,
            VideoGioiThieuUrl = sp.VideoGioiThieuUrl,
            NongDoCon = sp.NongDoCon,
            DungTichMl = sp.DungTichMl,
            XuatXu = sp.XuatXu,
            QuyCachDongGoi = sp.QuyCachDongGoi,
            HienThiTrangChu = sp.HienThiTrangChu,
            TrangThaiHoatDong = sp.TrangThaiHoatDong,
            ThuTuHienThi = sp.ThuTuHienThi,
            DanhMucId = sp.DanhMucId,
            DanhSachDanhMuc = await LayDanhSachChonDanhMucAsync()
        };

        return View(vm);
    }

    [HttpPost("chinh-sua/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(int id, SanPhamUpsertViewModel vm)
    {
        if (id != vm.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            vm.DanhSachDanhMuc = await LayDanhSachChonDanhMucAsync();
            return View(vm);
        }

        var sp = await _context.SanPhams.FindAsync(id);
        if (sp == null)
            return NotFound();

        // Cập nhật Slug
        var slugMoi = string.IsNullOrWhiteSpace(vm.DuongDanSlug)
            ? SlugHelper.TaoSlug(vm.TenSanPham)
            : SlugHelper.TaoSlug(vm.DuongDanSlug);

        if (slugMoi != sp.DuongDanSlug)
        {
            var tonTai = await _context.SanPhams.AnyAsync(s => s.DuongDanSlug == slugMoi && s.Id != id);
            if (tonTai)
            {
                slugMoi = $"{slugMoi}-{DateTime.UtcNow.Ticks % 10000}";
            }
            sp.DuongDanSlug = slugMoi;
        }

        // Tải ảnh mới nếu có
        if (vm.TapTinHinhAnh != null && vm.TapTinHinhAnh.Length > 0)
        {
            var oldImage = sp.HinhAnhWebP;
            sp.HinhAnhWebP = await _imageOptimizer.ToiUuVaLuuWebPAsync(vm.TapTinHinhAnh, "san-pham", chieuRongToiDa: 1200, chatLuong: 82);
            _imageOptimizer.XoaHinhAnh(oldImage);
        }

        sp.TenSanPham = vm.TenSanPham.Trim();
        sp.MoTaNgan = vm.MoTaNgan;
        sp.NoiDungChiTiet = vm.NoiDungChiTiet;
        sp.VideoGioiThieuUrl = vm.VideoGioiThieuUrl;
        sp.NongDoCon = vm.NongDoCon;
        sp.DungTichMl = vm.DungTichMl;
        sp.XuatXu = vm.XuatXu?.Trim() ?? "Thái Lan";
        sp.QuyCachDongGoi = vm.QuyCachDongGoi?.Trim() ?? string.Empty;
        sp.HienThiTrangChu = vm.HienThiTrangChu;
        sp.TrangThaiHoatDong = vm.TrangThaiHoatDong;
        sp.ThuTuHienThi = vm.ThuTuHienThi;
        sp.DanhMucId = vm.DanhMucId;
        sp.NgayCapNhat = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _cacheInvalidator.LamMoiSanPhamAsync();

        TempData["ThongBaoThanhCong"] = "Cập nhật sản phẩm thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("xoa/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var sp = await _context.SanPhams.FindAsync(id);
        if (sp != null)
        {
            _imageOptimizer.XoaHinhAnh(sp.HinhAnhWebP);
            _context.SanPhams.Remove(sp);
            await _context.SaveChangesAsync();
            await _cacheInvalidator.LamMoiSanPhamAsync();
            TempData["ThongBaoThanhCong"] = "Đã xóa sản phẩm thành công!";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> LayDanhSachChonDanhMucAsync()
    {
        return await _context.DanhMucs
            .Where(d => d.TrangThaiHoatDong)
            .OrderBy(d => d.ThuTuHienThi)
            .Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.TenDanhMuc
            })
            .ToListAsync();
    }
}
