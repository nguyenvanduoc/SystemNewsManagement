using ThaiBeer.Core.Domain.Entities;

namespace ThaiBeer.Areas.QuanTri.ViewModels;

public class ThongKeViewModel
{
    // ---- Bộ lọc ----
    public string Khoang { get; set; } = "7-ngay";
    public DateTime TuNgay { get; set; }   // ngày giờ Việt Nam (UTC+7), bao gồm
    public DateTime DenNgay { get; set; }  // ngày Việt Nam, bao gồm
    public bool TheoGio { get; set; }      // khoảng 1 ngày => biểu đồ theo giờ

    // ---- Chỉ số chính (kỳ hiện tại / kỳ trước cùng độ dài) ----
    public ChiSo HienTai { get; set; } = new();
    public ChiSo KyTruoc { get; set; } = new();
    public int DangTrucTuyen { get; set; }

    // ---- Biểu đồ ----
    public List<DiemThoiGian> ChuoiThoiGian { get; set; } = new();
    public int[] LuotXemTheoGio { get; set; } = new int[24];

    // ---- Phân bổ ----
    public List<MucPhanBo> ThietBi { get; set; } = new();
    public List<MucPhanBo> HeDieuHanh { get; set; } = new();
    public List<MucPhanBo> TrinhDuyet { get; set; } = new();
    public List<MucPhanBo> TinhThanh { get; set; } = new();
    public List<MucPhanBo> NguonTruyCap { get; set; } = new();
    public List<TrangThongKe> TrangXemNhieu { get; set; } = new();

    // ---- Danh sách phiên ----
    public List<PhienTruyCap> PhienGanDay { get; set; } = new();
    public int Trang { get; set; } = 1;
    public int TongTrang { get; set; } = 1;
}

public class ChiSo
{
    public int LuotXem { get; set; }
    public int Phien { get; set; }
    public int KhachDuyNhat { get; set; }
    public int KhachMoi { get; set; }
    public double ThoiGianTBGiay { get; set; }
    public double TyLeThoat { get; set; }      // 0..100
    public double TrangMoiPhien { get; set; }
}

public class DiemThoiGian
{
    public string Nhan { get; set; } = string.Empty;
    public int LuotXem { get; set; }
    public int Phien { get; set; }
    public int Khach { get; set; }
}

public class MucPhanBo
{
    public string Ten { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public double ThoiGianTBGiay { get; set; }
}

public class TrangThongKe
{
    public string DuongDan { get; set; } = string.Empty;
    public string? TieuDe { get; set; }
    public int LuotXem { get; set; }
    public double ThoiGianTBGiay { get; set; }
}

public static class DinhDangThongKe
{
    public static readonly TimeSpan LechMuiGioVN = TimeSpan.FromHours(7);

    public static DateTime SangGioVN(DateTime utc) => utc + LechMuiGioVN;

    /// <summary>45 -> "45 giây"; 135 -> "2 phút 15 giây"; 3720 -> "1 giờ 2 phút".</summary>
    public static string ThoiLuong(double giayDouble)
    {
        var giay = (int)Math.Round(giayDouble);
        if (giay < 60) return $"{giay} giây";
        if (giay < 3600) return giay % 60 == 0 ? $"{giay / 60} phút" : $"{giay / 60} phút {giay % 60} giây";
        var phut = (giay % 3600) / 60;
        return phut == 0 ? $"{giay / 3600} giờ" : $"{giay / 3600} giờ {phut} phút";
    }

    /// <summary>Phần trăm thay đổi so với kỳ trước; null nếu kỳ trước = 0 (không so sánh được).</summary>
    public static double? PhanTramThayDoi(double hienTai, double truoc)
        => truoc == 0 ? null : (hienTai - truoc) / truoc * 100.0;
}

/// <summary>Model cho partial _PhanBo: 1 khối danh sách thanh ngang (thiết bị, tỉnh thành, nguồn...).</summary>
public class KhoiPhanBo
{
    public string TieuDe { get; set; } = string.Empty;
    public string Icon { get; set; } = "fa-chart-bar";
    public List<MucPhanBo> Muc { get; set; } = new();
    public int Tong { get; set; }
    public bool CuonDoc { get; set; }
}
