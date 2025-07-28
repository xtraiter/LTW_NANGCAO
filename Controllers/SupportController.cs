using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.Services;
using CinemaManagement.Extensions;

namespace CinemaManagement.Controllers
{
    public class SupportController : Controller
    {
        private readonly CinemaDbContext _context;
        private readonly FileUploadService _fileUploadService;
        private readonly ILogger<SupportController> _logger;

        public SupportController(CinemaDbContext context, FileUploadService fileUploadService, ILogger<SupportController> logger)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
            if (string.IsNullOrEmpty(maKhachHang))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để sử dụng chức năng hỗ trợ";
                return RedirectToAction("Login", "Auth");
            }

            var khachHang = await _context.KhachHangs.FindAsync(maKhachHang);
            if (khachHang == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng";
                return RedirectToAction("Login", "Auth");
            }
            
            var tinNhans = await _context.TinNhans
                .Where(t => t.MaKhachHang == maKhachHang)
                .OrderBy(t => t.ThoiGianGui)
                .ToListAsync();

            ViewBag.MaKhachHang = maKhachHang;
            ViewBag.TenKhachHang = khachHang.HoTen;

            return View(tinNhans);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string noiDung, IFormFile? hinhAnh)
        {
            try
            {
                var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
                if (string.IsNullOrEmpty(maKhachHang))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để gửi tin nhắn" });
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(noiDung) && hinhAnh == null)
                {
                    return Json(new { success = false, message = "Vui lòng nhập nội dung tin nhắn hoặc chọn hình ảnh" });
                }

                // Handle image upload
                string? imagePath = null;
                if (hinhAnh != null)
                {
                    var uploadResult = await _fileUploadService.UploadImageAsync(hinhAnh, "support");
                    if (!uploadResult.Success)
                    {
                        return Json(new { success = false, message = uploadResult.ErrorMessage });
                    }
                    imagePath = uploadResult.FilePath;
                }

                // Create new message
                var tinNhan = new TinNhan
                {
                    MaTinNhan = "TN" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999),
                    MaKhachHang = maKhachHang,
                    NoiDung = noiDung?.Trim(),
                    HinhAnh = imagePath,
                    ThoiGianGui = DateTime.Now,
                    TrangThai = "Đã gửi"
                };

                _context.TinNhans.Add(tinNhan);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New support message created: {MessageId} by customer {CustomerId}", tinNhan.MaTinNhan, maKhachHang);

                return Json(new { 
                    success = true, 
                    message = "Tin nhắn đã được gửi thành công",
                    data = new {
                        maTinNhan = tinNhan.MaTinNhan,
                        noiDung = tinNhan.NoiDung,
                        hinhAnh = tinNhan.HinhAnh,
                        thoiGianGui = tinNhan.ThoiGianGui.ToString("dd/MM/yyyy HH:mm"),
                        trangThai = tinNhan.TrangThai
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending support message");
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi tin nhắn" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                var uploadResult = await _fileUploadService.UploadImageAsync(file, "support");
                
                if (uploadResult.Success)
                {
                    return Json(new { 
                        success = true, 
                        filePath = uploadResult.FilePath,
                        fileName = file.FileName
                    });
                }
                else
                {
                    return Json(new { success = false, message = uploadResult.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return Json(new { success = false, message = "Có lỗi xảy ra khi upload hình ảnh" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages()
        {
            try
            {
                var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
                if (string.IsNullOrEmpty(maKhachHang))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }
                
                var tinNhans = await _context.TinNhans
                    .Where(t => t.MaKhachHang == maKhachHang)
                    .OrderBy(t => t.ThoiGianGui)
                    .Select(t => new {
                        maTinNhan = t.MaTinNhan,
                        noiDung = t.NoiDung,
                        hinhAnh = t.HinhAnh,
                        thoiGianGui = t.ThoiGianGui.ToString("dd/MM/yyyy HH:mm"),
                        trangThai = t.TrangThai,
                        noiDungTraNoi = t.NoiDungTraNoi
                    })
                    .ToListAsync();

                return Json(new { success = true, data = tinNhans });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải tin nhắn" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage(string maTinNhan)
        {
            try
            {
                var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
                if (string.IsNullOrEmpty(maKhachHang))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }
                
                var tinNhan = await _context.TinNhans
                    .FirstOrDefaultAsync(t => t.MaTinNhan == maTinNhan && t.MaKhachHang == maKhachHang);

                if (tinNhan == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tin nhắn" });
                }

                // Delete associated image if exists
                if (!string.IsNullOrEmpty(tinNhan.HinhAnh))
                {
                    _fileUploadService.DeleteFile(tinNhan.HinhAnh);
                }

                _context.TinNhans.Remove(tinNhan);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xóa tin nhắn thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message: {MessageId}", maTinNhan);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa tin nhắn" });
            }
        }
    }
} 