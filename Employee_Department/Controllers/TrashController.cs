using Employee_Department.Data;
using Employee_Department.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Employee_Department.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TrashController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrashController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Trash - แสดงข้อมูลที่ถูกลบทั้งหมด
        public async Task<IActionResult> Index()
        {
            var vm = new TrashViewModel
            {
                Departments = await _context.Departments.IgnoreQueryFilters()
                    .Where(d => d.IsDeleted).OrderByDescending(d => d.DeletedAt).ToListAsync(),

                Employees = await _context.Employees.IgnoreQueryFilters()
                    .Include(e => e.Department)
                    .Where(e => e.IsDeleted).OrderByDescending(e => e.DeletedAt).ToListAsync(),

                Managers = await _context.Managers.IgnoreQueryFilters()
                    .Include(m => m.Department)
                    .Where(m => m.IsDeleted).OrderByDescending(m => m.DeletedAt).ToListAsync(),

                Positions = await _context.Positions.IgnoreQueryFilters()
                    .Where(p => p.IsDeleted).OrderByDescending(p => p.DeletedAt).ToListAsync(),

                LeaveRequests = await _context.LeaveRequests.IgnoreQueryFilters()
                    .Include(l => l.Employee)
                    .Where(l => l.IsDeleted).OrderByDescending(l => l.DeletedAt).ToListAsync(),

                Users = await _context.Users.IgnoreQueryFilters()
                    .Where(u => u.IsDeleted).OrderByDescending(u => u.DeletedAt).ToListAsync()
            };
            return View(vm);
        }

        // POST: Trash/RestoreDepartment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreDepartment(int id)
        {
            var dept = await _context.Departments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.DepartmentId == id);
            if (dept == null) return NotFound();

            dept.IsDeleted = false;
            dept.DeletedAt = null;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"กู้คืนแผนก \"{dept.DepartmentName}\" เรียบร้อย";
            return RedirectToAction(nameof(Index));
        }

        // POST: Trash/RestoreEmployee/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreEmployee(int id)
        {
            var emp = await _context.Employees.IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.EmployeeId == id);
            if (emp == null) return NotFound();

            emp.IsDeleted = false;
            emp.DeletedAt = null;

            // กู้บัญชี User ที่ผูกกัน
            var user = await _context.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.EmployeeId == id);
            if (user != null)
            {
                user.IsDeleted = false;
                user.DeletedAt = null;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"กู้คืนพนักงาน \"{emp.FullName}\" เรียบร้อย";
            return RedirectToAction(nameof(Index));
        }

        // POST: Trash/RestoreManager/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreManager(int id)
        {
            var mgr = await _context.Managers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.ManagerId == id);
            if (mgr == null) return NotFound();

            mgr.IsDeleted = false;
            mgr.DeletedAt = null;

            var user = await _context.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.ManagerId == id);
            if (user != null)
            {
                user.IsDeleted = false;
                user.DeletedAt = null;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"กู้คืนหัวหน้าแผนก \"{mgr.FullName}\" เรียบร้อย";
            return RedirectToAction(nameof(Index));
        }

        // POST: Trash/RestorePosition/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestorePosition(int id)
        {
            var pos = await _context.Positions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.PositionId == id);
            if (pos == null) return NotFound();

            pos.IsDeleted = false;
            pos.DeletedAt = null;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"กู้คืนตำแหน่ง \"{pos.PositionName}\" เรียบร้อย";
            return RedirectToAction(nameof(Index));
        }

        // POST: Trash/RestoreLeaveRequest/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreLeaveRequest(int id)
        {
            var req = await _context.LeaveRequests.IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.LeaveRequestId == id);
            if (req == null) return NotFound();

            req.IsDeleted = false;
            req.DeletedAt = null;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"กู้คืนคำขอลางาน #{req.LeaveRequestId:D5} เรียบร้อย";
            return RedirectToAction(nameof(Index));
        }

        // POST: Trash/PurgeDepartment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurgeDepartment(int id)
        {
            var dept = await _context.Departments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.DepartmentId == id);
            if (dept == null) return NotFound();

            var hasEmployees = await _context.Employees.IgnoreQueryFilters()
                .AnyAsync(e => e.DepartmentId == id);
            var hasManager = await _context.Managers.IgnoreQueryFilters()
                .AnyAsync(m => m.DepartmentId == id);

            if (hasEmployees || hasManager)
            {
                TempData["Error"] = $"ลบถาวรไม่ได้: แผนก \"{dept.DepartmentName}\" ยังมีพนักงาน/หัวหน้าอ้างอิงอยู่";
                return RedirectToAction(nameof(Index));
            }

            _context.Departments.Remove(dept);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"ลบแผนก \"{dept.DepartmentName}\" ถาวรเรียบร้อย";
            return RedirectToAction(nameof(Index));
        }

        // POST: Trash/PurgeEmployee/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurgeEmployee(int id)
        {
            var emp = await _context.Employees.IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.EmployeeId == id);
            if (emp == null) return NotFound();

            var leaves = await _context.LeaveRequests.IgnoreQueryFilters()
                .Where(l => l.EmployeeId == id).ToListAsync();
            _context.LeaveRequests.RemoveRange(leaves);

            var user = await _context.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.EmployeeId == id);
            if (user != null) _context.Users.Remove(user);

            _context.Employees.Remove(emp);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"ลบพนักงาน \"{emp.FullName}\" ถาวรเรียบร้อย";
            return RedirectToAction(nameof(Index));
        }

        // POST: Trash/PurgeManager/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurgeManager(int id)
        {
            var mgr = await _context.Managers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.ManagerId == id);
            if (mgr == null) return NotFound();

            var responses = await _context.LeaveRequests.IgnoreQueryFilters()
                .Where(l => l.RespondedByManagerId == id).ToListAsync();
            foreach (var r in responses) r.RespondedByManagerId = null;

            var user = await _context.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.ManagerId == id);
            if (user != null) _context.Users.Remove(user);

            _context.Managers.Remove(mgr);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"ลบหัวหน้าแผนก \"{mgr.FullName}\" ถาวรเรียบร้อย";
            return RedirectToAction(nameof(Index));
        }

        // POST: Trash/PurgePosition/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurgePosition(int id)
        {
            var pos = await _context.Positions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.PositionId == id);
            if (pos == null) return NotFound();

            _context.Positions.Remove(pos);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"ลบตำแหน่ง \"{pos.PositionName}\" ถาวรเรียบร้อย";
            return RedirectToAction(nameof(Index));
        }

        // POST: Trash/PurgeLeaveRequest/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurgeLeaveRequest(int id)
        {
            var req = await _context.LeaveRequests.IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.LeaveRequestId == id);
            if (req == null) return NotFound();

            _context.LeaveRequests.Remove(req);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"ลบคำขอ #{req.LeaveRequestId:D5} ถาวรเรียบร้อย";
            return RedirectToAction(nameof(Index));
        }

        // POST: Trash/EmptyTrash — ลบทุกอย่างใน trash
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmptyTrash()
        {
            var leaveRequests = await _context.LeaveRequests.IgnoreQueryFilters()
                .Where(l => l.IsDeleted).ToListAsync();
            _context.LeaveRequests.RemoveRange(leaveRequests);

            var users = await _context.Users.IgnoreQueryFilters()
                .Where(u => u.IsDeleted).ToListAsync();
            _context.Users.RemoveRange(users);

            var employees = await _context.Employees.IgnoreQueryFilters()
                .Where(e => e.IsDeleted).ToListAsync();
            _context.Employees.RemoveRange(employees);

            var managers = await _context.Managers.IgnoreQueryFilters()
                .Where(m => m.IsDeleted).ToListAsync();
            _context.Managers.RemoveRange(managers);

            var positions = await _context.Positions.IgnoreQueryFilters()
                .Where(p => p.IsDeleted).ToListAsync();
            _context.Positions.RemoveRange(positions);

            var departments = await _context.Departments.IgnoreQueryFilters()
                .Where(d => d.IsDeleted).ToListAsync();
            _context.Departments.RemoveRange(departments);

            await _context.SaveChangesAsync();
            TempData["Success"] = "ล้างถังขยะถาวรเรียบร้อย";
            return RedirectToAction(nameof(Index));
        }
    }

    public class TrashViewModel
    {
        public List<Department> Departments { get; set; } = new();
        public List<Employee> Employees { get; set; } = new();
        public List<Manager> Managers { get; set; } = new();
        public List<Position> Positions { get; set; } = new();
        public List<LeaveRequest> LeaveRequests { get; set; } = new();
        public List<User> Users { get; set; } = new();
    }
}