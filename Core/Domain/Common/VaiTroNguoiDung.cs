namespace ThaiBeer.Core.Domain.Common;

/// <summary>Danh sách vai trò tài khoản quản trị (lưu vào cột NguoiDungs.VaiTro và Claim Role).</summary>
public static class VaiTroNguoiDung
{
    /// <summary>Toàn quyền, được quản lý tài khoản người dùng khác.</summary>
    public const string QuanTriVien = "QuanTriVien";

    /// <summary>Chỉ quản lý nội dung (sản phẩm, danh mục, bài viết, liên hệ).</summary>
    public const string BienTapVien = "BienTapVien";

    public static readonly IReadOnlyDictionary<string, string> TenHienThi = new Dictionary<string, string>
    {
        [QuanTriVien] = "Quản trị viên",
        [BienTapVien] = "Biên tập viên"
    };

    public static bool HopLe(string? vaiTro) => vaiTro == QuanTriVien || vaiTro == BienTapVien;
}
