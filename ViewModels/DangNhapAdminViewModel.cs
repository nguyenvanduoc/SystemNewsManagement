using System.ComponentModel.DataAnnotations;

namespace ThaiBeer.ViewModels;

public class DangNhapAdminViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [Display(Name = "Tên đăng nhập")]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string MatKhau { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool GhiNhoDangNhap { get; set; } = true;

    public string? ReturnUrl { get; set; }
}
