using Microsoft.EntityFrameworkCore;
using ThaiBeer.Core.Domain.Entities;

namespace ThaiBeer.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task KhoiTaoDuLieuMauAsync(ThaiBeerDbContext context)
    {
        // Tự động tạo bảng nếu chưa có
        await context.Database.EnsureCreatedAsync();

        // Đảm bảo cột ToneMau tồn tại nếu bảng đã được tạo trước đó
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'DanhMucs')
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DanhMucs') AND name = 'ToneMau')
                    BEGIN
                        EXEC('ALTER TABLE DanhMucs ADD ToneMau varchar(30) NULL DEFAULT ''#007A29'';');
                    END
                    EXEC('UPDATE DanhMucs SET ToneMau = ''#007A29'' WHERE ToneMau IS NULL;');
                END

                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'SanPhams')
                BEGIN
                    UPDATE SanPhams 
                    SET TenSanPham = 'THAIBEER SLEEK', 
                        MoTaNgan = N'Thái bạc lon cao', 
                        HinhAnhWebP = '/images/logo/thailoncao.png'
                    WHERE ThuTuHienThi = 1;
                END
            ");
        }
        catch { }

        // Kiểm tra nếu đã có dữ liệu thì không seed lại
        if (await context.DanhMucs.AnyAsync())
        {
            return;
        }

        // 1. Khởi tạo Danh Mục
        var danhMucChai = new DanhMuc
        {
            TenDanhMuc = "Bia Chai Thái Cao Cấp",
            DuongDanSlug = "bia-chai-thai",
            MoTa = "Các dòng bia chai thủy tinh hảo hạng, giữ trọn hương vị sảng khoái mát lạnh và phong cách hoàng gia.",
            HinhAnhWebP = "/images/categories/bia-chai.webp",
            ThuTuHienThi = 1,
            TrangThaiHoatDong = true,
            ToneMau = "#007A29"
        };

        var danhMucLon = new DanhMuc
        {
            TenDanhMuc = "Bia Lon Tiện Lợi",
            DuongDanSlug = "bia-lon-thai",
            MoTa = "Bia lon thời thượng, tiện lợi cho các bữa tiệc ngoài trời, dã ngoại và hội họp bạn bè.",
            HinhAnhWebP = "/images/categories/bia-lon.webp",
            ThuTuHienThi = 2,
            TrangThaiHoatDong = true,
            ToneMau = "#007A29"
        };

        var danhMucCraft = new DanhMuc
        {
            TenDanhMuc = "Bia Tươi & Craft Thủ Công",
            DuongDanSlug = "bia-tuoi-craft",
            MoTa = "Hương vị thủ công độc đáo với men bia sống và hoa bia tuyển chọn, độ đắng êm dịu.",
            HinhAnhWebP = "/images/categories/bia-craft.webp",
            ThuTuHienThi = 3,
            TrangThaiHoatDong = true,
            ToneMau = "#D4AF37"
        };

        var danhMucQuaTang = new DanhMuc
        {
            TenDanhMuc = "Hộp Quà & Phụ Kiện Thương Hiệu",
            DuongDanSlug = "hop-qua-phu-kien",
            MoTa = "Bộ sưu tập ly thủy tinh pha lê cao cấp và set quà tặng đặc biệt cho đối tác.",
            HinhAnhWebP = "/images/categories/qua-tang.webp",
            ThuTuHienThi = 4,
            TrangThaiHoatDong = true,
            ToneMau = "#900C13"
        };

        context.DanhMucs.AddRange(danhMucChai, danhMucLon, danhMucCraft, danhMucQuaTang);
        await context.SaveChangesAsync();

        // 2. Khởi tạo Banner Trang Chủ
        var banner1 = new BannerQuangCao
        {
            TieuDe = "ThaiBeer - Hương Vị Hoàng Gia Đích Thực",
            PhuDe = "Sự kết hợp hoàn hảo giữa hoa bia ngoại nhập thượng hạng và bí quyết ủ men truyền thống Thái Lan.",
            HinhAnhWebP = "https://images.unsplash.com/photo-1535958636474-b021ee887b13?q=80&w=1920&auto=format&fit=crop",
            LienKetUrl = "/san-pham",
            TextNutBam = "Khám Phá Bộ Sưu Tập",
            ThuTuHienThi = 1,
            TrangThaiHoatDong = true
        };

        var banner2 = new BannerQuangCao
        {
            TieuDe = "Sảng Khoái Bất Tận - Khởi Sắc Mọi Cuộc Vui",
            PhuDe = "Độ cồn 5.0% êm ái, bọt mịn màng, hậu vị ngọt thanh chuẩn gu sành điệu.",
            HinhAnhWebP = "https://images.unsplash.com/photo-1518176258769-f227c798150e?q=80&w=1920&auto=format&fit=crop",
            LienKetUrl = "/lien-he",
            TextNutBam = "Hợp Tác Phân Phối",
            ThuTuHienThi = 2,
            TrangThaiHoatDong = true
        };

        context.BannerQuangCaos.AddRange(banner1, banner2);

        // 3. Khởi tạo Sản Phẩm Mẫu
        var sanPhams = new List<SanPham>
        {
            new()
            {
                TenSanPham = "ThaiBeer Original Premium Lager 330ml (Chai)",
                DuongDanSlug = "thaibeer-original-premium-lager-330ml-chai",
                MoTaNgan = "Dòng bia chai hoàng gia biểu tượng với sắc vàng óng ánh, lớp bọt tuyết dày mịn và hậu vị mạch nha êm dịu.",
                NoiDungChiTiet = "<p>ThaiBeer Original Lager là sự kết tinh của tinh hoa nấu bia truyền thống Thái Lan. Nguồn nước ngầm tinh khiết hòa quyện cùng mạch nha lúa mạch thượng hạng tạo nên sắc vàng hổ phách cuốn hút. Vị đắng thanh thoát từ hoa bia quý tộc lưu lại hậu vị ngọt ngào bền lâu.</p><ul><li>Độ cồn: 5.0%</li><li>Dung tích: 330ml</li><li>Nhiệt độ thưởng thức lý tưởng: 4°C - 6°C</li></ul>",
                HinhAnhWebP = "https://images.unsplash.com/photo-1608270177770-9831a2a46618?q=80&w=800&auto=format&fit=crop",
                NongDoCon = 5.0m,
                DungTichMl = 330,
                XuatXu = "Thái Lan",
                QuyCachDongGoi = "Két 24 chai 330ml",
                HienThiTrangChu = true,
                TrangThaiHoatDong = true,
                ThuTuHienThi = 1,
                DanhMucId = danhMucChai.Id
            },
            new()
            {
                TenSanPham = "ThaiBeer Gold Extra Canned 330ml (Lon)",
                DuongDanSlug = "thaibeer-gold-extra-canned-330ml-lon",
                MoTaNgan = "Thiết kế lon vàng ánh kim sang trọng, giữ trọn độ tươi mát sảng khoái mọi lúc mọi nơi.",
                NoiDungChiTiet = "<p>Dòng bia lon cao cấp ThaiBeer Gold Extra được đóng lon với công nghệ chiết lạnh vô trùng, bảo toàn độ tươi mới và hàm lượng gas sảng khoái tối đa. Hoàn hảo cho các bữa tiệc nướng BBQ và hội ngộ bạn bè.</p>",
                HinhAnhWebP = "https://images.unsplash.com/photo-1618886614638-80e3c103d31a?q=80&w=800&auto=format&fit=crop",
                NongDoCon = 5.2m,
                DungTichMl = 330,
                XuatXu = "Thái Lan",
                QuyCachDongGoi = "Thùng 24 lon 330ml",
                HienThiTrangChu = true,
                TrangThaiHoatDong = true,
                ThuTuHienThi = 2,
                DanhMucId = danhMucLon.Id
            },
            new()
            {
                TenSanPham = "ThaiBeer Siam Wheat Craft Ale 330ml",
                DuongDanSlug = "thaibeer-siam-wheat-craft-ale-330ml",
                MoTaNgan = "Bia lúa mì thủ công phong cách Thái thoang thoảng hương vỏ cam nhiệt đới và hoa ngò rí.",
                NoiDungChiTiet = "<p>Siam Wheat Ale mở ra một trải nghiệm độc bản: chất bia sánh đục tự nhiên từ lúa mì mùa đông, điểm xuyết nốt hương vỏ cam sành tươi và hạt mùi Thái Lan, mang lại cảm giác giải nhiệt tuyệt đối.</p>",
                HinhAnhWebP = "https://images.unsplash.com/photo-1584225064785-c62a8b43d148?q=80&w=800&auto=format&fit=crop",
                NongDoCon = 4.8m,
                DungTichMl = 330,
                XuatXu = "Thái Lan",
                QuyCachDongGoi = "Thùng 24 chai 330ml",
                HienThiTrangChu = true,
                TrangThaiHoatDong = true,
                ThuTuHienThi = 3,
                DanhMucId = danhMucCraft.Id
            },
            new()
            {
                TenSanPham = "ThaiBeer Imperial Black Stout 330ml",
                DuongDanSlug = "thaibeer-imperial-black-stout-330ml",
                MoTaNgan = "Hương vị đen huyền bí với nốt hương cà phê rang cháy và socola đen nồng nàn.",
                NoiDungChiTiet = "<p>Dành riêng cho những người đam mê chiều sâu vị giác, ThaiBeer Imperial Black Stout sở hữu màu đen sánh đặc, hương cà phê robusta và cacao nguyên chất cùng độ cồn 5.5% ấm nồng.</p>",
                HinhAnhWebP = "https://images.unsplash.com/photo-1567696911980-2eed69a46042?q=80&w=800&auto=format&fit=crop",
                NongDoCon = 5.5m,
                DungTichMl = 330,
                XuatXu = "Thái Lan",
                QuyCachDongGoi = "Thùng 24 chai 330ml",
                HienThiTrangChu = true,
                TrangThaiHoatDong = true,
                ThuTuHienThi = 4,
                DanhMucId = danhMucCraft.Id
            },
            new()
            {
                TenSanPham = "ThaiBeer King Export Canned 500ml",
                DuongDanSlug = "thaibeer-king-export-canned-500ml",
                MoTaNgan = "Phiên bản lon lớn 500ml xuất khẩu, độ cồn cân bằng, sảng khoái kéo dài.",
                NoiDungChiTiet = "<p>Lon đại 500ml dành cho những tín đồ yêu thích sự trọn vẹn. Hương hoa bia sắc nét, độ gas dồi dào giải tỏa mọi cơn khát.</p>",
                HinhAnhWebP = "https://images.unsplash.com/photo-1527661591475-527312dd65f5?q=80&w=800&auto=format&fit=crop",
                NongDoCon = 5.0m,
                DungTichMl = 500,
                XuatXu = "Thái Lan",
                QuyCachDongGoi = "Thùng 12 lon 500ml",
                HienThiTrangChu = true,
                TrangThaiHoatDong = true,
                ThuTuHienThi = 5,
                DanhMucId = danhMucLon.Id
            },
            new()
            {
                TenSanPham = "Bộ Ly Pha Lê Thưởng Bia ThaiBeer Royal Edition",
                DuongDanSlug = "bo-ly-pha-le-thuong-bia-thaibeer-royal-edition",
                MoTaNgan = "Bộ 2 ly thủy tinh pha lê cao cấp in logo nhũ vàng độc quyền thương hiệu ThaiBeer.",
                NoiDungChiTiet = "<p>Ly được thiết kế dáng Tulip giữ bọt lâu và tập trung hương thơm hoa bia. Món quà sang trọng dành tặng đối tác và khách hàng sành điệu.</p>",
                HinhAnhWebP = "https://images.unsplash.com/photo-1575037614876-c38a4d44f5b8?q=80&w=800&auto=format&fit=crop",
                NongDoCon = 0.0m,
                DungTichMl = 450,
                XuatXu = "Thái Lan",
                QuyCachDongGoi = "Hộp quà 2 ly cao cấp",
                HienThiTrangChu = true,
                TrangThaiHoatDong = true,
                ThuTuHienThi = 6,
                DanhMucId = danhMucQuaTang.Id
            }
        };

        context.SanPhams.AddRange(sanPhams);

        // 4. Khởi tạo Bài Viết Mẫu
        var baiViets = new List<BaiViet>
        {
            new()
            {
                TieuDe = "Nghệ Thuật Thưởng Thức Bia Chuẩn Phong Vị Hoàng Gia Thái Lan",
                DuongDanSlug = "nghe-thuat-thuong-thuc-bia-chuan-phong-vi-hoang-gia-thai-lan",
                TomTat = "Bí quyết rót bia tạo bọt tuyết dày mịn 2 ngón tay và kết hợp hoàn hảo cùng các món ăn cay nồng đặc trưng Đông Nam Á.",
                NoiDungHtml = "<p>Để cảm nhận trọn vẹn hương vị của một chai bia ThaiBeer, ly thưởng thức phải được ướp lạnh ở 4-6°C. Khi rót, nghiêng ly 45 độ và đưa dần về góc thẳng đứng để tạo lớp bọt tuyết mịn màng...</p>",
                HinhAnhWebP = "https://images.unsplash.com/photo-1514933651103-005eec06c04b?q=80&w=800&auto=format&fit=crop",
                TacGia = "ThaiBeer Master Brewer",
                NoiBat = true,
                TrangThaiHoatDong = true
            },
            new()
            {
                TieuDe = "Quy Trình Tuyển Chọn Hoa Bia Và Mạch Nha Tạo Nên Chất Lượng ThaiBeer",
                DuongDanSlug = "quy-trinh-tuyen-chon-hoa-bia-va-mach-nha-tao-nen-chat-luong-thaibeer",
                TomTat = "Hành trình từ những cánh đồng đại mạch trù phú đến ly bia vàng óng ánh trên bàn tiệc.",
                NoiDungHtml = "<p>Mỗi giọt bia ThaiBeer là cam kết khắt khe về nguồn nguyên liệu không biến đổi gen (Non-GMO), hoa bia Saaz quý tộc và công nghệ lên men lạnh 21 ngày đêm...</p>",
                HinhAnhWebP = "https://images.unsplash.com/photo-1571613316887-6f8d5cbf7ef7?q=80&w=800&auto=format&fit=crop",
                TacGia = "Ban Biên Tập ThaiBeer",
                NoiBat = true,
                TrangThaiHoatDong = true
            },
            new()
            {
                TieuDe = "ThaiBeer Vinh Dự Đạt Huy Chương Vàng World Beer Championship",
                DuongDanSlug = "thaibeer-vinh-du-dat-huy-chuong-vang-world-beer-championship",
                TomTat = "Vượt qua hàng trăm thương hiệu bia quốc tế để khẳng định vị thế bia lager hảo hạng hàng đầu khu vực.",
                NoiDungHtml = "<p>Tại lễ trao giải vừa qua, dòng bia ThaiBeer Original Lager đã xuất sắc chinh phục hội đồng giám khảo khó tính nhờ độ êm mượt và sự cân bằng vị giác hoàn hảo...</p>",
                HinhAnhWebP = "https://images.unsplash.com/photo-1510812431401-41d2bd2722f3?q=80&w=800&auto=format&fit=crop",
                TacGia = "Tin Tức Sự Kiện",
                NoiBat = false,
                TrangThaiHoatDong = true
            }
        };

        context.BaiViets.AddRange(baiViets);
        await context.SaveChangesAsync();
    }
}
