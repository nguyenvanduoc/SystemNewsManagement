/* =====================================================================
   KHÔI PHỤC HÌNH ẢNH SẢN PHẨM BỊ LỖI 404 TRÊN saigoncuulong.vn
   ---------------------------------------------------------------------
   Nguyên nhân: database đang trỏ tới 8 file ảnh không tồn tại trên server
   (được upload từ một máy chạy localhost dùng chung database thật).
   Toàn bộ 13 file ảnh gốc trong wwwroot/uploads/san-pham/2026/09/ VẪN CÒN
   trên server (đã kiểm tra trả về HTTP 200), nên script này chỉ cần trỏ
   database về lại các file đó.

   An toàn:
   - Chỉ UPDATE khi giá trị hiện tại ĐÚNG BẰNG đường dẫn đang bị lỗi,
     nên chạy lại nhiều lần cũng không ảnh hưởng (idempotent).
   - Chạy trong TRANSACTION, có SELECT kiểm tra trước và sau.

   Cách chạy: mở SQL Server Management Studio (hoặc trang quản lý SQL của
   hosting), chọn database duocnguy_ThaiBeerDb, dán toàn bộ script và Execute.
   ===================================================================== */

SET XACT_ABORT ON;

PRINT N'--- TRƯỚC KHI KHÔI PHỤC ---';
SELECT Id, TenSanPham, DuongDanSlug, HinhAnhWebP, DanhSachHinhAnhPhu FROM SanPhams ORDER BY Id;
SELECT h.Id, h.SanPhamId, s.TenSanPham, h.ThuTu, h.DuongDanWebP
FROM HinhAnhSanPhams h JOIN SanPhams s ON s.Id = h.SanPhamId ORDER BY h.SanPhamId, h.ThuTu;

BEGIN TRANSACTION;

/* Bảng ánh xạ: đường dẫn đang lỗi (404) -> file gốc còn trên server */
DECLARE @AnhLoi TABLE (CuLoi NVARCHAR(500) PRIMARY KEY, MoiTonTai NVARCHAR(500) NOT NULL, GhiChu NVARCHAR(200));
INSERT INTO @AnhLoi (CuLoi, MoiTonTai, GhiChu) VALUES
 -- ẢNH CHÍNH
 (N'/uploads/san-pham/2026/09/d9599bc93586473989a21218a07b741a.webp', N'/uploads/san-pham/2026/09/fcb471b018e14a6197e95f96504927b9.webp', N'Thái Beer Premium SLEEK - thùng bạc lon cao'),
 (N'/uploads/san-pham/2026/09/750cb78296344e09804f8eecb47b059e.webp', N'/uploads/san-pham/2026/09/8e83a85ffe47408d919adc27e2081609.webp', N'Bia 123 ZO Premium Lager - lon cao'),
 (N'/uploads/san-pham/2026/09/c18e1b3ad13046108bc223d0c9f126d8.webp', N'/uploads/san-pham/2026/09/190e0df56c2c414eb2ecac4e68cc3f0e.webp', N'Bia 123 ZO Lager - lon lùn'),
 -- ẢNH PHỤ (chi tiết)
 (N'/uploads/san-pham/2026/09/46aef7c7c6a7484b8bdbfdb22a193694.webp', N'/uploads/san-pham/2026/09/ef0e3d7c381d4aa587dd98eb3d27cade.webp', N'Premium SLEEK - thiết kế thùng bạc'),
 (N'/uploads/san-pham/2026/09/d5eeea7872f24ab48f9b0f99ce9a9d39.webp', N'/uploads/san-pham/2026/09/deae5ef8a3354d218b232511a3342f89.webp', N'123 ZO Premium Lager - lon cao (ảnh 2)'),
 (N'/uploads/san-pham/2026/09/fc33202d20824af9a958ab9efc834de5.webp', N'/uploads/san-pham/2026/09/fe2fa2311e8544f3ae243131216d795e.webp', N'Thái Beer Lager - ảnh chụp lon xanh'),
 (N'/uploads/san-pham/2026/09/ae0fd6268da4450f8d8a6c569f014048.webp', N'/uploads/san-pham/2026/09/1222ca967a114cbb8f20d907795076cd.webp', N'Premium Sleek (đỏ) - lon đỏ');

/* 1. Ảnh chính của sản phẩm */
UPDATE s SET s.HinhAnhWebP = a.MoiTonTai, s.NgayCapNhat = SYSUTCDATETIME()
FROM SanPhams s JOIN @AnhLoi a ON s.HinhAnhWebP = a.CuLoi;
PRINT N'Đã sửa ảnh chính: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' sản phẩm';

/* 2. Ảnh phụ trong bảng HinhAnhSanPhams */
UPDATE h SET h.DuongDanWebP = a.MoiTonTai
FROM HinhAnhSanPhams h JOIN @AnhLoi a ON h.DuongDanWebP = a.CuLoi;
PRINT N'Đã sửa ảnh phụ: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' ảnh';

/* 3. Ảnh phụ còn bị lỗi mà không có ảnh thay thế (123 ZO Lager lon lùn) -> gỡ khỏi bộ sưu tập để không hiện ô trống */
DELETE FROM HinhAnhSanPhams WHERE DuongDanWebP = N'/uploads/san-pham/2026/09/ee20da12fa794033ade55ce7da178016.webp';
PRINT N'Đã gỡ ảnh phụ không có bản thay thế: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' ảnh';

/* 4. Đồng bộ lại cột DanhSachHinhAnhPhu từ bảng HinhAnhSanPhams (giống logic SanPhamController.ChinhSua) */
/*    (dùng FOR XML PATH thay cho STRING_AGG để chạy được cả SQL Server 2012-2016) */
UPDATE s SET s.DanhSachHinhAnhPhu = ISNULL(STUFF((
    SELECT N';' + h.DuongDanWebP
    FROM HinhAnhSanPhams h WHERE h.SanPhamId = s.Id
    ORDER BY h.ThuTu
    FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, N''), N'')
FROM SanPhams s
WHERE EXISTS (SELECT 1 FROM @AnhLoi a WHERE s.DanhSachHinhAnhPhu LIKE N'%' + a.CuLoi + N'%')
   OR s.DanhSachHinhAnhPhu LIKE N'%ee20da12fa794033ade55ce7da178016%';
PRINT N'Đã đồng bộ DanhSachHinhAnhPhu: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' sản phẩm';

COMMIT TRANSACTION;

PRINT N'--- SAU KHI KHÔI PHỤC ---';
SELECT Id, TenSanPham, HinhAnhWebP, DanhSachHinhAnhPhu FROM SanPhams ORDER BY Id;
SELECT h.Id, h.SanPhamId, s.TenSanPham, h.ThuTu, h.DuongDanWebP
FROM HinhAnhSanPhams h JOIN SanPhams s ON s.Id = h.SanPhamId ORDER BY h.SanPhamId, h.ThuTu;

/* LƯU Ý: Trang web có OutputCache 15-60 phút. Sau khi chạy script, để thấy ngay:
   - Recycle Application Pool / Restart website trên hosting, HOẶC
   - Vào /quan-tri/san-pham, bấm Chỉnh sửa 1 sản phẩm bất kỳ rồi Lưu (tự xoá cache). */
