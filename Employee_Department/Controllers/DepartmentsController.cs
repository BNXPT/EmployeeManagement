using Employee_Department.Models;
using Employee_Department.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Department.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentsController : Controller
    {
        private readonly IDepartmentService _service;
        public DepartmentsController(IDepartmentService service) => _service = service;

        public async Task<IActionResult> Index()
        {
            //ใช้ method ที่มี count
            ViewBag.DeptData = await _service.GetAllWithCountsAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            //ส่ง JSON พร้อม count
            var data = await _service.GetAllWithCountsAsync();
            return Json(new { success = true, data });
        }

        [HttpGet]
        public async Task<IActionResult> GetRows()
        {
            var data = await _service.GetAllWithCountsAsync();
            return PartialView("_DepartmentRows", data);
        }

        [HttpGet]
        public async Task<IActionResult> GetOne(int id)
        {
            var d = await _service.GetByIdAsync(id);
            return d == null
                ? Json(new { success = false, message = "ไม่พบ" })
                : Json(new { success = true, data = new { d.DepartmentId, d.DepartmentName } });
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("DepartmentName")] Department department)
        {
            var result = await _service.CreateAsync(department);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("DepartmentId,DepartmentName")] Department department)
        {
            if (id != department.DepartmentId) return Json(new { success = false, message = "ID ไม่ตรง" });
            var result = await _service.UpdateAsync(department);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}