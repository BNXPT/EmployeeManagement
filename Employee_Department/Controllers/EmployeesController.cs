using Employee_Department.Data;
using Employee_Department.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace Employee_Department.Controllers
{
    [Authorize]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        // GET: Employees
        public async Task<IActionResult> Index(string? search, int? departmentId)
        {
            var query = _context.Employees.Include(e => e.Department).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.FullName.Contains(search)
                    || e.NationalId.Contains(search)
                    || e.PhoneNumber.Contains(search)
                    || e.Email.Contains(search));
            }

            if (departmentId.HasValue && departmentId.Value > 0)
            {
                query = query.Where(e => e.DepartmentId == departmentId.Value);
            }

            var employees = await query.OrderBy(e => e.EmployeeId).ToListAsync();
            var departments = await _context.Departments.OrderBy(d => d.DepartmentId).ToListAsync();

            // ส่งข้อมูลผ่าน ViewBag
            ViewBag.Employees = employees;
            ViewBag.Departments = departments;
            ViewBag.Search = search;
            ViewBag.SelectedDepartmentId = departmentId;
            ViewBag.TotalCount = employees.Count;

            return View();
        }


        // GET: Employees/GetDepartments — โหลด dropdown
        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            var depts = await _context.Departments
                .OrderBy(d => d.DepartmentId)
                .Select(d => new { departmentId = d.DepartmentId, departmentName = d.DepartmentName })
                .ToListAsync();
            return Json(new { success = true, data = depts });
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        // GET: Employees/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await PopulateDepartmentsAsync();
            await PopulatePositionsAsync();
            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("FullName,NationalId,DepartmentId,Position,PhoneNumber,Email")] Employee employee)
        {
            // เช็คว่ามี Employee active ที่ใช้ NationalId นี้อยู่แล้วไหม
            if (await _context.Employees.AnyAsync(e => e.NationalId == employee.NationalId))
            {
                ModelState.AddModelError("NationalId", "เลขบัตรประชาชนนี้มีอยู่ในระบบแล้ว");
            }

            if (ModelState.IsValid)
            {
                // เช็คว่ามีใน trash (soft-deleted) ที่ใช้ NationalId เดียวกันไหม
                var existingDeleted = await _context.Employees
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(e => e.NationalId == employee.NationalId && e.IsDeleted);

                if (existingDeleted != null)
                {
                    // ⬇️ มีใน trash → กู้คืน + อัปเดตข้อมูลใหม่
                    existingDeleted.IsDeleted = false;
                    existingDeleted.DeletedAt = null;
                    existingDeleted.FullName = employee.FullName;
                    existingDeleted.DepartmentId = employee.DepartmentId;
                    existingDeleted.Position = employee.Position;
                    existingDeleted.PhoneNumber = employee.PhoneNumber;
                    existingDeleted.Email = employee.Email;

                    // กู้บัญชี User ที่ผูกกัน (ถ้ามี)
                    var linkedUser = await _context.Users.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(u => u.EmployeeId == existingDeleted.EmployeeId);
                    if (linkedUser != null)
                    {
                        linkedUser.IsDeleted = false;
                        linkedUser.DeletedAt = null;
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"กู้คืนพนักงาน \"{existingDeleted.FullName}\" จากถังขยะและอัปเดตข้อมูลเรียบร้อย";
                }
                else
                {
                    // ADD + Create User
                    _context.Employees.Add(employee);
                    await _context.SaveChangesAsync();

                    //Create new Username
                    string baseUsername = $"emp{employee.EmployeeId}";
                    string username = baseUsername;
                    int suffix = 1;
                    while (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == username))
                    {
                        username = $"{baseUsername}_{suffix++}";
                    }

                    // password 6หลักท้ายจาก NationalId
                    string password = employee.NationalId.Length >= 6
                        ? employee.NationalId.Substring(employee.NationalId.Length - 6)
                        : employee.NationalId;

                    var newUser = new User
                    {
                        Username = username,
                        Password = password,
                        FullName = employee.FullName,
                        Role = "Employee",
                        EmployeeId = employee.EmployeeId
                    };
                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"เพิ่มพนักงาน \"{employee.FullName}\" เรียบร้อย - Login: {username} / รหัสผ่าน: {password}";
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDepartmentsAsync(employee.DepartmentId);
            await PopulatePositionsAsync();
            return View(employee);
        }

        // GET: Employees/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            await PopulateDepartmentsAsync(employee.DepartmentId);
            await PopulatePositionsAsync();
            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("EmployeeId,FullName,NationalId,DepartmentId,Position,PhoneNumber,Email")] Employee employee)
        {
            if (id != employee.EmployeeId) return NotFound();

            // Check NationalId uniqueness (excluding self)
            if (await _context.Employees.AnyAsync(e => e.NationalId == employee.NationalId && e.EmployeeId != id))
            {
                ModelState.AddModelError("NationalId", "เลขบัตรประชาชนนี้มีอยู่ในระบบแล้ว");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);

                    // Sync ชื่อใน User ที่ผูกกับพนักงานนี้
                    var linkedUser = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employee.EmployeeId);
                    if (linkedUser != null)
                    {
                        linkedUser.FullName = employee.FullName;
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "แก้ไขข้อมูลพนักงานเรียบร้อยแล้ว";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.EmployeeId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDepartmentsAsync(employee.DepartmentId);
            await PopulatePositionsAsync();
            return View(employee);
        }

        // GET: Employees/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null) return NotFound();
            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                if (IsAjax) return Json(new { success = false, message = "ไม่พบพนักงาน" });
                return NotFound();
            }

            // Soft delete employee
            employee.IsDeleted = true;
            employee.DeletedAt = DateTime.Now;

            // Soft delete linked user account ด้วย
            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);
            if (user != null)
            {
                user.IsDeleted = true;
                user.DeletedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            var msg = $"ลบพนักงาน \"{employee.FullName}\" เรียบร้อยแล้ว";
            if (IsAjax) return Json(new { success = true, message = msg });
            TempData["Success"] = msg;
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }

        private async Task PopulateDepartmentsAsync(int? selectedId = null)
        {
            var depts = await _context.Departments.OrderBy(d => d.DepartmentId).ToListAsync();
            ViewBag.Departments = new SelectList(depts, "DepartmentId", "DepartmentName", selectedId);
        }

        private async Task PopulatePositionsAsync()
        {
            var positions = await _context.Positions
                .OrderBy(p => p.PositionId)
                .Select(p => p.PositionName)
                .ToListAsync();
            ViewBag.Positions = positions;
        }

        // GET: Employees/ExportExcel
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportExcel()
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .OrderBy(e => e.EmployeeId)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("รายชื่อพนักงาน");

            // Header
            string[] headers = { "ลำดับ", "ชื่อ-นามสกุล", "แผนก", "ตำแหน่ง", "เลขประชาชน", "เบอร์โทร", "อีเมล" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
            }

            // จัดสไตล์ Header
            var headerRow = ws.Range(1, 1, 1, headers.Length);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data rows
            for (int i = 0; i < employees.Count; i++)
            {
                var e = employees[i];
                ws.Cell(i + 2, 1).Value = i + 1;
                ws.Cell(i + 2, 2).Value = e.FullName;
                ws.Cell(i + 2, 3).Value = e.Department?.DepartmentName ?? "";
                ws.Cell(i + 2, 4).Value = e.Position;
                ws.Cell(i + 2, 5).Value = e.NationalId;
                ws.Cell(i + 2, 6).Value = e.PhoneNumber;
                ws.Cell(i + 2, 7).Value = e.Email;
            }

            // ปรับความกว้างคอลัมน์อัตโนมัติ
            ws.Columns().AdjustToContents();

            // ตีกรอบ
            var dataRange = ws.Range(1, 1, employees.Count + 1, headers.Length);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"รายชื่อพนักงาน_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}