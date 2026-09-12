using Microsoft.EntityFrameworkCore;
using ThaiBeer.Core.Domain.Entities;

namespace ThaiBeer.Infrastructure.Data;

public class ThaiBeerDbContext : DbContext
{
    public ThaiBeerDbContext(DbContextOptions<ThaiBeerDbContext> options) : base(options)
    {
    }

    public DbSet<DanhMuc> DanhMucs => Set<DanhMuc>();
    public DbSet<SanPham> SanPhams => Set<SanPham>();
    public DbSet<BaiViet> BaiViets => Set<BaiViet>();
    public DbSet<BannerQuangCao> BannerQuangCaos => Set<BannerQuangCao>();
    public DbSet<LienHe> LienHes => Set<LienHe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cấu hình DanhMuc
        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DuongDanSlug)
                  .IsUnique()
                  .HasDatabaseName("IX_DanhMucs_DuongDanSlug");
            
            entity.HasIndex(e => new { e.TrangThaiHoatDong, e.ThuTuHienThi })
                  .HasDatabaseName("IX_DanhMucs_TrangThai_ThuTu");

            entity.Property(e => e.DuongDanSlug).IsUnicode(false);
            entity.Property(e => e.HinhAnhWebP).IsUnicode(false);
            entity.Property(e => e.ToneMau).IsUnicode(false).HasMaxLength(30);
        });

        // Cấu hình SanPham
        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DuongDanSlug)
                  .IsUnique()
                  .HasDatabaseName("IX_SanPhams_DuongDanSlug");

            entity.HasIndex(e => e.DanhMucId)
                  .HasDatabaseName("IX_SanPhams_DanhMucId");

            entity.HasIndex(e => new { e.TrangThaiHoatDong, e.HienThiTrangChu, e.ThuTuHienThi })
                  .HasDatabaseName("IX_SanPhams_LocTrangChu");

            entity.Property(e => e.DuongDanSlug).IsUnicode(false);
            entity.Property(e => e.HinhAnhWebP).IsUnicode(false);
            entity.Property(e => e.DanhSachHinhAnhPhu).IsUnicode(false);
            entity.Property(e => e.VideoGioiThieuUrl).IsUnicode(false);
            entity.Property(e => e.NongDoCon).HasPrecision(4, 2);

            entity.HasOne(e => e.DanhMuc)
                  .WithMany(d => d.SanPhams)
                  .HasForeignKey(e => e.DanhMucId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Cấu hình BaiViet
        modelBuilder.Entity<BaiViet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DuongDanSlug)
                  .IsUnique()
                  .HasDatabaseName("IX_BaiViets_DuongDanSlug");

            entity.HasIndex(e => new { e.TrangThaiHoatDong, e.NgayTao })
                  .HasDatabaseName("IX_BaiViets_TrangThai_NgayTao");

            entity.Property(e => e.DuongDanSlug).IsUnicode(false);
            entity.Property(e => e.HinhAnhWebP).IsUnicode(false);
        });

        // Cấu hình BannerQuangCao
        modelBuilder.Entity<BannerQuangCao>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TrangThaiHoatDong, e.ThuTuHienThi })
                  .HasDatabaseName("IX_BannerQuangCaos_TrangThai_ThuTu");

            entity.Property(e => e.HinhAnhWebP).IsUnicode(false);
            entity.Property(e => e.HinhAnhMobileWebP).IsUnicode(false);
            entity.Property(e => e.LienKetUrl).IsUnicode(false);
        });

        // Cấu hình LienHe
        modelBuilder.Entity<LienHe>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DaXuLy, e.NgayTao })
                  .HasDatabaseName("IX_LienHes_DaXuLy_NgayTao");

            entity.Property(e => e.SoDienThoai).IsUnicode(false);
            entity.Property(e => e.Email).IsUnicode(false);
        });
    }
}
