using System.Globalization;
using System.Text;

namespace ThaiBeer.Infrastructure.Services;

/// <summary>
/// Chuẩn hoá tên tỉnh/thành Việt Nam từ các nguồn định vị IP (tiếng Anh, không dấu, tên cũ trước sáp nhập)
/// về 34 đơn vị hành chính cấp tỉnh từ 01/07/2025. Đồng thời đặt tên tiếng Việt cho các nước hay gặp.
/// </summary>
public static class TinhThanhVietNam
{
    // Tên mới (34 tỉnh/thành) -> các tên cũ được sáp nhập vào (tên không dấu, viết liền khi so khớp)
    private static readonly (string Moi, string[] Cu)[] _sapNhap =
    {
        ("Hà Nội", new[] { "Ha Noi", "Hanoi" }),
        ("Huế", new[] { "Hue", "Thua Thien Hue", "Thua Thien" }),
        ("Lai Châu", new[] { "Lai Chau" }),
        ("Điện Biên", new[] { "Dien Bien" }),
        ("Sơn La", new[] { "Son La" }),
        ("Lạng Sơn", new[] { "Lang Son" }),
        ("Quảng Ninh", new[] { "Quang Ninh" }),
        ("Thanh Hóa", new[] { "Thanh Hoa" }),
        ("Nghệ An", new[] { "Nghe An" }),
        ("Hà Tĩnh", new[] { "Ha Tinh" }),
        ("Cao Bằng", new[] { "Cao Bang" }),
        ("Tuyên Quang", new[] { "Tuyen Quang", "Ha Giang" }),
        ("Lào Cai", new[] { "Lao Cai", "Yen Bai" }),
        ("Thái Nguyên", new[] { "Thai Nguyen", "Bac Kan", "Bac Can" }),
        ("Phú Thọ", new[] { "Phu Tho", "Vinh Phuc", "Hoa Binh" }),
        ("Bắc Ninh", new[] { "Bac Ninh", "Bac Giang" }),
        ("Hưng Yên", new[] { "Hung Yen", "Thai Binh" }),
        ("Hải Phòng", new[] { "Hai Phong", "Haiphong", "Hai Duong" }),
        ("Ninh Bình", new[] { "Ninh Binh", "Ha Nam", "Nam Dinh" }),
        ("Quảng Trị", new[] { "Quang Tri", "Quang Binh" }),
        ("Đà Nẵng", new[] { "Da Nang", "Danang", "Quang Nam" }),
        ("Quảng Ngãi", new[] { "Quang Ngai", "Kon Tum" }),
        ("Gia Lai", new[] { "Gia Lai", "Binh Dinh" }),
        ("Khánh Hòa", new[] { "Khanh Hoa", "Ninh Thuan" }),
        ("Lâm Đồng", new[] { "Lam Dong", "Dak Nong", "Dac Nong", "Binh Thuan" }),
        ("Đắk Lắk", new[] { "Dak Lak", "Dac Lac", "Daklak", "Phu Yen" }),
        ("TP. Hồ Chí Minh", new[] { "Ho Chi Minh", "Ho Chi Minh City", "HCMC", "Saigon", "Sai Gon", "Binh Duong", "Ba Ria Vung Tau", "Ba Ria", "Vung Tau" }),
        ("Đồng Nai", new[] { "Dong Nai", "Binh Phuoc" }),
        ("Tây Ninh", new[] { "Tay Ninh", "Long An" }),
        ("Cần Thơ", new[] { "Can Tho", "Soc Trang", "Hau Giang" }),
        ("Vĩnh Long", new[] { "Vinh Long", "Ben Tre", "Tra Vinh" }),
        ("Đồng Tháp", new[] { "Dong Thap", "Tien Giang" }),
        ("Cà Mau", new[] { "Ca Mau", "Bac Lieu" }),
        ("An Giang", new[] { "An Giang", "Kien Giang" }),
    };

    private static readonly Dictionary<string, string> _tra = TaoBangTra();

    private static readonly Dictionary<string, string> _tenNuoc = new(StringComparer.OrdinalIgnoreCase)
    {
        ["US"] = "Mỹ", ["SG"] = "Singapore", ["JP"] = "Nhật Bản", ["KR"] = "Hàn Quốc", ["CN"] = "Trung Quốc",
        ["TW"] = "Đài Loan", ["HK"] = "Hồng Kông", ["TH"] = "Thái Lan", ["MY"] = "Malaysia", ["ID"] = "Indonesia",
        ["PH"] = "Philippines", ["KH"] = "Campuchia", ["LA"] = "Lào", ["AU"] = "Úc", ["NZ"] = "New Zealand",
        ["IN"] = "Ấn Độ", ["DE"] = "Đức", ["FR"] = "Pháp", ["GB"] = "Anh", ["NL"] = "Hà Lan", ["RU"] = "Nga",
        ["CA"] = "Canada", ["IE"] = "Ireland", ["SE"] = "Thụy Điển", ["FI"] = "Phần Lan", ["IT"] = "Ý", ["ES"] = "Tây Ban Nha",
    };

    /// <summary>Tên tỉnh/thành (34 đơn vị mới) từ tên vùng bất kỳ; null nếu không nhận ra.</summary>
    public static string? ChuanHoaTinh(string? tenVung)
    {
        if (string.IsNullOrWhiteSpace(tenVung)) return null;
        var k = Khoa(tenVung);
        foreach (var bo in new[] { "thanhpho", "tinh", "province", "city", "tp" })
        {
            if (_tra.TryGetValue(k, out var t)) return t;
            if (k.StartsWith(bo)) k = k[bo.Length..];
            if (k.EndsWith(bo)) k = k[..^bo.Length];
        }
        return _tra.TryGetValue(k, out var kq) ? kq : null;
    }

    public static string TenNuoc(string? maNuoc)
        => string.IsNullOrWhiteSpace(maNuoc) ? "không rõ" : (_tenNuoc.TryGetValue(maNuoc, out var t) ? t : maNuoc.ToUpperInvariant());

    /// <summary>Bỏ dấu, chữ thường, chỉ giữ chữ và số: "Thừa Thiên-Huế" -> "thuathienhue".</summary>
    public static string Khoa(string s)
    {
        var d = s.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    private static Dictionary<string, string> TaoBangTra()
    {
        var bang = new Dictionary<string, string>();
        foreach (var (moi, cu) in _sapNhap)
        {
            bang[Khoa(moi)] = moi;
            foreach (var c in cu) bang[Khoa(c)] = moi;
        }
        return bang;
    }
}
