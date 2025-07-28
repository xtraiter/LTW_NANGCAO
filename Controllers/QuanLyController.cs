using Microsoft.AspNetCore.Mvc;

namespace CinemaManagement.Controllers
{
    public class QuanLyController : Controller
    {
        private bool IsManagerOrStaff()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            return vaiTro == "Quản lý" || vaiTro == "Nhân viên";
        }

        public IActionResult Index(DateTime? tuNgay, DateTime? denNgay, string? tenPhim)
        {
            if (!IsManagerOrStaff())
            {
                return RedirectToAction("Index", "Home");
            }

            // Redirect tới Dashboard controller
            return RedirectToAction("Index", "Dashboard", new { tuNgay, denNgay, tenPhim });
        }

        // Redirect actions cho backward compatibility
        public IActionResult ThongKeChiTiet(DateTime? tuNgay, DateTime? denNgay, string? tenPhim)
        {
            return RedirectToAction("ChiTiet", "ThongKe", new { tuNgay, denNgay, tenPhim });
        }

        public IActionResult QuanLyPhim()
        {
            return RedirectToAction("Index", "Phim");
        }

        public IActionResult QuanLyLichChieu()
        {
            return RedirectToAction("Index", "LichChieu");
        }

        public IActionResult QuanLyNhanVien()
        {
            return RedirectToAction("Index", "NhanVienManagement");
        }

        public IActionResult BaoCao()
        {
            return RedirectToAction("BaoCao", "ThongKe");
        }

        public IActionResult QuanLyHoaDon()
        {
            return RedirectToAction("Index", "QuanLyHoaDon");
        }

        // API actions redirect for backward compatibility
        [HttpPost]
        public IActionResult ThemPhim(string tenPhim, string theLoai, int thoiLuong, string doTuoiPhanAnh, string moTa, string viTriFilePhim)
        {
            return RedirectToAction("ThemPhim", "Phim", new { tenPhim, theLoai, thoiLuong, doTuoiPhanAnh, moTa, viTriFilePhim });
        }

        [HttpPost]
        public IActionResult XoaPhim(string maPhim)
        {
            return RedirectToAction("XoaPhim", "Phim", new { maPhim });
        }

        [HttpGet]
        public IActionResult ChiTietPhim(string maPhim)
        {
            return RedirectToAction("ChiTietPhim", "Phim", new { maPhim });
        }

        [HttpGet] 
        public IActionResult GetDoanhThuData(string type = "day", int days = 7)
        {
            return RedirectToAction("GetDoanhThuData", "Dashboard", new { type, days });
        }
    }
}
