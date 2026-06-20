using System.Security.Claims;
using Employee_Department.Data;
using Employee_Department.Models;
using Employee_Department.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace Employee_Department.Controllers
{
    [Authorize]
    public class LeaveRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;
        private readonly ILogger<LeaveRequestsController> _logger;

        public LeaveRequestsController(ApplicationDbContext context, IEmailService email, ILogger<LeaveRequestsController> logger)
        {
            _context = context;
            _email = email;
            _logger = logger;
        }

        // GET: LeaveRequests - shows requests
        // Employee sees own, Manager sees their department's, Admin sees all
        public async Task<IActionResult> Index()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var userIdStr = User.FindFirst("UserId")?.Value;
            int.TryParse(userIdStr, out int userId);

            var query = _context.LeaveRequests
                .Include(l => l.Employee).ThenInclude(e => e!.Department)
                .Include(l => l.RespondedByManager)
                .AsQueryable();

            if (role == "Employee")
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user?.EmployeeId != null)
                {
                    query = query.Where(l => l.EmployeeId == user.EmployeeId);
                }
            }
            else if (role == "Manager")
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user?.ManagerId != null)
                {
                    var mgr = await _context.Managers.FirstOrDefaultAsync(m => m.ManagerId == user.ManagerId);
                    if (mgr != null)
                        query = query.Where(l => l.Employee!.DepartmentId == mgr.DepartmentId);
                }
            }
            // Admin sees all

            var list = await query.OrderByDescending(l => l.RequestedAt).ToListAsync();
            ViewBag.Role = role;
            return View(list);
        }

        // GET: LeaveRequests/Create  (Employee only)
        [Authorize(Roles = "Employee")]
        public IActionResult Create()
        {
            return View(new LeaveRequest
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today
            });
        }

        // POST: LeaveRequests/Create
        [HttpPost]
        [Authorize(Roles = "Employee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LeaveType,Reason,StartDate,EndDate")] LeaveRequest req)
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            int.TryParse(userIdStr, out int userId);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user?.EmployeeId == null)
            {
                ModelState.AddModelError(string.Empty, "ไม่พบข้อมูลพนักงาน");
                return View(req);
            }

            if (req.EndDate.Date < req.StartDate.Date)
            {
                ModelState.AddModelError("EndDate", "วันที่สิ้นสุดต้องไม่น้อยกว่าวันที่เริ่ม");
            }

            if (!ModelState.IsValid) return View(req);

            req.EmployeeId = user.EmployeeId.Value;
            req.Status = "Pending";
            req.RequestedAt = DateTime.Now;
            req.ApprovalToken = Guid.NewGuid().ToString("N");

            _context.LeaveRequests.Add(req);
            await _context.SaveChangesAsync();

            // Send email to manager
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Department).ThenInclude(d => d!.Manager)
                    .FirstOrDefaultAsync(e => e.EmployeeId == req.EmployeeId);

                if (employee?.Department?.Manager != null)
                {
                    var baseUrl = $"{Request.Scheme}://{Request.Host}";
                    var approveUrl = $"{baseUrl}/LeaveRequests/Respond?token={req.ApprovalToken}&decision=approve";
                    var rejectUrl = $"{baseUrl}/LeaveRequests/Respond?token={req.ApprovalToken}&decision=reject";
                    var viewUrl    = $"{baseUrl}/LeaveRequests/Details/{req.LeaveRequestId}";

                    var html = EmailTemplates.BuildLeaveRequestEmail(
                        req, employee, employee.Department.DepartmentName,
                        approveUrl, rejectUrl, viewUrl);

                    await _email.SendAsync(
                        employee.Department.Manager.Email,
                        $"[คำขอลางาน #{req.LeaveRequestId:D5}] {employee.FullName} ขอลา {req.LeaveType}",
                        html);

                    TempData["Success"] = "ส่งคำขอเรียบร้อย และแจ้งหัวหน้าแผนกผ่านอีเมลแล้ว";
                }
                else
                {
                    TempData["Success"] = "ส่งคำขอเรียบร้อย (แต่แผนกของคุณยังไม่มีหัวหน้า)";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email for leave request {Id}", req.LeaveRequestId);
                TempData["Error"] = "บันทึกคำขอแล้ว แต่ส่งอีเมลแจ้งหัวหน้าไม่สำเร็จ — โปรดตรวจ SMTP config";
            }

            return RedirectToAction(nameof(Details), new { id = req.LeaveRequestId });
        }

        // GET: LeaveRequests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var req = await _context.LeaveRequests
                .Include(l => l.Employee).ThenInclude(e => e!.Department).ThenInclude(d => d!.Manager)
                .Include(l => l.RespondedByManager)
                .FirstOrDefaultAsync(l => l.LeaveRequestId == id);
            if (req == null) return NotFound();
            return View(req);
        }

        // GET: LeaveRequests/Respond?token=...&action=approve|reject
        // Public endpoint - reached from email link, NO login required
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Respond(string token, string decision)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(decision))
                return View("RespondResult", new RespondResultVM { Ok = false, Message = "ลิงก์ไม่ถูกต้อง" });

            var req = await _context.LeaveRequests
                .Include(l => l.Employee).ThenInclude(e => e!.Department).ThenInclude(d => d!.Manager)
                .FirstOrDefaultAsync(l => l.ApprovalToken == token);
            if (req == null)
                return View("RespondResult", new RespondResultVM { Ok = false, Message = "ไม่พบคำขอนี้ในระบบ" });

            if (req.Status != "Pending")
                return View("RespondResult", new RespondResultVM
                {
                    Ok = false,
                    Message = $"คำขอนี้ถูกดำเนินการไปแล้ว สถานะปัจจุบัน: {req.Status}",
                    LeaveRequest = req
                });

            return View("RespondConfirm", new RespondConfirmVM
            {
                Token = token,
                Decision = decision,
                LeaveRequest = req
            });
        }

        // POST: LeaveRequests/Respond
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondConfirm(string token, string decision, string? comment)
        {
            var req = await _context.LeaveRequests
                .Include(l => l.Employee).ThenInclude(e => e!.Department).ThenInclude(d => d!.Manager)
                .FirstOrDefaultAsync(l => l.ApprovalToken == token);
            if (req == null)
                return View("RespondResult", new RespondResultVM { Ok = false, Message = "ไม่พบคำขอนี้" });

            if (req.Status != "Pending")
                return View("RespondResult", new RespondResultVM
                {
                    Ok = false,
                    Message = $"คำขอนี้ถูกดำเนินการไปแล้ว ({req.Status})",
                    LeaveRequest = req
                });

            req.Status = decision == "approve" ? "Approved" : "Rejected";
            req.RespondedAt = DateTime.Now;
            req.ManagerComment = comment;
            req.RespondedByManagerId = req.Employee?.Department?.Manager?.ManagerId;

            await _context.SaveChangesAsync();

            try
            {
                if (req.Employee != null && !string.IsNullOrWhiteSpace(req.Employee.Email))
                {
                    var html = EmailTemplates.BuildEmployeeNotificationEmail(req, req.Employee, req.Status == "Approved", comment);
                    await _email.SendAsync(
                        req.Employee.Email,
                        $"[ผลคำขอลา #{req.LeaveRequestId:D5}] {(req.Status == "Approved" ? "อนุมัติ" : "ปฏิเสธ")}",
                        html);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify employee for request {Id}", req.LeaveRequestId);
            }

            return View("RespondResult", new RespondResultVM
            {
                Ok = true,
                Message = req.Status == "Approved" ? "อนุมัติคำขอเรียบร้อยแล้ว" : "ปฏิเสธคำขอเรียบร้อยแล้ว",
                LeaveRequest = req
            });
        }

        // GET: LeaveRequests/Delete/5
        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null) return NotFound();
            var req = await _context.LeaveRequests.FindAsync(id);
            if (req == null) return NotFound();

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var userIdStr = User.FindFirst("UserId")?.Value;
            int.TryParse(userIdStr, out int userId);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            bool canCancel = role == "Admin" ||
                (role == "Employee" && user?.EmployeeId == req.EmployeeId && req.Status == "Pending");
            if (!canCancel) return Forbid();

            req.IsDeleted = true;
            req.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["Success"] = "ยกเลิก/ลบคำขอเรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index));
        }

        // GET: LeaveRequests/ExportExcel
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportExcel()
        {
            var leaves = await _context.LeaveRequests
                .Include(l => l.Employee).ThenInclude(e => e!.Department)
                .OrderByDescending(l => l.RequestedAt)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("คำขอลางาน");

            string[] headers = { "เลขที่", "พนักงาน", "แผนก", "ประเภทการลา", "วันที่เริ่ม", "วันที่สิ้นสุด", "จำนวนวัน", "เหตุผล", "สถานะ", "ยื่นเมื่อ", "ตอบกลับเมื่อ", "ความเห็นหัวหน้า" };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            var headerRow = ws.Range(1, 1, 1, headers.Length);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            for (int i = 0; i < leaves.Count; i++)
            {
                var l = leaves[i];
                var days = Math.Max(1, (l.EndDate.Date - l.StartDate.Date).Days + 1);
                var statusText = l.Status switch
                {
                    "Pending" => "รอพิจารณา",
                    "Approved" => "อนุมัติ",
                    "Rejected" => "ปฏิเสธ",
                    _ => l.Status
                };

                ws.Cell(i + 2, 1).Value = $"#{l.LeaveRequestId:D5}";
                ws.Cell(i + 2, 2).Value = l.Employee?.FullName ?? "";
                ws.Cell(i + 2, 3).Value = l.Employee?.Department?.DepartmentName ?? "";
                ws.Cell(i + 2, 4).Value = l.LeaveType;
                ws.Cell(i + 2, 5).Value = $"{l.StartDate:dd/MM/}{l.StartDate.Year + 543}";
                ws.Cell(i + 2, 6).Value = $"{l.EndDate:dd/MM/}{l.EndDate.Year + 543}";
                ws.Cell(i + 2, 7).Value = days;
                ws.Cell(i + 2, 8).Value = l.Reason;
                ws.Cell(i + 2, 9).Value = statusText;
                ws.Cell(i + 2, 10).Value = $"{l.RequestedAt:dd/MM/}{l.RequestedAt.Year + 543} {l.RequestedAt:HH:mm}";
                ws.Cell(i + 2, 11).Value = l.RespondedAt.HasValue
                    ? $"{l.RespondedAt.Value:dd/MM/}{l.RespondedAt.Value.Year + 543} {l.RespondedAt.Value:HH:mm}"
                    : "-";
                ws.Cell(i + 2, 12).Value = l.ManagerComment ?? "-";
            }

            ws.Columns().AdjustToContents();
            var dataRange = ws.Range(1, 1, leaves.Count + 1, headers.Length);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"คำขอลางาน_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }

    public class RespondConfirmVM
    {
        public string Token { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty;
        public LeaveRequest LeaveRequest { get; set; } = null!;
    }

    public class RespondResultVM
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = string.Empty;
        public LeaveRequest? LeaveRequest { get; set; }
    }
}
