using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;
using CinemaManagement.Extensions;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace CinemaManagement.Controllers
{
    public class AuthController : Controller
    {
        private readonly CinemaDbContext _context;

        public AuthController(CinemaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập, chuyển về trang chính
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Email")))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Tìm tài khoản theo email
                var taiKhoan = await _context.TaiKhoans
                    .Include(t => t.NhanVien)
                    .Include(t => t.KhachHang)
                    .FirstOrDefaultAsync(t => t.Email == model.Email);

                if (taiKhoan == null)
                {
                    ViewBag.ErrorMessage = "Email hoặc mật khẩu không đúng";
                    return View(model);
                }

                // Kiểm tra mật khẩu (giả sử mật khẩu được hash)
                if (!VerifyPassword(model.Password, taiKhoan.MatKhau))
                {
                    ViewBag.ErrorMessage = "Email hoặc mật khẩu không đúng";
                    return View(model);
                }

                // Kiểm tra trạng thái tài khoản
                if (taiKhoan.TrangThai != "Hoạt động")
                {
                    ViewBag.ErrorMessage = "Tài khoản đã bị khóa";
                    return View(model);
                }

                // Lưu thông tin vào session
                HttpContext.Session.SetString("MaTK", taiKhoan.MaTK);
                HttpContext.Session.SetString("Email", taiKhoan.Email);
                HttpContext.Session.SetString("Role", taiKhoan.Role);

                if (taiKhoan.Role == "Nhân viên" || taiKhoan.Role == "Quản lý")
                {
                    if (taiKhoan.NhanVien != null)
                    {
                        HttpContext.Session.SetString("MaNhanVien", taiKhoan.MaNhanVien!);
                        HttpContext.Session.SetString("TenNhanVien", taiKhoan.NhanVien.TenNhanVien);
                        HttpContext.Session.SetString("VaiTro", taiKhoan.NhanVien.ChucVu);
                    }
                }
                else if (taiKhoan.Role == "Khách hàng")
                {
                    if (taiKhoan.KhachHang != null)
                    {
                        HttpContext.Session.SetString("MaKhachHang", taiKhoan.MaKhachHang!);
                        HttpContext.Session.SetString("TenKhachHang", taiKhoan.KhachHang.HoTen);
                        HttpContext.Session.SetString("VaiTro", "Khách hàng");
                    }
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra: " + ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Kiểm tra email đã tồn tại
                var existingAccount = await _context.TaiKhoans
                    .FirstOrDefaultAsync(t => t.Email == model.Email);

                if (existingAccount != null)
                {
                    ViewBag.ErrorMessage = "Email đã được sử dụng";
                    return View(model);
                }

                // Tạo mã khách hàng mới
                var lastCustomer = await _context.KhachHangs
                    .OrderByDescending(k => k.MaKhachHang)
                    .FirstOrDefaultAsync();

                string newCustomerId = "KH001";
                if (lastCustomer != null)
                {
                    int lastId = int.Parse(lastCustomer.MaKhachHang.Substring(2));
                    newCustomerId = $"KH{(lastId + 1):D3}";
                }

                // Tạo mã tài khoản mới
                var lastAccount = await _context.TaiKhoans
                    .OrderByDescending(t => t.MaTK)
                    .FirstOrDefaultAsync();

                string newAccountId = "TK001";
                if (lastAccount != null)
                {
                    int lastId = int.Parse(lastAccount.MaTK.Substring(2));
                    newAccountId = $"TK{(lastId + 1):D3}";
                }

                // Tạo khách hàng mới
                var khachHang = new KhachHang
                {
                    MaKhachHang = newCustomerId,
                    HoTen = model.HoTen,
                    SDT = model.SDT,
                    DiemTichLuy = 0
                };

                // Tạo tài khoản mới
                var taiKhoan = new TaiKhoan
                {
                    MaTK = newAccountId,
                    Email = model.Email,
                    MatKhau = HashPassword(model.Password),
                    Role = "Khách hàng",
                    TrangThai = "Hoạt động",
                    MaKhachHang = newCustomerId
                };

                _context.KhachHangs.Add(khachHang);
                _context.TaiKhoans.Add(taiKhoan);
                await _context.SaveChangesAsync();

                ViewBag.SuccessMessage = "Đăng ký thành công! Vui lòng đăng nhập.";
                return View("Login");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra: " + ex.Message;
                return View(model);
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginViewModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Name) || string.IsNullOrEmpty(model.GoogleId))
                {
                    return Json(new { success = false, message = "Thông tin Google không hợp lệ" });
                }

                // Kiểm tra xem tài khoản đã tồn tại chưa
                var existingAccount = await _context.TaiKhoans
                    .Include(t => t.KhachHang)
                    .FirstOrDefaultAsync(t => t.Email == model.Email);

                if (existingAccount != null)
                {
                    // Tài khoản đã tồn tại, đăng nhập
                    if (existingAccount.TrangThai != "Hoạt động")
                    {
                        return Json(new { success = false, message = "Tài khoản đã bị khóa" });
                    }

                    // Lưu thông tin vào session
                    HttpContext.Session.SetString("MaTK", existingAccount.MaTK);
                    HttpContext.Session.SetString("Email", existingAccount.Email);
                    HttpContext.Session.SetString("Role", existingAccount.Role);

                    if (existingAccount.Role == "Khách hàng" && existingAccount.KhachHang != null)
                    {
                        HttpContext.Session.SetString("MaKhachHang", existingAccount.MaKhachHang!);
                        HttpContext.Session.SetString("TenKhachHang", existingAccount.KhachHang.HoTen);
                        HttpContext.Session.SetString("VaiTro", "Khách hàng");
                    }

                    return Json(new { success = true, message = "Đăng nhập thành công" });
                }
                else
                {
                    // Tạo tài khoản mới cho khách hàng

                    // Tạo mã khách hàng mới
                    var lastCustomer = await _context.KhachHangs
                        .OrderByDescending(k => k.MaKhachHang)
                        .FirstOrDefaultAsync();

                    string newCustomerId = "KH001";
                    if (lastCustomer != null)
                    {
                        int lastId = int.Parse(lastCustomer.MaKhachHang.Substring(2));
                        newCustomerId = $"KH{(lastId + 1):D3}";
                    }

                    // Tạo mã tài khoản mới
                    var lastAccount = await _context.TaiKhoans
                        .OrderByDescending(t => t.MaTK)
                        .FirstOrDefaultAsync();

                    string newAccountId = "TK001";
                    if (lastAccount != null)
                    {
                        int lastId = int.Parse(lastAccount.MaTK.Substring(2));
                        newAccountId = $"TK{(lastId + 1):D3}";
                    }

                    // Tạo khách hàng mới
                    var khachHang = new KhachHang
                    {
                        MaKhachHang = newCustomerId,
                        HoTen = model.Name,
                        SDT = "", // Sẽ cập nhật sau
                        DiemTichLuy = 0
                    };

                    // Tạo tài khoản mới
                    var taiKhoan = new TaiKhoan
                    {
                        MaTK = newAccountId,
                        Email = model.Email,
                        MatKhau = HashPassword(model.GoogleId), // Sử dụng GoogleId làm password hash
                        Role = "Khách hàng",
                        TrangThai = "Hoạt động",
                        MaKhachHang = newCustomerId
                    };

                    _context.KhachHangs.Add(khachHang);
                    _context.TaiKhoans.Add(taiKhoan);
                    await _context.SaveChangesAsync();

                    // Lưu thông tin vào session
                    HttpContext.Session.SetString("MaTK", taiKhoan.MaTK);
                    HttpContext.Session.SetString("Email", taiKhoan.Email);
                    HttpContext.Session.SetString("Role", taiKhoan.Role);
                    HttpContext.Session.SetString("MaKhachHang", newCustomerId);
                    HttpContext.Session.SetString("TenKhachHang", khachHang.HoTen);
                    HttpContext.Session.SetString("VaiTro", "Khách hàng");

                    return Json(new { success = true, message = "Đăng ký và đăng nhập thành công" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        private string HashPassword(string password)
        {
            // Đây là implementation đơn giản, trong thực tế nên dùng bcrypt hoặc tương tự
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            // Kiểm tra nếu mật khẩu đã được hash
            if (hashedPassword.Length == 64) // SHA256 hash length
            {
                return HashPassword(password) == hashedPassword;
            }
            else
            {
                // Nếu là plain text (để test)
                return password == hashedPassword;
            }
        }
        
    }
}
