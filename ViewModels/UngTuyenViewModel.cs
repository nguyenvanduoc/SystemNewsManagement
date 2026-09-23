using System.ComponentModel.DataAnnotations;

namespace ThaiBeer.ViewModels;

public class UngTuyenViewModel
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

    [Required(ErrorMessage = "Vui lòng chọn vị trí ứng tuyển")]
    [MaxLength(150)]
    [Display(Name = "Vị trí ứng tuyển")]
    public string ViTriUngTuyen { get; set; } = "Quản lý kinh doanh khu vực (ASM)";

    [Required(ErrorMessage = "Vui lòng chọn hoặc nhập khu vực bạn muốn ứng tuyển")]
    [MaxLength(200)]
    [Display(Name = "Khu vực ứng tuyển (Miền Trung / Miền Nam / Miền Tây)")]
    public string TinhThanh { get; set; } = string.Empty;

    [MaxLength(1000)]
    [Display(Name = "Kinh nghiệm làm việc & Ghi chú")]
    public string? KinhNghiem { get; set; }
}
