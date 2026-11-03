using System.Security.Claims;
using ThaiBeer.Core.Domain.Entities;

namespace ThaiBeer.Core.Interfaces;

public enum TrangThaiDangNhap
{
    ThanhCong,
    SaiThongTin,
    BiKhoaTam,
    BiVoHieuHoa
}

public record KetQuaDangNhap(TrangThaiDangNhap TrangThai, NguoiDung? NguoiDung = null, DateTime? KhoaDenLuc = null);

public interface ITaiKhoanService
{
    /// <summary>Xác thực tên đăng nhập + mật khẩu, có đếm sai và khóa tạm thời.</summary>
    Task<KetQuaDangNhap> DangNhapAsync(string tenDangNhap, string matKhau);

    /// <summary>Tạo tài khoản mới, mật khẩu được băm trước khi lưu.</summary>
    Task<(bool ThanhCong, string? Loi)> TaoTaiKhoanAsync(string tenDangNhap, string matKhau, string hoTen, string? email, string vaiTro, bool phaiDoiMatKhau);

    /// <summary>Người dùng tự đổi mật khẩu (phải nhập đúng mật khẩu cũ).</summary>
    Task<(bool ThanhCong, string? Loi)> DoiMatKhauAsync(int nguoiDungId, string matKhauCu, string matKhauMoi);

    /// <summary>Quản trị viên đặt lại mật khẩu cho người khác (bắt buộc đổi ở lần đăng nhập sau).</summary>
    Task<(bool ThanhCong, string? Loi)> DatLaiMatKhauAsync(int nguoiDungId, string matKhauMoi);

    /// <summary>Kiểm tra cookie còn hợp lệ: tài khoản còn hoạt động và mã bảo mật khớp.</summary>
    Task<bool> PhienConHopLeAsync(int nguoiDungId, string? maBaoMat);

    /// <summary>Tạo ClaimsPrincipal để ghi vào cookie đăng nhập.</summary>
    ClaimsPrincipal TaoPrincipal(NguoiDung nguoiDung);

    /// <summary>Kiểm tra độ mạnh mật khẩu, trả về thông báo lỗi hoặc null nếu hợp lệ.</summary>
    string? KiemTraDoManhMatKhau(string? matKhau);
}
