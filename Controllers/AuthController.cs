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
    }
}
