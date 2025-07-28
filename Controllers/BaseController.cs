using Microsoft.AspNetCore.Mvc;

namespace CinemaManagement.Controllers
{
    public class BaseController : Controller
    {
        protected bool IsEmployeeLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("MaNhanVien"));
        }

        protected bool IsCustomerLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("MaKhachHang"));
        }

        protected bool IsManagerRole()
        {
            return HttpContext.Session.GetString("VaiTro") == "Quản lý";
        }

        protected string? GetCurrentEmployeeId()
        {
            return HttpContext.Session.GetString("MaNhanVien");
        }

        protected string? GetCurrentCustomerId()
        {
            return HttpContext.Session.GetString("MaKhachHang");
        }
    }
}