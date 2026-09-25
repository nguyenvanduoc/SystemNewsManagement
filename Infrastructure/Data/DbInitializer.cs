using Microsoft.EntityFrameworkCore;
using ThaiBeer.Core.Domain.Entities;

namespace ThaiBeer.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task KhoiTaoDuLieuMauAsync(ThaiBeerDbContext context)
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

        // 3. Nếu database đã có dữ liệu danh mục hoặc sản phẩm, dừng lại ngay lập tức.
        // Tuyệt đối không can thiệp, không tự ý chèn dữ liệu mẫu hay update đè lên dữ liệu đã lưu của người dùng.
        if (await context.DanhMucs.AnyAsync() || await context.SanPhams.AnyAsync())
        {
            return;
        }

        // Đã dọn dẹp toàn bộ các câu lệnh tự ý UPDATE sản phẩm/danh mục và các khối tự động chèn dữ liệu giả lập.
        // Mọi dữ liệu sản phẩm, danh mục, bài viết, banner sẽ do quản trị viên toàn quyền thêm mới và chỉnh sửa qua trang Quản Trị (/quan-tri).
    }
}
