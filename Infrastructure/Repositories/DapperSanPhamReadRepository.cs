using Dapper;
using ThaiBeer.Core.Domain.Entities;
using ThaiBeer.Core.Interfaces;
using ThaiBeer.Infrastructure.Data;

namespace ThaiBeer.Infrastructure.Repositories;

public class DapperSanPhamReadRepository : ISanPhamReadRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperSanPhamReadRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<SanPham>> LayDanhSachTrangChuAsync(int soLuong = 8)
    {
        using var connection = _connectionFactory.TaoKetNoi();
        const string sql = @"
            SELECT TOP (@SoLuong)
                p.Id, p.TenSanPham, p.DuongDanSlug, p.MoTaNgan, p.HinhAnhWebP,
                p.NongDoCon, p.DungTichMl, p.XuatXu, p.QuyCachDongGoi,
                p.HienThiTrangChu, p.TrangThaiHoatDong, p.ThuTuHienThi, p.DanhMucId,
                d.Id, d.TenDanhMuc, d.DuongDanSlug
            FROM SanPhams p WITH (NOLOCK)
            INNER JOIN DanhMucs d WITH (NOLOCK) ON p.DanhMucId = d.Id
            WHERE p.TrangThaiHoatDong = 1 AND p.HienThiTrangChu = 1
            ORDER BY p.ThuTuHienThi ASC, p.Id DESC;";

        var ketQua = await connection.QueryAsync<SanPham, DanhMuc, SanPham>(
            sql,
            (sp, dm) =>
            {
                sp.DanhMuc = dm;
                return sp;
            },
            new { SoLuong = soLuong },
            splitOn: "Id");

        return ketQua.ToList();
    }

    public async Task<IReadOnlyList<SanPham>> LayDanhSachTheoDanhMucSlugAsync(string slug, int trang = 1, int soLuong = 12, string? tuKhoa = null, string? sapXep = null)
    {
        using var connection = _connectionFactory.TaoKetNoi();
        var offset = (trang - 1) * soLuong;

        string orderByClause = sapXep switch
        {
            "cu-nhat" => "p.Id ASC",
            "ten-az" => "p.TenSanPham ASC",
            "ten-za" => "p.TenSanPham DESC",
            _ => "p.ThuTuHienThi ASC, p.Id DESC"
        };

        string sql = $@"
            SELECT 
                p.Id, p.TenSanPham, p.DuongDanSlug, p.MoTaNgan, p.HinhAnhWebP,
                p.NongDoCon, p.DungTichMl, p.XuatXu, p.QuyCachDongGoi,
                p.HienThiTrangChu, p.TrangThaiHoatDong, p.ThuTuHienThi, p.DanhMucId,
                d.Id, d.TenDanhMuc, d.DuongDanSlug
            FROM SanPhams p WITH (NOLOCK)
            INNER JOIN DanhMucs d WITH (NOLOCK) ON p.DanhMucId = d.Id
            WHERE p.TrangThaiHoatDong = 1 
              AND (@Slug IS NULL OR @Slug = '' OR d.DuongDanSlug = @Slug)
              AND (@TuKhoa IS NULL OR @TuKhoa = '' OR p.TenSanPham LIKE @TuKhoaPattern OR p.MoTaNgan LIKE @TuKhoaPattern)
            ORDER BY {orderByClause}
            OFFSET @Offset ROWS FETCH NEXT @SoLuong ROWS ONLY;";

        var tuKhoaPattern = string.IsNullOrWhiteSpace(tuKhoa) ? null : $"%{tuKhoa.Trim()}%";

        var ketQua = await connection.QueryAsync<SanPham, DanhMuc, SanPham>(
            sql,
            (sp, dm) =>
            {
                sp.DanhMuc = dm;
                return sp;
            },
            new { Slug = slug, Offset = offset, SoLuong = soLuong, TuKhoa = tuKhoa, TuKhoaPattern = tuKhoaPattern },
            splitOn: "Id");

        return ketQua.ToList();
    }

    public async Task<int> DemSoLuongTheoDanhMucSlugAsync(string slug, string? tuKhoa = null)
    {
        using var connection = _connectionFactory.TaoKetNoi();
        const string sql = @"
            SELECT COUNT(1)
            FROM SanPhams p WITH (NOLOCK)
            INNER JOIN DanhMucs d WITH (NOLOCK) ON p.DanhMucId = d.Id
            WHERE p.TrangThaiHoatDong = 1 
              AND (@Slug IS NULL OR @Slug = '' OR d.DuongDanSlug = @Slug)
              AND (@TuKhoa IS NULL OR @TuKhoa = '' OR p.TenSanPham LIKE @TuKhoaPattern OR p.MoTaNgan LIKE @TuKhoaPattern);";

        var tuKhoaPattern = string.IsNullOrWhiteSpace(tuKhoa) ? null : $"%{tuKhoa.Trim()}%";

        return await connection.ExecuteScalarAsync<int>(sql, new { Slug = slug, TuKhoa = tuKhoa, TuKhoaPattern = tuKhoaPattern });
    }

    public async Task<SanPham?> LayChiTietTheoSlugAsync(string slug)
    {
        using var connection = _connectionFactory.TaoKetNoi();
        const string sql = @"
            SELECT 
                p.Id, p.TenSanPham, p.DuongDanSlug, p.MoTaNgan, p.NoiDungChiTiet, p.HinhAnhWebP,
                p.DanhSachHinhAnhPhu, p.VideoGioiThieuUrl, p.NongDoCon, p.DungTichMl, p.XuatXu, 
                p.QuyCachDongGoi, p.HienThiTrangChu, p.TrangThaiHoatDong, p.ThuTuHienThi, p.LuotXem, p.DanhMucId,
                d.Id, d.TenDanhMuc, d.DuongDanSlug
            FROM SanPhams p WITH (NOLOCK)
            INNER JOIN DanhMucs d WITH (NOLOCK) ON p.DanhMucId = d.Id
            WHERE p.DuongDanSlug = @Slug AND p.TrangThaiHoatDong = 1;";

        var ketQua = await connection.QueryAsync<SanPham, DanhMuc, SanPham>(
            sql,
            (sp, dm) =>
            {
                sp.DanhMuc = dm;
                return sp;
            },
            new { Slug = slug },
            splitOn: "Id");

        return ketQua.FirstOrDefault();
    }

    public async Task<IReadOnlyList<SanPham>> LaySanPhamLienQuanAsync(int danhMucId, int sanPhamHienTaiId, int soLuong = 4)
    {
        using var connection = _connectionFactory.TaoKetNoi();
        const string sql = @"
            SELECT TOP (@SoLuong)
                p.Id, p.TenSanPham, p.DuongDanSlug, p.MoTaNgan, p.HinhAnhWebP,
                p.NongDoCon, p.DungTichMl, p.XuatXu, p.QuyCachDongGoi,
                p.HienThiTrangChu, p.TrangThaiHoatDong, p.ThuTuHienThi, p.DanhMucId,
                d.Id, d.TenDanhMuc, d.DuongDanSlug
            FROM SanPhams p WITH (NOLOCK)
            INNER JOIN DanhMucs d WITH (NOLOCK) ON p.DanhMucId = d.Id
            WHERE p.TrangThaiHoatDong = 1 
              AND p.DanhMucId = @DanhMucId 
              AND p.Id <> @SanPhamHienTaiId
            ORDER BY p.ThuTuHienThi ASC, p.Id DESC;";

        var ketQua = await connection.QueryAsync<SanPham, DanhMuc, SanPham>(
            sql,
            (sp, dm) =>
            {
                sp.DanhMuc = dm;
                return sp;
            },
            new { DanhMucId = danhMucId, SanPhamHienTaiId = sanPhamHienTaiId, SoLuong = soLuong },
            splitOn: "Id");

        return ketQua.ToList();
    }

    public async Task<IReadOnlyList<BannerQuangCao>> LayBannerTrangChuAsync()
    {
        using var connection = _connectionFactory.TaoKetNoi();
        const string sql = @"
            SELECT 
                Id, TieuDe, PhuDe, HinhAnhWebP, HinhAnhMobileWebP, LienKetUrl, TextNutBam, ThuTuHienThi
            FROM BannerQuangCaos WITH (NOLOCK)
            WHERE TrangThaiHoatDong = 1
            ORDER BY ThuTuHienThi ASC, Id DESC;";

        var ketQua = await connection.QueryAsync<BannerQuangCao>(sql);
        return ketQua.ToList();
    }

    public async Task<IReadOnlyList<DanhMuc>> LayDanhSachDanhMucHoatDongAsync()
    {
        using var connection = _connectionFactory.TaoKetNoi();
        const string sql = @"
            SELECT 
                Id, TenDanhMuc, DuongDanSlug, MoTa, HinhAnhWebP, ThuTuHienThi, ToneMau
            FROM DanhMucs WITH (NOLOCK)
            WHERE TrangThaiHoatDong = 1
            ORDER BY ThuTuHienThi ASC, Id ASC;";

        var ketQua = await connection.QueryAsync<DanhMuc>(sql);
        return ketQua.ToList();
    }

    public async Task<IReadOnlyList<BaiViet>> LayTinTucNoiBatAsync(int soLuong = 3)
    {
        using var connection = _connectionFactory.TaoKetNoi();
        const string sql = @"
            SELECT TOP (@SoLuong)
                Id, TieuDe, DuongDanSlug, TomTat, HinhAnhWebP, TacGia, NgayTao, LuotXem
            FROM BaiViets WITH (NOLOCK)
            WHERE TrangThaiHoatDong = 1
            ORDER BY NoiBat DESC, NgayTao DESC;";

        var ketQua = await connection.QueryAsync<BaiViet>(sql, new { SoLuong = soLuong });
        return ketQua.ToList();
    }

    public async Task<BaiViet?> LayChiTietBaiVietAsync(string slug)
    {
        using var connection = _connectionFactory.TaoKetNoi();
        const string sql = @"
            SELECT 
                Id, TieuDe, DuongDanSlug, TomTat, NoiDungHtml, HinhAnhWebP, TacGia, NgayTao, LuotXem
            FROM BaiViets WITH (NOLOCK)
            WHERE DuongDanSlug = @Slug AND TrangThaiHoatDong = 1;";

        return await connection.QueryFirstOrDefaultAsync<BaiViet>(sql, new { Slug = slug });
    }

    public async Task TangLuotXemSanPhamAsync(int id)
    {
        using var connection = _connectionFactory.TaoKetNoi();
        const string sql = "UPDATE SanPhams SET LuotXem = LuotXem + 1 WHERE Id = @Id;";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
