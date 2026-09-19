using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ThaiBeer.Areas.QuanTri.ViewModels;

public class BaiVietUpsertViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài viết")]
    [MaxLength(300, ErrorMessage = "Tiêu đề tối đa 300 ký tự")]
    [Display(Name = "Tiêu đề bài viết")]
    public string TieuDe { get; set; } = string.Empty;

    [MaxLength(300)]
    [Display(Name = "Đường dẫn thân thiện (Slug)")]
    public string? DuongDanSlug { get; set; }

    [MaxLength(600, ErrorMessage = "Tóm tắt tối đa 600 ký tự")]
    [Display(Name = "Tóm tắt ngắn")]
    public string? TomTat { get; set; }

    [Display(Name = "Nội dung bài viết chi tiết (HTML)")]
    public string? NoiDungHtml { get; set; }

    [Display(Name = "Hình ảnh hiện tại (WebP)")]
    public string? HinhAnhHienTai { get; set; }

    [Display(Name = "Tải lên ảnh mới (Tự động chuyển WebP)")]
    public IFormFile? TapTinHinhAnh { get; set; }

    [MaxLength(100)]
    [Display(Name = "Tác giả")]
    public string TacGia { get; set; } = "ThaiBeer Ban Biên Tập";

    [Display(Name = "Bài viết nổi bật")]
    public bool NoiBat { get; set; } = false;

    [Display(Name = "Trạng thái hiển thị")]
    public bool TrangThaiHoatDong { get; set; } = true;
}
