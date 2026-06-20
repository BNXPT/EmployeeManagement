using Employee_Department.Data;
using Employee_Department.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Employee_Department.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PositionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PositionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        //ตรวจสอบว่า Request ปัจจุบันถูกส่งมาจาก Ajax หรือไม่
        private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        //Get: Position
        public async Task<IActionResult> Index()
        {
            var data = await GetPositionDataAsync();
            ViewBag.PosData = data;
            return View();
        }

        // GET: Positions/GetRows — AJAX
        [HttpGet]
        public async Task<IActionResult> GetRows(string? search)
        {
            var data = await GetPositionDataAsync(search);
            return PartialView("_PositionRows", data);
        }

        private async Task<List<dynamic>> GetPositionDataAsync(string? search = null)
        {
            var query = _context.Positions.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.PositionName.Contains(search));

            return await query
                .Select(p => new
                {
                    Position = p,
                    UsageCount = _context.Employees.Count(e => e.Position == p.PositionName)
                })
                .OrderBy(x => x.Position.PositionId)
                .ToListAsync<dynamic>();
        }


        //Get: Position/Create
        public IActionResult Create() => View(new Position());

        // รับข้อมูลจาก Form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PositionName")] Position position)
        {
            if (string.IsNullOrWhiteSpace(position.PositionName))
            {
                if (IsAjax) return Json(new { success = false, message = "กรุณาระบุชื่อตำแหน่ง" });
                return View(position);
            }

            // เช็คว่าตำแหน่งซ้ำมั้ย
            if (await _context.Positions.AnyAsync(p => p.PositionName == position.PositionName))
            {
                if (IsAjax) return Json(new { success = false, message = "ชื่อตำแหน่งนี้มีอยู่แล้ว" });
                ModelState.AddModelError("PositionName", "ชื่อตำแหน่งนี้มีอยู่แล้ว");
                return View(position);
            }

            // Check from trash
            var existingDeleted = await _context.Positions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.PositionName == position.PositionName && p.IsDeleted);

            string msg;
            if (existingDeleted != null)
            {
                existingDeleted.IsDeleted = false;
                existingDeleted.DeletedAt = null;
                await _context.SaveChangesAsync();
                msg = $"กู้คืนตำแหน่ง \"{existingDeleted.PositionName}\" จากถังขยะเรียบร้อย";
            }

            // Add new
            else
            {
                _context.Positions.Add(position);
                await _context.SaveChangesAsync();
                msg = $"เพิ่มตำแหน่ง \"{position.PositionName}\" เรียบร้อยแล้ว";
            }

            if (IsAjax) return Json(new { success = true, message = msg });
            TempData["Success"] = msg;
            return RedirectToAction(nameof(Index));
        }

        // Get: Position / Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PositionId,PositionName")] Position position)
        {
            //Check PositionId ป้องกันแก้ไขข้อมูลผิดรายการ
            if (id != position.PositionId)
            {
                if (IsAjax) return Json(new { success = false, message = "ID ไม่ตรงกัน" });
                return NotFound();
            }

            // ดึงข้อมูลเดิม
            var oldPosition = await _context.Positions.AsNoTracking().FirstOrDefaultAsync(p => p.PositionId == id);

            // เช็คว่าชื่อตำแหน่งซ้ำมั้ย
            if (await _context.Positions.AnyAsync(p => p.PositionName == position.PositionName && p.PositionId != id))
            {
                if (IsAjax) return Json(new { success = false, message = "ชื่อตำแหน่งนี้มีอยู่แล้ว" });
                ModelState.AddModelError("PositionName", "ชื่อตำแหน่งนี้มีอยู่แล้ว");
                return View(position);
            }

            // แก้ไขชื่อตำแหน่ง
            _context.Update(position);

            if (oldPosition != null && oldPosition.PositionName != position.PositionName)
            {
                var employees = await _context.Employees
                    .Where(e => e.Position == oldPosition.PositionName)
                    .ToListAsync();
                foreach (var e in employees) e.Position = position.PositionName;
            }

            await _context.SaveChangesAsync();

            var msg = "แก้ไขชื่อตำแหน่งเรียบร้อยแล้ว";
            if (IsAjax) return Json(new { success = true, message = msg });
            TempData["Success"] = msg;
            return RedirectToAction(nameof(Index));
        }

        //Get: Position / Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Find PositionId from Ajax
            var pos = await _context.Positions.FindAsync(id);
            if (pos == null)
            {
                if (IsAjax) return Json(new { success = false, message = "ไม่พบตำแหน่งนี้" });
                return NotFound();
            }

            // นับจำนวนพนักงานในตำแหน่ง(ถ้ามีพนักงานจะลบไม่ได้)
            var usageCount = await _context.Employees.CountAsync(e => e.Position == pos.PositionName);
            if (usageCount > 0)
            {
                var errMsg = $"ลบไม่ได้: ตำแหน่ง \"{pos.PositionName}\" กำลังถูกใช้โดยพนักงาน {usageCount} คน";
                if (IsAjax) return Json(new { success = false, message = errMsg });
                TempData["Error"] = errMsg;
                return RedirectToAction(nameof(Index));
            }

            // Deleted Position
            pos.IsDeleted = true;
            pos.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var sucMsg = $"ลบตำแหน่ง \"{pos.PositionName}\" เรียบร้อยแล้ว";
            if (IsAjax) return Json(new { success = true, message = sucMsg });
            TempData["Success"] = sucMsg;
            return RedirectToAction(nameof(Index));
        }

        // ดึงข้อมูลตำแหน่งงาน 1 รายการตาม ID แล้วส่งกลับเป็น JSON เพื่อให้ JavaScript/Ajax นำไปแสดงใน Modal หรือ Form แก้ไข
        [HttpGet]
        public async Task<IActionResult> GetOne(int id)
        {
            var pos = await _context.Positions.FindAsync(id);
            if (pos == null) return Json(new { success = false, message = "ไม่พบ" });
            return Json(new { success = true, data = new { pos.PositionId, pos.PositionName } });
        }
    }
}