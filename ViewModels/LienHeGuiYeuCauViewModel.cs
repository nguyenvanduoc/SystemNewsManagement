using System.ComponentModel.DataAnnotations;

namespace ThaiBeer.ViewModels;

public class LienHeGuiYeuCauViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên của bạn")]
    [MaxLength(150, ErrorMessage = "Họ tên không vượt quá 150 ký tự")]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [MaxLength(20, ErrorMessage = "Số điện thoại không hợp lệ")]
    [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
    [MaxLength(150)]
    [Display(Name = "Địa chỉ Email")]
    public string? Email { get; set; }

    [MaxLength(200)]
    [Display(Name = "Khu vực / Tỉnh thành")]
    public string? DiaChiKhuVuc { get; set; }

    [Display(Name = "Hình thức hợp tác")]
    public string LoaiHopTac { get; set; } = "Đại lý phân phối cấp 1";

    [MaxLength(1000)]
    [Display(Name = "Ghi chú thêm")]
    public string? NoiDungGhiChu { get; set; }
}
