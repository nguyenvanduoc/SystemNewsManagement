using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThaiBeer.Areas.QuanTri.Filters;
using ThaiBeer.Areas.QuanTri.ViewModels;
using ThaiBeer.Core.Domain.Common;
using ThaiBeer.Infrastructure.Data;

namespace ThaiBeer.Areas.QuanTri.Controllers;

/// <summary>
/// Màn hình Thống kê truy cập. Chỉ Quản trị viên được xem (có địa chỉ IP của khách - dữ liệu cá nhân).
/// Mọi thời gian lưu UTC, hiển thị và gom nhóm theo giờ Việt Nam (UTC+7).
/// </summary>
[Authorize(Roles = VaiTroNguoiDung.QuanTriVien)]
[Area("QuanTri")]
[Route("quan-tri/thong-ke")]
[DinhDangVietNam]
public class ThongKeController : Controller
{
    private const int SoPhienMoiTrang = 20;
    private const int SoNgayToiDa = 366;
    private static readonly TimeSpan VN = DinhDangThongKe.LechMuiGioVN;

    private readonly ThaiBeerDbContext _context;

    public ThongKeController(ThaiBeerDbContext context)
    {
        _context = context;
    }

    /// <summary>GET /quan-tri/thong-ke?khoang=hom-nay|7-ngay|30-ngay|90-ngay|tuy-chon&amp;tuNgay=&amp;denNgay=&amp;trang=</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(string? khoang, DateTime? tuNgay, DateTime? denNgay, int trang = 1)
    {
        var homNayVN = (DateTime.UtcNow + VN).Date;
        var vm = new ThongKeViewModel { Khoang = khoang ?? "7-ngay" };

        (vm.TuNgay, vm.DenNgay) = vm.Khoang switch
        {
            "hom-nay" => (homNayVN, homNayVN),
            "30-ngay" => (homNayVN.AddDays(-29), homNayVN),
            "90-ngay" => (homNayVN.AddDays(-89), homNayVN),
            "tuy-chon" when tuNgay.HasValue && denNgay.HasValue => (tuNgay.Value.Date, denNgay.Value.Date),
            _ => (homNayVN.AddDays(-6), homNayVN)
        };
        if (vm.Khoang is not ("hom-nay" or "30-ngay" or "90-ngay" or "tuy-chon") ||
            (vm.Khoang == "tuy-chon" && !(tuNgay.HasValue && denNgay.HasValue)))
            vm.Khoang = "7-ngay";
        if (vm.DenNgay < vm.TuNgay) (vm.TuNgay, vm.DenNgay) = (vm.DenNgay, vm.TuNgay);
        if ((vm.DenNgay - vm.TuNgay).TotalDays >= SoNgayToiDa) vm.TuNgay = vm.DenNgay.AddDays(-(SoNgayToiDa - 1));

        var soNgay = (int)(vm.DenNgay - vm.TuNgay).TotalDays + 1;
        vm.TheoGio = soNgay == 1;

        // Khoảng thời gian UTC [tu, den)
        var tu = vm.TuNgay - VN;
        var den = vm.DenNgay.AddDays(1) - VN;
        var tuTruoc = tu.AddDays(-soNgay);

        var phienQ = _context.PhienTruyCaps.AsNoTracking().Where(p => p.BatDau >= tu && p.BatDau < den);
        var luotQ = _context.LuotXemTrangs.AsNoTracking().Where(l => l.ThoiDiem >= tu && l.ThoiDiem < den);

        vm.HienTai = await TinhChiSoAsync(tu, den);
        vm.KyTruoc = await TinhChiSoAsync(tuTruoc, tu);
        var nguongTrucTuyen = DateTime.UtcNow.AddMinutes(-5);
        vm.DangTrucTuyen = await _context.PhienTruyCaps.CountAsync(p => p.HoatDongCuoi >= nguongTrucTuyen);

        // ---- Biểu đồ tăng trưởng ----
        if (vm.TheoGio)
        {
            var luotTheoGio = await luotQ.GroupBy(l => l.ThoiDiem.AddHours(7).Hour)
                .Select(g => new { g.Key, SoLuong = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.SoLuong);
            var phienTheoGio = await phienQ.GroupBy(p => p.BatDau.AddHours(7).Hour)
                .Select(g => new { g.Key, Phien = g.Count(), Khach = g.Select(x => x.MaKhach).Distinct().Count() })
                .ToDictionaryAsync(x => x.Key);
            var gioToiDa = vm.DenNgay == homNayVN ? (DateTime.UtcNow + VN).Hour : 23;
            for (var h = 0; h <= gioToiDa; h++)
            {
                vm.ChuoiThoiGian.Add(new DiemThoiGian
                {
                    Nhan = $"{h:00}:00",
                    LuotXem = luotTheoGio.GetValueOrDefault(h),
                    Phien = phienTheoGio.TryGetValue(h, out var p) ? p.Phien : 0,
                    Khach = phienTheoGio.TryGetValue(h, out var k) ? k.Khach : 0
                });
            }
        }
        else
        {
            var luotTheoNgay = await luotQ.GroupBy(l => l.ThoiDiem.AddHours(7).Date)
                .Select(g => new { g.Key, SoLuong = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.SoLuong);
            var phienTheoNgay = await phienQ.GroupBy(p => p.BatDau.AddHours(7).Date)
                .Select(g => new { g.Key, Phien = g.Count(), Khach = g.Select(x => x.MaKhach).Distinct().Count() })
                .ToDictionaryAsync(x => x.Key);
            for (var d = vm.TuNgay; d <= vm.DenNgay; d = d.AddDays(1))
            {
                vm.ChuoiThoiGian.Add(new DiemThoiGian
                {
                    Nhan = d.ToString("dd/MM"),
                    LuotXem = luotTheoNgay.GetValueOrDefault(d),
                    Phien = phienTheoNgay.TryGetValue(d, out var p) ? p.Phien : 0,
                    Khach = phienTheoNgay.TryGetValue(d, out var k) ? k.Khach : 0
                });
            }
        }

        // ---- Lượt xem theo giờ trong ngày (khung giờ khách hay vào) ----
        var theoGio = await luotQ.GroupBy(l => l.ThoiDiem.AddHours(7).Hour)
            .Select(g => new { g.Key, SoLuong = g.Count() }).ToListAsync();
        foreach (var x in theoGio) vm.LuotXemTheoGio[x.Key] = x.SoLuong;

        // ---- Phân bổ theo phiên ----
        vm.ThietBi = await PhanBoAsync(phienQ.GroupBy(p => p.ThietBi), 10);
        vm.HeDieuHanh = await PhanBoAsync(phienQ.GroupBy(p => p.HeDieuHanh), 8);
        vm.TrinhDuyet = await PhanBoAsync(phienQ.GroupBy(p => p.TrinhDuyet), 8);
        vm.TinhThanh = await PhanBoAsync(phienQ.GroupBy(p => p.TinhThanh), 40);
        vm.NguonTruyCap = await PhanBoAsync(phienQ.GroupBy(p => p.NguonTruyCap), 10);

        vm.TrangXemNhieu = await luotQ.GroupBy(l => l.DuongDan)
            .Select(g => new TrangThongKe
            {
                DuongDan = g.Key,
                TieuDe = g.Max(x => x.TieuDe),
                LuotXem = g.Count(),
                ThoiGianTBGiay = g.Average(x => (double)x.ThoiGianOGiay)
            })
            .OrderByDescending(x => x.LuotXem).Take(10).ToListAsync();

        // ---- Danh sách phiên truy cập (mới nhất trước) ----
        vm.TongTrang = Math.Max(1, (int)Math.Ceiling(vm.HienTai.Phien / (double)SoPhienMoiTrang));
        vm.Trang = Math.Clamp(trang, 1, vm.TongTrang);
        vm.PhienGanDay = await phienQ.OrderByDescending(p => p.BatDau)
            .Skip((vm.Trang - 1) * SoPhienMoiTrang).Take(SoPhienMoiTrang).ToListAsync();

        return View(vm);
    }

    /// <summary>GET /quan-tri/thong-ke/phien/{id} — chi tiết 1 phiên: các trang đã xem và thời gian ở mỗi trang.</summary>
    [HttpGet("phien/{id:long}")]
    public async Task<IActionResult> ChiTietPhien(long id)
    {
        var phien = await _context.PhienTruyCaps.AsNoTracking()
            .Include(p => p.LuotXemTrangs.OrderBy(l => l.ThoiDiem))
            .FirstOrDefaultAsync(p => p.Id == id);
        if (phien == null)
            return NotFound();

        ViewBag.SoPhienCuaKhach = await _context.PhienTruyCaps.CountAsync(p => p.MaKhach == phien.MaKhach);
        return View(phien);
    }

    private async Task<ChiSo> TinhChiSoAsync(DateTime tu, DateTime den)
    {
        var phienQ = _context.PhienTruyCaps.AsNoTracking().Where(p => p.BatDau >= tu && p.BatDau < den);
        var tongHop = await phienQ
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Phien = g.Count(),
                Khach = g.Select(x => x.MaKhach).Distinct().Count(),
                KhachMoi = g.Count(x => x.KhachMoi),
                Thoat = g.Count(x => x.SoTrangXem <= 1),
                TongGiay = g.Sum(x => (long)x.ThoiGianOGiay),
                TongTrang = g.Sum(x => x.SoTrangXem)
            })
            .FirstOrDefaultAsync();

        var luotXem = await _context.LuotXemTrangs.CountAsync(l => l.ThoiDiem >= tu && l.ThoiDiem < den);
        if (tongHop == null || tongHop.Phien == 0)
            return new ChiSo { LuotXem = luotXem };

        return new ChiSo
        {
            LuotXem = luotXem,
            Phien = tongHop.Phien,
            KhachDuyNhat = tongHop.Khach,
            KhachMoi = tongHop.KhachMoi,
            ThoiGianTBGiay = tongHop.TongGiay / (double)tongHop.Phien,
            TyLeThoat = tongHop.Thoat * 100.0 / tongHop.Phien,
            TrangMoiPhien = tongHop.TongTrang / (double)tongHop.Phien
        };
    }

    private static async Task<List<MucPhanBo>> PhanBoAsync(IQueryable<IGrouping<string, Core.Domain.Entities.PhienTruyCap>> nhom, int soLuong)
    {
        return await nhom
            .Select(g => new MucPhanBo
            {
                Ten = g.Key,
                SoLuong = g.Count(),
                ThoiGianTBGiay = g.Average(x => (double)x.ThoiGianOGiay)
            })
            .OrderByDescending(x => x.SoLuong)
            .Take(soLuong)
            .ToListAsync();
    }
}
