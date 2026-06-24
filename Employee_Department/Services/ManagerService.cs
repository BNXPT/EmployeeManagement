using ClosedXML.Excel;
using Employee_Department.Data;
using Employee_Department.Models;
using Microsoft.EntityFrameworkCore;

namespace Employee_Department.Services
{
    public class ManagerService : IManagerService
    {
        private readonly ApplicationDbContext _context;

        public ManagerService(ApplicationDbContext context) => _context = context;

        public async Task<List<Manager>> GetFilteredAsync(string? search)
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

        public async Task<Manager?> GetByIdAsync(int id)
            => await _context.Managers.Include(m => m.Department).FirstOrDefaultAsync(m => m.ManagerId == id);

        public async Task<ServiceResult> CreateAsync(Manager manager)
        {
            if (await _context.Managers.AnyAsync(m => m.DepartmentId == manager.DepartmentId))
                return ServiceResult.Fail("แผนกนี้มีหัวหน้าอยู่แล้ว");

            if (await _context.Managers.AnyAsync(m => m.NationalId == manager.NationalId))
                return ServiceResult.Fail("เลขบัตรประชาชนนี้มีอยู่ในระบบแล้ว");

            var existingDeleted = await _context.Managers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => (m.NationalId == manager.NationalId || m.DepartmentId == manager.DepartmentId) && m.IsDeleted);

            string msg;
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
                msg = $"กู้คืนหัวหน้า \"{existingDeleted.FullName}\" จากถังขยะ";
            }
            else
            {
                _context.Managers.Add(manager);
                await _context.SaveChangesAsync();

                string baseUsername = $"manager{manager.ManagerId}";
                string username = baseUsername;
                int suffix = 1;
                while (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == username))
                    username = $"{baseUsername}_{suffix++}";

                string password = manager.NationalId.Length >= 6
                    ? manager.NationalId.Substring(manager.NationalId.Length - 6)
                    : manager.NationalId;

                _context.Users.Add(new User
                {
                    Username = username,
                    Password = password,
                    FullName = manager.FullName,
                    Role = "Manager",
                    ManagerId = manager.ManagerId
                });
                await _context.SaveChangesAsync();
                msg = $"เพิ่มหัวหน้า \"{manager.FullName}\" — Login: {username} / รหัสผ่าน: {password}";
            }
            return ServiceResult.Ok(msg);
        }

        public async Task<ServiceResult> UpdateAsync(Manager manager)
        {
            if (await _context.Managers.AnyAsync(m => m.NationalId == manager.NationalId && m.ManagerId != manager.ManagerId))
                return ServiceResult.Fail("เลขบัตรประชาชนนี้มีอยู่ในระบบแล้ว");

            if (await _context.Managers.AnyAsync(m => m.DepartmentId == manager.DepartmentId && m.ManagerId != manager.ManagerId))
                return ServiceResult.Fail("แผนกนี้มีหัวหน้าอยู่แล้ว");

            _context.Update(manager);
            var linkedUser = await _context.Users.FirstOrDefaultAsync(u => u.ManagerId == manager.ManagerId);
            if (linkedUser != null) linkedUser.FullName = manager.FullName;

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("แก้ไขข้อมูลหัวหน้าเรียบร้อยแล้ว");
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var manager = await _context.Managers.FindAsync(id);
            if (manager == null) return ServiceResult.Fail("ไม่พบหัวหน้า");

            manager.IsDeleted = true;
            manager.DeletedAt = DateTime.Now;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ManagerId == id);
            if (user != null)
            {
                user.IsDeleted = true;
                user.DeletedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Ok($"ลบหัวหน้า \"{manager.FullName}\" เรียบร้อย");
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var managers = await GetFilteredAsync(null);
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("รายชื่อหัวหน้าแผนก");

            string[] headers = { "ลำดับ", "ชื่อ-นามสกุล", "แผนกที่รับผิดชอบ", "เลขประชาชน", "เบอร์โทร", "อีเมล" };
            for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

            var headerRow = ws.Range(1, 1, 1, headers.Length);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
            headerRow.Style.Font.FontColor = XLColor.White;

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
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}