using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaManagement.Data;
using CinemaManagement.Models;
using CinemaManagement.ViewModels;

namespace CinemaManagement.Controllers
{
    public class AdminSupportController : Controller
    {
        private readonly CinemaDbContext _context;
        private readonly ILogger<AdminSupportController> _logger;

        public AdminSupportController(CinemaDbContext context, ILogger<AdminSupportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Kiểm tra quyền admin
        private bool IsAdminLoggedIn()
        {
            var role = HttpContext.Session.GetString("VaiTro");
            var maNhanVien = HttpContext.Session.GetString("MaNhanVien");
            return role == "Quản lý" && !string.IsNullOrEmpty(maNhanVien);
        }

        public async Task<IActionResult> Index(string? searchTerm, string? trangThai, int page = 1, int pageSize = 20)
        {
            if (!IsAdminLoggedIn())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập chức năng này";
                return RedirectToAction("Login", "Auth");
            }

            var query = _context.TinNhans
                .Include(t => t.KhachHang)
                .AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(t => 
                    t.KhachHang!.HoTen.Contains(searchTerm) ||
                    t.NoiDung!.Contains(searchTerm) ||
                    t.NoiDungTraNoi!.Contains(searchTerm));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "Chưa trả lời")
                {
                    query = query.Where(t => string.IsNullOrEmpty(t.NoiDungTraNoi));
                }
                else if (trangThai == "Đã trả lời")
                {
                    query = query.Where(t => !string.IsNullOrEmpty(t.NoiDungTraNoi));
                }
                else
                {
                    query = query.Where(t => t.TrangThai == trangThai);
                }
            }

            var totalCount = await query.CountAsync();
            var messages = await query
                .OrderByDescending(t => t.ThoiGianGui)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AdminSupportViewModel
            {
                Messages = messages,
                SearchTerm = searchTerm,
                TrangThai = trangThai,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ChatDetail(string maKhachHang)
        {
            if (!IsAdminLoggedIn())
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            var khachHang = await _context.KhachHangs.FindAsync(maKhachHang);
            if (khachHang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khách hàng" });
            }

            var messages = await _context.TinNhans
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

            return Json(new { 
                success = true, 
                khachHang = new { 
                    maKhachHang = khachHang.MaKhachHang,
                    hoTen = khachHang.HoTen,
                    sdt = khachHang.SDT
                },
                messages = messages 
            });
        }

        [HttpPost]
        public async Task<IActionResult> SendReply(string maTinNhan, string noiDungTraNoi)
        {
            try
            {
                if (!IsAdminLoggedIn())
                {
                    return Json(new { success = false, message = "Không có quyền truy cập" });
                }

                var tinNhan = await _context.TinNhans.FindAsync(maTinNhan);

                if (tinNhan == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tin nhắn" });
                }

                if (string.IsNullOrWhiteSpace(noiDungTraNoi))
                {
                    return Json(new { success = false, message = "Vui lòng nhập nội dung phản hồi" });
                }

                // Cập nhật phản hồi
                tinNhan.NoiDungTraNoi = noiDungTraNoi.Trim();
                tinNhan.TrangThai = "Đã trả lời";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Reply sent to message {MessageId}", maTinNhan);

                return Json(new { 
                    success = true, 
                    message = "Phản hồi đã được gửi thành công",
                    data = new {
                        noiDungTraNoi = tinNhan.NoiDungTraNoi
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reply to message: {MessageId}", maTinNhan);
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi phản hồi" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsProcessed(string maTinNhan)
        {
            try
            {
                if (!IsAdminLoggedIn())
                {
                    return Json(new { success = false, message = "Không có quyền truy cập" });
                }

                var tinNhan = await _context.TinNhans.FindAsync(maTinNhan);

                if (tinNhan == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tin nhắn" });
                }

                tinNhan.TrangThai = "Đã xử lý";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Message {MessageId} marked as processed", maTinNhan);

                return Json(new { 
                    success = true, 
                    message = "Đã đánh dấu tin nhắn là đã xử lý"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {MessageId}", maTinNhan);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý tin nhắn" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(string maTinNhan)
        {
            try
            {
                if (!IsAdminLoggedIn())
                {
                    return Json(new { success = false, message = "Không có quyền truy cập" });
                }

                var tinNhan = await _context.TinNhans.FindAsync(maTinNhan);
                if (tinNhan == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tin nhắn" });
                }

                tinNhan.TrangThai = "Đã đọc";
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã đánh dấu là đã đọc" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read: {MessageId}", maTinNhan);
                return Json(new { success = false, message = "Có lỗi xảy ra" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            if (!IsAdminLoggedIn())
            {
                return Json(new { success = false, message = "Không có quyền truy cập" });
            }

            var today = DateTime.Today;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var stats = new
            {
                totalMessages = await _context.TinNhans.CountAsync(),
                totalCustomers = await _context.TinNhans.Select(t => t.MaKhachHang).Distinct().CountAsync(),
                unansweredMessages = await _context.TinNhans.CountAsync(t => string.IsNullOrEmpty(t.NoiDungTraNoi)),
                answeredMessages = await _context.TinNhans.CountAsync(t => !string.IsNullOrEmpty(t.NoiDungTraNoi)),
                todayMessages = await _context.TinNhans.CountAsync(t => t.ThoiGianGui.Date == today),
                weekMessages = await _context.TinNhans.CountAsync(t => t.ThoiGianGui >= thisWeek),
                monthMessages = await _context.TinNhans.CountAsync(t => t.ThoiGianGui >= thisMonth)
            };

            return Json(new { success = true, data = stats });
        }


    }
}

// ViewModel for Admin Support
namespace CinemaManagement.ViewModels
{
    public class AdminSupportViewModel
    {
        public List<TinNhan> Messages { get; set; } = new List<TinNhan>();
        public string? SearchTerm { get; set; }
        public string? TrangThai { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
} 