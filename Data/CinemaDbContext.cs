using Microsoft.EntityFrameworkCore;
using CinemaManagement.Models;

namespace CinemaManagement.Data
{
    public class CinemaDbContext : DbContext
    {
        public CinemaDbContext(DbContextOptions<CinemaDbContext> options) : base(options)
        {
        }

        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<PhongChieu> PhongChieus { get; set; }
        public DbSet<GheNgoi> GheNgois { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<TaiKhoan> TaiKhoans { get; set; }
        public DbSet<Phim> Phims { get; set; }
        public DbSet<LichChieu> LichChieus { get; set; }
        public DbSet<Ve> Ves { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<CTHD> CTHDs { get; set; }
        public DbSet<HDVoucher> HDVouchers { get; set; }
        public DbSet<DanhGiaPhim> DanhGiaPhims { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<TinNhan> TinNhans { get; set; }

        // Product related DbSets
        public DbSet<SanPham> SanPhams { get; set; }
        public DbSet<GioHang> GioHangs { get; set; }
        public DbSet<ChiTietGioHang> ChiTietGioHangs { get; set; }
        public DbSet<HoaDonSanPham> HoaDonSanPhams { get; set; }
        public DbSet<ChiTietHoaDonSanPham> ChiTietHoaDonSanPhams { get; set; }
        public DbSet<VoucherSanPham> VoucherSanPhams { get; set; }
        public DbSet<HoaDonSanPhamVoucher> HoaDonSanPhamVouchers { get; set; }
        public DbSet<YeuCauHoanTra> YeuCauHoanTras { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure composite key for HDVoucher
            modelBuilder.Entity<HDVoucher>()
                .HasKey(h => new { h.MaHoaDon, h.MaGiamGia });

            // Configure table names to match database
            modelBuilder.Entity<NhanVien>().ToTable("NhanVien");
            modelBuilder.Entity<PhongChieu>().ToTable("PhongChieu");
            modelBuilder.Entity<GheNgoi>().ToTable("GheNgoi");
            modelBuilder.Entity<KhachHang>().ToTable("KhachHang");
            modelBuilder.Entity<TaiKhoan>().ToTable("TaiKhoan");
            modelBuilder.Entity<Phim>().ToTable("Phim");
            modelBuilder.Entity<LichChieu>().ToTable("LichChieu");
            modelBuilder.Entity<Ve>().ToTable("Ve");
            modelBuilder.Entity<Voucher>().ToTable("Voucher");
            modelBuilder.Entity<HoaDon>().ToTable("HoaDon");
            modelBuilder.Entity<CTHD>().ToTable("CTHD");
            modelBuilder.Entity<HDVoucher>().ToTable("HD_voucher");
            modelBuilder.Entity<DanhGiaPhim>().ToTable("DanhGiaPhim");
            modelBuilder.Entity<ChatMessage>().ToTable("ChatMessage");
            modelBuilder.Entity<TinNhan>().ToTable("TinNhan");

            // Product related tables
            modelBuilder.Entity<SanPham>().ToTable("SanPham");
            modelBuilder.Entity<GioHang>().ToTable("GioHang");
            modelBuilder.Entity<ChiTietGioHang>().ToTable("ChiTietGioHang");
            modelBuilder.Entity<HoaDonSanPham>().ToTable("HoaDonSanPham");
            modelBuilder.Entity<ChiTietHoaDonSanPham>().ToTable("ChiTietHoaDonSanPham");
            modelBuilder.Entity<VoucherSanPham>().ToTable("VoucherSanPham");
            modelBuilder.Entity<HoaDonSanPhamVoucher>().ToTable("HoaDonSanPham_Voucher");
            modelBuilder.Entity<YeuCauHoanTra>().ToTable("YeuCauHoanTra");

            // Configure TaiKhoan relationships with explicit foreign keys
            modelBuilder.Entity<TaiKhoan>()
                .HasOne(t => t.NhanVien)
                .WithMany(n => n.TaiKhoans)
                .HasForeignKey(t => t.MaNhanVien)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaiKhoan>()
                .HasOne(t => t.KhachHang)
                .WithMany(k => k.TaiKhoans)
                .HasForeignKey(t => t.MaKhachHang)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure TinNhan relationships
            modelBuilder.Entity<TinNhan>()
                .HasOne(t => t.KhachHang)
                .WithMany(k => k.TinNhans)
                .HasForeignKey(t => t.MaKhachHang)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure relationships to prevent cascading deletes where appropriate
            modelBuilder.Entity<Ve>()
                .HasOne(v => v.PhongChieu)
                .WithMany(p => p.Ves)
                .HasForeignKey(v => v.MaPhong)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Ve>()
                .HasOne(v => v.Phim)
                .WithMany(p => p.Ves)
                .HasForeignKey(v => v.MaPhim)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure composite key for HoaDonSanPhamVoucher
            modelBuilder.Entity<HoaDonSanPhamVoucher>()
                .HasKey(h => new { h.MaHoaDonSanPham, h.MaVoucherSanPham });

            // Configure product relationships
            modelBuilder.Entity<SanPham>()
                .HasOne(s => s.NhanVien)
                .WithMany()
                .HasForeignKey(s => s.MaNhanVien)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<GioHang>()
                .HasOne(g => g.KhachHang)
                .WithMany(k => k.GioHangs)
                .HasForeignKey(g => g.MaKhachHang)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChiTietGioHang>()
                .HasOne(c => c.GioHang)
                .WithMany(g => g.ChiTietGioHangs)
                .HasForeignKey(c => c.MaGioHang)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChiTietGioHang>()
                .HasOne(c => c.SanPham)
                .WithMany(s => s.ChiTietGioHangs)
                .HasForeignKey(c => c.MaSanPham)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<HoaDonSanPham>()
                .HasOne(h => h.KhachHang)
                .WithMany(k => k.HoaDonSanPhams)
                .HasForeignKey(h => h.MaKhachHang)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChiTietHoaDonSanPham>()
                .HasOne(c => c.HoaDonSanPham)
                .WithMany(h => h.ChiTietHoaDonSanPhams)
                .HasForeignKey(c => c.MaHoaDonSanPham)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChiTietHoaDonSanPham>()
                .HasOne(c => c.SanPham)
                .WithMany(s => s.ChiTietHoaDonSanPhams)
                .HasForeignKey(c => c.MaSanPham)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<YeuCauHoanTra>()
                .HasOne(y => y.HoaDonSanPham)
                .WithMany(h => h.YeuCauHoanTras)
                .HasForeignKey(y => y.MaHoaDonSanPham)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<YeuCauHoanTra>()
                .HasOne(y => y.KhachHang)
                .WithMany(k => k.YeuCauHoanTras)
                .HasForeignKey(y => y.MaKhachHang)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
