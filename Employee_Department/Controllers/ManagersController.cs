using ClosedXML.Excel;
using Employee_Department.Data;
using Employee_Department.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Employee_Department.Controllers
{
    [Authorize]
    public class ManagersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManagersController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        // GET: Managers
        public async Task<IActionResult> Index(string? search)
        {
            var managers = await GetFilteredManagersAsync(search);
            ViewBag.Managers = managers;
            ViewBag.Search = search;
            return View();
        }

        // GET: Managers/GetList — สำหรับ AJAX
        [HttpGet]
        public async Task<IActionResult> GetRows(string? search)
        {
            var managers = await GetFilteredManagersAsync(search);
            return PartialView("_ManagerRows", managers);
        }

        // Helper - reusable
        private async Task<List<Manager>> GetFilteredManagersAsync(string? search)
        {
            var query = _context.Managers.Include(m => m.Department).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m => m.FullName.Contains(search)
                    || m.NationalId.Contains(search)
                    || m.PhoneNumber.Contains(search)
                    || m.Email.Contains(search));
            }
            return await query.OrderBy(m => m.ManagerId).ToListAsync();
        }

        // GET: Managers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var manager = await _context.Managers
                .Include(m => m.Department)
                .FirstOrDefaultAsync(m => m.ManagerId == id);

            if (manager == null) return NotFound();
            return View(manager);
        }

        // GET: Managers/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var availableDepts = await _context.Departments
                .Where(d => !_context.Managers.Any(m => m.DepartmentId == d.DepartmentId))
                .ToListAsync();

            if (!availableDepts.Any())
            {
                TempData["Error"] = "ทุกแผนกมีหัวหน้าครบแล้ว ไม่สามารถเพิ่มได้";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Departments = new SelectList(availableDepts, "DepartmentId", "DepartmentName");
            return View();
        }

        // POST: Managers/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("FullName,NationalId,DepartmentId,PhoneNumber,Email")] Manager manager)
        {
            if (await _context.Managers.AnyAsync(m => m.DepartmentId == manager.DepartmentId))
                ModelState.AddModelError("DepartmentId", "แผนกนี้มีหัวหน้าอยู่แล้ว");

            if (await _context.Managers.AnyAsync(m => m.NationalId == manager.NationalId))
                ModelState.AddModelError("NationalId", "เลขบัตรประชาชนนี้มีอยู่ในระบบแล้ว");

            if (ModelState.IsValid)
            {
                var existingDeleted = await _context.Managers
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(m =>
                        (m.NationalId == manager.NationalId || m.DepartmentId == manager.DepartmentId)
                        && m.IsDeleted);

                if (existingDeleted != null)
                {
                    existingDeleted.IsDeleted = false;
                    existingDeleted.DeletedAt = null;
                    existingDeleted.FullName = manager.FullName;
                    existingDeleted.NationalId = manager.NationalId;
                    existingDeleted.DepartmentId = manager.DepartmentId;
                    existingDeleted.PhoneNumber = manager.PhoneNumber;
                    existingDeleted.Email = manager.Email;

                    var linkedUser = await _context.Users.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(u => u.ManagerId == existingDeleted.ManagerId);
                    if (linkedUser != null)
                    {
                        linkedUser.IsDeleted = false;
                        linkedUser.DeletedAt = null;
                        linkedUser.FullName = manager.FullName;
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"กู้คืนหัวหน้าแผนก \"{existingDeleted.FullName}\" จากถังขยะและอัปเดตข้อมูลเรียบร้อย";
                }
                else
                {
                    _context.Managers.Add(manager);
                    await _context.SaveChangesAsync();

                    string baseUsername = $"manager{manager.ManagerId}";
                    string username = baseUsername;
                    int suffix = 1;
                    while (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == username))
                    {
                        username = $"{baseUsername}_{suffix++}";
                    }

                    string password = manager.NationalId.Length >= 6
                        ? manager.NationalId.Substring(manager.NationalId.Length - 6)
                        : manager.NationalId;

                    var newUser = new User
                    {
                        Username = username,
                        Password = password,
                        FullName = manager.FullName,
                        Role = "Manager",
                        ManagerId = manager.ManagerId
                    };
                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"เพิ่มหัวหน้าแผนก \"{manager.FullName}\" เรียบร้อย - Login: {username} / รหัสผ่าน: {password}";
                }
                return RedirectToAction(nameof(Index));
            }

            var availableDepts = await _context.Departments
                .Where(d => !_context.Managers.Any(m => m.DepartmentId == d.DepartmentId) || d.DepartmentId == manager.DepartmentId)
                .ToListAsync();
            ViewBag.Departments = new SelectList(availableDepts, "DepartmentId", "DepartmentName", manager.DepartmentId);
            return View(manager);
        }

        // GET: Managers/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var manager = await _context.Managers.FindAsync(id);
            if (manager == null) return NotFound();

            var availableDepts = await _context.Departments
                .Where(d => !_context.Managers.Any(m => m.DepartmentId == d.DepartmentId) || d.DepartmentId == manager.DepartmentId)
                .ToListAsync();
            ViewBag.Departments = new SelectList(availableDepts, "DepartmentId", "DepartmentName", manager.DepartmentId);

            return View(manager);
        }

        // POST: Managers/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("ManagerId,FullName,NationalId,DepartmentId,PhoneNumber,Email")] Manager manager)
        {
            if (id != manager.ManagerId) return NotFound();

            if (await _context.Managers.AnyAsync(m => m.NationalId == manager.NationalId && m.ManagerId != id))
                ModelState.AddModelError("NationalId", "เลขบัตรประชาชนนี้มีอยู่ในระบบแล้ว");

            if (await _context.Managers.AnyAsync(m => m.DepartmentId == manager.DepartmentId && m.ManagerId != id))
                ModelState.AddModelError("DepartmentId", "แผนกนี้มีหัวหน้าอยู่แล้ว");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(manager);
                    var linkedUser = await _context.Users.FirstOrDefaultAsync(u => u.ManagerId == manager.ManagerId);
                    if (linkedUser != null) linkedUser.FullName = manager.FullName;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "แก้ไขข้อมูลหัวหน้าแผนกเรียบร้อยแล้ว";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Managers.Any(m => m.ManagerId == manager.ManagerId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            var availableDepts = await _context.Departments
                .Where(d => !_context.Managers.Any(m => m.DepartmentId == d.DepartmentId) || d.DepartmentId == manager.DepartmentId)
                .ToListAsync();
            ViewBag.Departments = new SelectList(availableDepts, "DepartmentId", "DepartmentName", manager.DepartmentId);
            return View(manager);
        }

        // GET: Managers/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var manager = await _context.Managers
                .Include(m => m.Department)
                .FirstOrDefaultAsync(m => m.ManagerId == id);
            if (manager == null) return NotFound();
            return View(manager);
        }

        // POST: Managers/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var manager = await _context.Managers.FindAsync(id);
            if (manager == null)
            {
                if (IsAjax) return Json(new { success = false, message = "ไม่พบหัวหน้าแผนก" });
                return NotFound();
            }

            manager.IsDeleted = true;
            manager.DeletedAt = DateTime.Now;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ManagerId == id);
            if (user != null)
            {
                user.IsDeleted = true;
                user.DeletedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            var msg = $"ลบหัวหน้าแผนก \"{manager.FullName}\" เรียบร้อยแล้ว";
            if (IsAjax) return Json(new { success = true, message = msg });
            TempData["Success"] = msg;
            return RedirectToAction(nameof(Index));
        }

        // GET: Managers/ExportExcel
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportExcel()
        {
            var managers = await _context.Managers
                .Include(m => m.Department)
                .OrderBy(m => m.ManagerId)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("รายชื่อหัวหน้าแผนก");

            string[] headers = { "ลำดับ", "ชื่อ-นามสกุล", "แผนกที่รับผิดชอบ", "เลขประชาชน", "เบอร์โทร", "อีเมล" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            var headerRow = ws.Range(1, 1, 1, headers.Length);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            for (int i = 0; i < managers.Count; i++)
            {
                var m = managers[i];
                ws.Cell(i + 2, 1).Value = i + 1;
                ws.Cell(i + 2, 2).Value = m.FullName;
                ws.Cell(i + 2, 3).Value = m.Department?.DepartmentName ?? "";
                ws.Cell(i + 2, 4).Value = m.NationalId;
                ws.Cell(i + 2, 5).Value = m.PhoneNumber;
                ws.Cell(i + 2, 6).Value = m.Email;
            }

            ws.Columns().AdjustToContents();
            var dataRange = ws.Range(1, 1, managers.Count + 1, headers.Length);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"หัวหน้าแผนก_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}