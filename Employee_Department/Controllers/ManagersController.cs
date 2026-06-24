using Employee_Department.Models;
using Employee_Department.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Employee_Department.Controllers
{
    [Authorize]
    public class ManagersController : Controller
    {
        private readonly IManagerService _managerService;
        private readonly IDepartmentService _departmentService;

        public ManagersController(IManagerService managerService, IDepartmentService departmentService)
        {
            _managerService = managerService;
            _departmentService = departmentService;
        }

        public async Task<IActionResult> Index(string? search)
        {
            ViewBag.Managers = await _managerService.GetFilteredAsync(search);
            ViewBag.Search = search;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRows(string? search)
        {
            var managers = await _managerService.GetFilteredAsync(search);
            return PartialView("_ManagerRows", managers);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var m = await _managerService.GetByIdAsync(id.Value);
            if (m == null) return NotFound();
            return View(m);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await PopulateDepartmentsAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("FullName,NationalId,DepartmentId,PhoneNumber,Email")] Manager manager)
        {
            var result = await _managerService.CreateAsync(manager);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", result.Message);
            await PopulateDepartmentsAsync(manager.DepartmentId);
            return View(manager);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var m = await _managerService.GetByIdAsync(id.Value);
            if (m == null) return NotFound();
            await PopulateDepartmentsAsync(m.DepartmentId);
            return View(m);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("ManagerId,FullName,NationalId,DepartmentId,PhoneNumber,Email")] Manager manager)
        {
            if (id != manager.ManagerId) return NotFound();
            var result = await _managerService.UpdateAsync(manager);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", result.Message);
            await PopulateDepartmentsAsync(manager.DepartmentId);
            return View(manager);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _managerService.DeleteAsync(id);
            return Json(new { success = result.Success, message = result.Message });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportExcel()
        {
            var bytes = await _managerService.ExportExcelAsync();
            var fileName = $"หัวหน้าแผนก_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private async Task PopulateDepartmentsAsync(int? selectedDeptId = null)
        {
            var depts = await _departmentService.GetAllAsync();
            ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName", selectedDeptId);
        }
    }
}