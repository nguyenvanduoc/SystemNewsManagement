using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ThaiBeer.Core.Domain.Common;
using ThaiBeer.Core.Domain.Entities;

namespace ThaiBeer.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task KhoiTaoDuLieuMauAsync(ThaiBeerDbContext context, IConfiguration configuration, ILogger logger)
    {
        // 1. Tự động đảm bảo database và các bảng được tạo (nếu chưa có)
        await context.Database.EnsureCreatedAsync();

        // 2. Đảm bảo cấu trúc cột ToneMau trong bảng DanhMucs (chỉ thêm cột nếu thiếu cấu trúc, tuyệt đối KHÔNG tự ý UPDATE dữ liệu người dùng)
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'DanhMucs')
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DanhMucs') AND name = 'ToneMau')
                    BEGIN
                        EXEC('ALTER TABLE DanhMucs ADD ToneMau varchar(30) NULL DEFAULT ''#007A29'';');
                    END
                END
            ");
        }
        catch { }

        // 3. Bảng tài khoản đăng nhập Quản trị + tài khoản quản trị đầu tiên
        await DamBaoBangNguoiDungAsync(context);
        await KhoiTaoTaiKhoanQuanTriAsync(context, configuration, logger);

        // 4. Nếu database đã có dữ liệu danh mục hoặc sản phẩm, dừng lại ngay lập tức.
        // Tuyệt đối không can thiệp, không tự ý chèn dữ liệu mẫu hay update đè lên dữ liệu đã lưu của người dùng.
        if (await context.DanhMucs.AnyAsync() || await context.SanPhams.AnyAsync())
        {
            return;
        }

        // Đã dọn dẹp toàn bộ các câu lệnh tự ý UPDATE sản phẩm/danh mục và các khối tự động chèn dữ liệu giả lập.
        // Mọi dữ liệu sản phẩm, danh mục, bài viết, banner sẽ do quản trị viên toàn quyền thêm mới và chỉnh sửa qua trang Quản Trị (/quan-tri).
    }

    /// <summary>
    /// EnsureCreated() KHÔNG thêm bảng mới vào database đã tồn tại, nên tự tạo bảng NguoiDungs nếu chưa có.
    /// Cấu trúc cột khớp với entity NguoiDung.
    /// </summary>
    private static async Task DamBaoBangNguoiDungAsync(ThaiBeerDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NguoiDungs')
            BEGIN
                CREATE TABLE NguoiDungs (
                    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NguoiDungs PRIMARY KEY,
                    TenDangNhap       VARCHAR(50)   NOT NULL,
                    MatKhauHash       VARCHAR(500)  NOT NULL,
                    HoTen             NVARCHAR(150) NOT NULL,
                    Email             VARCHAR(150)  NULL,
                    VaiTro            VARCHAR(30)   NOT NULL,
                    TrangThaiHoatDong BIT           NOT NULL,
                    PhaiDoiMatKhau    BIT           NOT NULL,
                    SoLanDangNhapSai  INT           NOT NULL,
                    KhoaDenLuc        DATETIME2     NULL,
                    LanDangNhapCuoi   DATETIME2     NULL,
                    MaBaoMat          VARCHAR(64)   NOT NULL,
                    NgayTao           DATETIME2     NOT NULL,
                    NgayCapNhat       DATETIME2     NULL
                );
                CREATE UNIQUE INDEX IX_NguoiDungs_TenDangNhap ON NguoiDungs (TenDangNhap);
            END
        ");
    }

    /// <summary>
    /// Chỉ chạy khi bảng NguoiDungs CHƯA có tài khoản nào: tạo 1 Quản trị viên từ cấu hình AdminAccount
    /// (Username/Password trong appsettings.json). Mật khẩu được băm trước khi lưu và bắt buộc đổi ở lần đăng nhập đầu.
    /// Sau khi đã có tài khoản, cấu hình AdminAccount KHÔNG còn được dùng để đăng nhập nữa.
    /// </summary>
    private static async Task KhoiTaoTaiKhoanQuanTriAsync(ThaiBeerDbContext context, IConfiguration configuration, ILogger logger)
    {
        if (await context.NguoiDungs.AnyAsync())
            return;

        var tenDangNhap = (configuration["AdminAccount:Username"] ?? "admin").Trim().ToLowerInvariant();
        var matKhau = configuration["AdminAccount:Password"];

        if (string.IsNullOrWhiteSpace(matKhau))
        {
            logger.LogError("Bảng NguoiDungs trống và chưa cấu hình AdminAccount:Password, không thể tạo tài khoản quản trị đầu tiên.");
            return;
        }

        var admin = new NguoiDung
        {
            TenDangNhap = tenDangNhap,
            HoTen = "Quản Trị Viên ThaiBeer",
            VaiTro = VaiTroNguoiDung.QuanTriVien,
            TrangThaiHoatDong = true,
            PhaiDoiMatKhau = true,
            MaBaoMat = Guid.NewGuid().ToString("N"),
            NgayTao = DateTime.UtcNow
        };
        admin.MatKhauHash = new PasswordHasher<NguoiDung>().HashPassword(admin, matKhau);

        context.NguoiDungs.Add(admin);
        await context.SaveChangesAsync();

        logger.LogWarning("Đã tạo tài khoản quản trị đầu tiên '{TenDangNhap}' từ cấu hình AdminAccount. Hãy đăng nhập, đổi mật khẩu và xóa AdminAccount:Password khỏi appsettings.json.", tenDangNhap);
    }
}
