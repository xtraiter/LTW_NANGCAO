using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaManagement.Data;
using CinemaManagement.Models;

namespace CinemaManagement.Controllers
{
    public class AuthController : Controller
    {
        private readonly CinemaDbContext _context;

        public AuthController(CinemaDbContext context)
        {
            _context = context;
        }

        // GET: Auth/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            var taiKhoan = await _context.TaiKhoans
                .Include(t => t.NhanVien)
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.Email == email && t.MatKhau == password);

            if (taiKhoan == null || taiKhoan.TrangThai != "Hoạt động")
            {
                ViewBag.ErrorMessage = "Email hoặc mật khẩu không đúng";
                return View();
            }

            // Lưu thông tin vào session
            HttpContext.Session.SetString("MaTK", taiKhoan.MaTK);
            HttpContext.Session.SetString("Email", taiKhoan.Email);
            HttpContext.Session.SetString("Role", taiKhoan.Role);
            HttpContext.Session.SetString("VaiTro", taiKhoan.Role); // Thêm để tương thích

            if (taiKhoan.NhanVien != null)
            {
                HttpContext.Session.SetString("MaNhanVien", taiKhoan.NhanVien.MaNhanVien);
                HttpContext.Session.SetString("TenNhanVien", taiKhoan.NhanVien.TenNhanVien);
                HttpContext.Session.SetString("ChucVu", taiKhoan.NhanVien.ChucVu);
            }

            if (taiKhoan.KhachHang != null)
            {
                HttpContext.Session.SetString("MaKhachHang", taiKhoan.KhachHang.MaKhachHang);
                HttpContext.Session.SetString("TenKhachHang", taiKhoan.KhachHang.HoTen);
            }

            // Redirect based on role
            if (taiKhoan.Role == "Quản lý")
            {
                return RedirectToAction("Index", "QuanLy"); // Chuyển đến dashboard quản lý
            }
            else if (taiKhoan.Role == "Nhân viên")
            {
                return RedirectToAction("Index", "BanVe"); // Nhân viên chỉ đi đến bán vé
            }
            else
            {
                return RedirectToAction("Index", "Home"); // Khách hàng đi đến trang chủ
            }
        }

        // GET: Auth/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // GET: Auth/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Auth/Register
        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword, string hoTen, string sdt)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || 
                string.IsNullOrEmpty(confirmPassword) || string.IsNullOrEmpty(hoTen))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu xác nhận không khớp";
                return View();
            }

            // Kiểm tra email đã tồn tại
            var existingAccount = await _context.TaiKhoans
                .FirstOrDefaultAsync(t => t.Email == email);

            if (existingAccount != null)
            {
                ViewBag.ErrorMessage = "Email đã được sử dụng";
                return View();
            }

            try
            {
                // Tạo mã khách hàng mới
                var lastCustomer = await _context.KhachHangs
                    .OrderByDescending(k => k.MaKhachHang)
                    .FirstOrDefaultAsync();

                string maKhachHang;
                if (lastCustomer != null && lastCustomer.MaKhachHang.StartsWith("KH"))
                {
                    var lastNumber = int.Parse(lastCustomer.MaKhachHang.Substring(2));
                    maKhachHang = "KH" + (lastNumber + 1).ToString("D3");
                }
                else
                {
                    maKhachHang = "KH001";
                }

                // Tạo khách hàng mới
                var khachHang = new KhachHang
                {
                    MaKhachHang = maKhachHang,
                    HoTen = hoTen,
                    SDT = sdt ?? string.Empty,
                    DiemTichLuy = 0
                };

                _context.KhachHangs.Add(khachHang);

                // Tạo mã tài khoản mới
                var lastAccount = await _context.TaiKhoans
                    .OrderByDescending(t => t.MaTK)
                    .FirstOrDefaultAsync();

                string maTK;
                if (lastAccount != null && lastAccount.MaTK.StartsWith("TK"))
                {
                    var lastNumber = int.Parse(lastAccount.MaTK.Substring(2));
                    maTK = "TK" + (lastNumber + 1).ToString("D3");
                }
                else
                {
                    maTK = "TK001";
                }

                // Tạo tài khoản mới
                var taiKhoan = new TaiKhoan
                {
                    MaTK = maTK,
                    Email = email,
                    MatKhau = password, // Trong thực tế nên hash password
                    Role = "Khách hàng",
                    TrangThai = "Hoạt động",
                    MaNhanVien = string.Empty,
                    MaKhachHang = maKhachHang
                };

                _context.TaiKhoans.Add(taiKhoan);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra: " + ex.Message;
                return View();
            }
        }
    }
}
