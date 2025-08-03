using CinemaManagement.Models;
using System.ComponentModel.DataAnnotations;

namespace CinemaManagement.ViewModels
{
    // ViewModel cho quản lý sản phẩm
    public class QuanLySanPhamViewModel
    {
        public List<SanPham> SanPhams { get; set; } = new List<SanPham>();
        public int TongSoSanPham { get; set; }
        public int SanPhamConHang { get; set; }
        public int SanPhamHetHang { get; set; }
        public decimal TongGiaTriTonKho { get; set; }
        
        // Filters
        public string? TenSanPham { get; set; }
        public string? TrangThai { get; set; }
        public decimal? GiaMin { get; set; }
        public decimal? GiaMax { get; set; }
    }

    // ViewModel cho tạo/sửa sản phẩm
    public class CreateSanPhamViewModel
    {
        [Required(ErrorMessage = "Mã sản phẩm là bắt buộc")]
        [StringLength(50)]
        public string MaSanPham { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(255)]
        public string TenSanPham { get; set; } = string.Empty;

        public string? MoTa { get; set; }

        [Required(ErrorMessage = "Giá là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Gia { get; set; }

        [Required(ErrorMessage = "Số lượng tồn là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn phải lớn hơn hoặc bằng 0")]
        public int SoLuongTon { get; set; }

        public string? HinhAnh { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public string TrangThai { get; set; } = "Còn hàng";

        public string MaNhanVien { get; set; } = string.Empty;
    }

    // ViewModel cho chi tiết sản phẩm
    public class ChiTietSanPhamViewModel
    {
        public SanPham SanPham { get; set; } = new SanPham();
        public List<ChiTietHoaDonSanPham> LichSuBanHang { get; set; } = new List<ChiTietHoaDonSanPham>();
        public int SoLuongDaBan { get; set; }
        public decimal DoanhThuTuSanPham { get; set; }
    }

    // ViewModel cho trang mua sắm của khách hàng
    public class ShoppingViewModel
    {
        public List<SanPham> SanPhams { get; set; } = new List<SanPham>();
        public List<SanPham> SanPhamMoi { get; set; } = new List<SanPham>();
        public List<SanPham> SanPhamBanChay { get; set; } = new List<SanPham>();
        public List<string> DanhMucSanPham { get; set; } = new List<string>();
        
        // Filters
        public string? SearchTerm { get; set; }
        public string? DanhMuc { get; set; }
        public decimal? GiaMin { get; set; }
        public decimal? GiaMax { get; set; }
        public string? SortBy { get; set; }
    }

    // ViewModel cho giỏ hàng
    public class GioHangViewModel
    {
        public GioHang GioHang { get; set; } = new GioHang();
        public List<GioHangItemViewModel> Items { get; set; } = new List<GioHangItemViewModel>();
        public decimal TongTien { get; set; }
        public int TongSoLuong { get; set; }
        public List<VoucherSanPham> VouchersApDung { get; set; } = new List<VoucherSanPham>();
    }

    public class GioHangItemViewModel
    {
        public ChiTietGioHang ChiTiet { get; set; } = new ChiTietGioHang();
        public SanPham SanPham { get; set; } = new SanPham();
        public decimal ThanhTien => ChiTiet.SoLuong * ChiTiet.DonGia;
    }

    // ViewModel cho thanh toán
    public class ThanhToanSanPhamViewModel
    {
        public GioHangViewModel GioHang { get; set; } = new GioHangViewModel();
        public KhachHang KhachHang { get; set; } = new KhachHang();
        
        [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc")]
        public string DiaChiGiaoHang { get; set; } = string.Empty;
        
        public string? GhiChu { get; set; }
        public string? MaVoucher { get; set; }
        public decimal TienGiam { get; set; }
        public decimal TongTienSauGiam { get; set; }
    }

    // ViewModel cho lịch sử đơn hàng
    public class LichSuDonHangViewModel
    {
        public List<HoaDonSanPhamViewModel> DonHangs { get; set; } = new List<HoaDonSanPhamViewModel>();
        public string? TrangThaiFilter { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
    }

    public class HoaDonSanPhamViewModel
    {
        public HoaDonSanPham HoaDon { get; set; } = new HoaDonSanPham();
        public List<HoaDonSanPhamItemViewModel> Items { get; set; } = new List<HoaDonSanPhamItemViewModel>();
        public decimal TongTienTruocGiam { get; set; }
        public decimal TienGiam { get; set; }
        public List<VoucherSanPham> VouchersApDung { get; set; } = new List<VoucherSanPham>();
    }

    public class HoaDonSanPhamItemViewModel
    {
        public ChiTietHoaDonSanPham ChiTiet { get; set; } = new ChiTietHoaDonSanPham();
        public SanPham SanPham { get; set; } = new SanPham();
        public decimal ThanhTien => ChiTiet.SoLuong * ChiTiet.DonGia;
    }

    // ViewModel cho thêm sản phẩm vào giỏ hàng
    public class ThemVaoGioHangRequest
    {
        public string MaSanPham { get; set; } = string.Empty;
        public int SoLuong { get; set; } = 1;
    }

    // ViewModel cho cập nhật giỏ hàng
    public class CapNhatGioHangRequest
    {
        public string MaChiTietGioHang { get; set; } = string.Empty;
        public int SoLuong { get; set; }
    }

    // ViewModel cho voucher sản phẩm
    public class VoucherSanPhamViewModel
    {
        public List<VoucherSanPham> Vouchers { get; set; } = new List<VoucherSanPham>();
        public List<VoucherSanPham> VouchersApDung { get; set; } = new List<VoucherSanPham>();
        public string? SearchTerm { get; set; }
        public bool? IsExpired { get; set; }
    }

    public class CreateVoucherSanPhamViewModel
    {
        [Required(ErrorMessage = "Mã voucher là bắt buộc")]
        [StringLength(50)]
        public string MaVoucherSanPham { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên voucher là bắt buộc")]
        [StringLength(100)]
        public string TenVoucher { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phần trăm giảm là bắt buộc")]
        [Range(1, 100, ErrorMessage = "Phần trăm giảm từ 1% đến 100%")]
        public int PhanTramGiam { get; set; }

        [Required(ErrorMessage = "Giá trị giảm tối đa là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị giảm tối đa phải lớn hơn 0")]
        public decimal GiaTriGiamToiDa { get; set; }

        public string? MoTa { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public DateTime ThoiGianBatDau { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public DateTime ThoiGianKetThuc { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int SoLuong { get; set; }

        public string? DieuKienApDung { get; set; }
    }
} 