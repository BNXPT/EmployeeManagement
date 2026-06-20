using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Department.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Employees");
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View();
        }
    }
}
