using Employee_Department.Data;
using Employee_Department.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Employee_Department.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        //ตรวจสอบว่า Request ปัจจุบันถูกส่งมาจาก Ajax หรือไม่
        private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        // GET: Departments
        public async Task<IActionResult> Index()
        {
            var data = await GetDepartmentDataAsync();
            ViewBag.DeptData = data;
            return View();
        }

        // GET: Departments/GetRows — AJAX
        [HttpGet]
        public async Task<IActionResult> GetRows()
        {
            var data = await GetDepartmentDataAsync();
            return PartialView("_DepartmentRows", data);
        }

        private async Task<List<dynamic>> GetDepartmentDataAsync()
        {
            return await _context.Departments
                .Select(d => new
                {
                    Department = d,
                    EmployeeCount = _context.Employees.Count(e => e.DepartmentId == d.DepartmentId),
                    ManagerCount = _context.Managers.Count(m => m.DepartmentId == d.DepartmentId),
                    HasManager = _context.Managers.Any(m => m.DepartmentId == d.DepartmentId)
                })
                .OrderBy(x => x.Department.DepartmentId)
                .ToListAsync<dynamic>();
        }

        // GET: Departments/Create
        public IActionResult Create() => View(new Department());

        // POST: Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DepartmentName")] Department department)
        {
            if (string.IsNullOrWhiteSpace(department.DepartmentName))
            {
                if (IsAjax) return Json(new { success = false, message = "กรุณาระบุชื่อแผนก" });
                ModelState.AddModelError("DepartmentName", "กรุณาระบุชื่อแผนก");
                return View(department);
            }

            if (await _context.Departments.AnyAsync(d => d.DepartmentName == department.DepartmentName))
            {
                if (IsAjax) return Json(new { success = false, message = "ชื่อแผนกนี้มีอยู่แล้ว" });
                ModelState.AddModelError("DepartmentName", "ชื่อแผนกนี้มีอยู่แล้ว");
                return View(department);
            }

            // เช็ค trash
            var existingDeleted = await _context.Departments
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.DepartmentName == department.DepartmentName && d.IsDeleted);

            string msg;
            if (existingDeleted != null)
            {
                existingDeleted.IsDeleted = false;
                existingDeleted.DeletedAt = null;
                await _context.SaveChangesAsync();
                msg = $"กู้คืนแผนก \"{existingDeleted.DepartmentName}\" จากถังขยะเรียบร้อย";
            }
            else
            {
                //Add more
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();
                msg = $"เพิ่มแผนก \"{department.DepartmentName}\" เรียบร้อยแล้ว";
            }

            // แยกการตอบกลับระหว่าง Ajax Request กับ Request ปกติ
            if (IsAjax) return Json(new { success = true, message = msg });
            TempData["Success"] = msg;
            return RedirectToAction(nameof(Index));
        }

        // POST: Departments/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DepartmentId,DepartmentName")] Department department)
        {
            if (id != department.DepartmentId)
            {
                if (IsAjax) return Json(new { success = false, message = "ID ไม่ตรงกัน" });
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(department.DepartmentName))
            {
                if (IsAjax) return Json(new { success = false, message = "กรุณาระบุชื่อแผนก" });
                ModelState.AddModelError("DepartmentName", "กรุณาระบุชื่อแผนก");
                return View(department);
            }

            if (await _context.Departments.AnyAsync(d => d.DepartmentName == department.DepartmentName && d.DepartmentId != id))
            {
                if (IsAjax) return Json(new { success = false, message = "ชื่อแผนกนี้มีอยู่แล้ว" });
                ModelState.AddModelError("DepartmentName", "ชื่อแผนกนี้มีอยู่แล้ว");
                return View(department);
            }

            _context.Update(department);
            await _context.SaveChangesAsync();

            if (IsAjax) return Json(new { success = true, message = "แก้ไขชื่อแผนกเรียบร้อยแล้ว" });
            TempData["Success"] = "แก้ไขชื่อแผนกเรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        // POST: Departments/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null)
            {
                if (IsAjax) return Json(new { success = false, message = "ไม่พบแผนกนี้" });
                return NotFound();
            }

            var empCount = await _context.Employees.CountAsync(e => e.DepartmentId == id);
            var hasManager = await _context.Managers.AnyAsync(m => m.DepartmentId == id);

            if (empCount > 0 || hasManager)
            {
                var errMsg = $"ลบไม่ได้: แผนก \"{dept.DepartmentName}\" ยังมีพนักงาน {empCount} คน" +
                             (hasManager ? " และมีหัวหน้าอยู่" : "");
                if (IsAjax) return Json(new { success = false, message = errMsg });
                TempData["Error"] = errMsg;
                return RedirectToAction(nameof(Index));
            }

            dept.IsDeleted = true;
            dept.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var sucMsg = $"ลบแผนก \"{dept.DepartmentName}\" เรียบร้อยแล้ว";
            if (IsAjax) return Json(new { success = true, message = sucMsg });
            TempData["Success"] = sucMsg;
            return RedirectToAction(nameof(Index));
        }

        // GET: Departments/GetOne/5 — สำหรับ AJAX edit dialog
        [HttpGet]
        public async Task<IActionResult> GetOne(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return Json(new { success = false, message = "ไม่พบ" });
            return Json(new { success = true, data = new { dept.DepartmentId, dept.DepartmentName } });
        }
    }
}