using Microsoft.AspNetCore.Mvc;
using CinemaManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace CinemaManagement.Controllers
{
    public class NhanVienManagementController : Controller
    {
        private readonly CinemaDbContext _context;

        public NhanVienManagementController(CinemaDbContext context)
        {
            _context = context;
        }

        private bool IsManager()
        {
            var vaiTro = HttpContext.Session.GetString("VaiTro");
            return vaiTro == "Quản lý";
        }

        public async Task<IActionResult> Index()
        {
            if (!IsManager())
            {
                return RedirectToAction("Index", "Home");
            }

            var nhanViens = await _context.NhanViens
                .OrderBy(n => n.TenNhanVien)
                .ToListAsync();

            return View(nhanViens);
        }
    }
}
