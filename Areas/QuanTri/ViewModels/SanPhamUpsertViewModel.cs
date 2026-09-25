using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ThaiBeer.Areas.QuanTri.ViewModels;

public class SanPhamUpsertViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
    [MaxLength(250, ErrorMessage = "Tối đa 250 ký tự")]
    [Display(Name = "Tên sản phẩm")]
    public string TenSanPham { get; set; } = string.Empty;

    [MaxLength(250)]
    [Display(Name = "Đường dẫn thân thiện (Slug)")]
    public string? DuongDanSlug { get; set; }

    [MaxLength(500)]
    [Display(Name = "Mô tả tóm tắt")]
    public string? MoTaNgan { get; set; }

    [Display(Name = "Nội dung bài viết chi tiết (HTML)")]
    public string? NoiDungChiTiet { get; set; }

    [Display(Name = "Hình ảnh hiện tại (WebP)")]
    public string? HinhAnhHienTai { get; set; }

    [Display(Name = "Tải lên tệp ảnh mới (Tự động chuyển WebP)")]
    public IFormFile? TapTinHinhAnh { get; set; }

    [Display(Name = "Ảnh chi tiết hiện tại (Tối đa 4 ảnh)")]
    public List<string> HinhAnhPhuHienTai { get; set; } = new();

    [Display(Name = "Tải lên các ảnh chi tiết bổ sung (Tối đa 4 ảnh)")]
    public List<IFormFile>? TapTinHinhAnhPhu { get; set; }

    public List<string>? XoaHinhAnhPhu { get; set; }

    [Display(Name = "Video giới thiệu URL (YouTube / CDN)")]
    public string? VideoGioiThieuUrl { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập độ cồn (%)")]
    [Range(0, 100, ErrorMessage = "Độ cồn không hợp lệ")]
    [Display(Name = "Độ cồn (%)")]
    public decimal NongDoCon { get; set; } = 5.0m;

    [Required(ErrorMessage = "Vui lòng nhập dung tích")]
    [Range(1, 50000, ErrorMessage = "Dung tích từ 1ml trở lên")]
    [Display(Name = "Dung tích (ml)")]
    public int DungTichMl { get; set; } = 330;

    [MaxLength(100)]
    [Display(Name = "Xuất xứ")]
    public string XuatXu { get; set; } = "Thái Lan";

    [MaxLength(150)]
    [Display(Name = "Quy cách đóng gói")]
    public string QuyCachDongGoi { get; set; } = "Thùng 24 lon 330ml";

    [Display(Name = "Hiển thị lên Trang Chủ")]
    public bool HienThiTrangChu { get; set; } = true;

    [Display(Name = "Trạng thái hoạt động")]
    public bool TrangThaiHoatDong { get; set; } = true;

    [Display(Name = "Thứ tự sắp xếp")]
    public int ThuTuHienThi { get; set; } = 0;

    [Required(ErrorMessage = "Vui lòng chọn danh mục sản phẩm")]
    [Display(Name = "Danh mục sản phẩm")]
    public int DanhMucId { get; set; }

    // Danh sách chọn danh mục
    public IEnumerable<SelectListItem> DanhSachDanhMuc { get; set; } = new List<SelectListItem>();
}
