using Employee_Department.Models;
using Employee_Department.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Employee_Department.Controllers
{
    [Authorize]
    public class EmployeesController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly IPositionService _positionService;

        public EmployeesController(
            IEmployeeService employeeService,
            IDepartmentService departmentService,
            IPositionService positionService)
        {
            _employeeService = employeeService;
            _departmentService = departmentService;
            _positionService = positionService;
        }

        public async Task<IActionResult> Index(string? search, int? departmentId)
        {
            ViewBag.Employees = await _employeeService.GetFilteredAsync(search, departmentId);
            ViewBag.Departments = await _departmentService.GetAllAsync();
            ViewBag.Search = search;
            ViewBag.SelectedDepartmentId = departmentId;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRows(string? search, int? departmentId)
        {
            var employees = await _employeeService.GetFilteredAsync(search, departmentId);
            return PartialView("_EmployeeRows", employees);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var emp = await _employeeService.GetByIdAsync(id.Value);
            if (emp == null) return NotFound();
            return View(emp);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("FullName,NationalId,DepartmentId,Position,PhoneNumber,Email")] Employee employee)
        {
            var result = await _employeeService.CreateAsync(employee);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", result.Message);
            await PopulateDropdownsAsync(employee.DepartmentId);
            return View(employee);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var emp = await _employeeService.GetByIdAsync(id.Value);
            if (emp == null) return NotFound();
            await PopulateDropdownsAsync(emp.DepartmentId);
            return View(emp);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeId,FullName,NationalId,DepartmentId,Position,PhoneNumber,Email")] Employee employee)
        {
            if (id != employee.EmployeeId) return NotFound();
            var result = await _employeeService.UpdateAsync(employee);
            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", result.Message);
            await PopulateDropdownsAsync(employee.DepartmentId);
            return View(employee);
        }

        // POST: /Employees/Delete/5 — รับ id เลย ไม่ต้องมี token
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _employeeService.DeleteAsync(id);
            return Json(new { success = result.Success, message = result.Message });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportExcel()
        {
            var bytes = await _employeeService.ExportExcelAsync();
            var fileName = $"รายชื่อพนักงาน_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private async Task PopulateDropdownsAsync(int? selectedDeptId = null)
        {
            var depts = await _departmentService.GetAllAsync();
            ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName", selectedDeptId);
            var positions = await _positionService.GetAllAsync();
            ViewBag.Positions = positions.Select(p => p.PositionName).ToList();
        }
    }
}