using ClosedXML.Excel;
using Employee_Department.Data;
using Employee_Department.Models;
using Microsoft.EntityFrameworkCore;

namespace Employee_Department.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _context;

        public EmployeeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Employee>> GetFilteredAsync(string? search, int? departmentId)
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
                query = query.Where(e => e.DepartmentId == departmentId.Value);

            return await query.OrderBy(e => e.EmployeeId).ToListAsync();
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            return await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);
        }

        public async Task<ServiceResult> CreateAsync(Employee employee)
        {
            if (await _context.Employees.AnyAsync(e => e.NationalId == employee.NationalId))
                return ServiceResult.Fail("เลขบัตรประชาชนนี้มีอยู่ในระบบแล้ว");

            // กู้คืนจาก trash ถ้ามี
            var existingDeleted = await _context.Employees
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.NationalId == employee.NationalId && e.IsDeleted);

            string msg;
            if (existingDeleted != null)
            {
                existingDeleted.IsDeleted = false;
                existingDeleted.DeletedAt = null;
                existingDeleted.FullName = employee.FullName;
                existingDeleted.DepartmentId = employee.DepartmentId;
                existingDeleted.Position = employee.Position;
                existingDeleted.PhoneNumber = employee.PhoneNumber;
                existingDeleted.Email = employee.Email;

                var linkedUser = await _context.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.EmployeeId == existingDeleted.EmployeeId);
                if (linkedUser != null)
                {
                    linkedUser.IsDeleted = false;
                    linkedUser.DeletedAt = null;
                    linkedUser.FullName = employee.FullName;
                }

                await _context.SaveChangesAsync();
                msg = $"กู้คืน \"{existingDeleted.FullName}\" จากถังขยะเรียบร้อย";
            }
            else
            {
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // สร้างบัญชี User
                string baseUsername = $"emp{employee.EmployeeId}";
                string username = baseUsername;
                int suffix = 1;
                while (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == username))
                    username = $"{baseUsername}_{suffix++}";

                string password = employee.NationalId.Length >= 6
                    ? employee.NationalId.Substring(employee.NationalId.Length - 6)
                    : employee.NationalId;

                _context.Users.Add(new User
                {
                    Username = username,
                    Password = password,
                    FullName = employee.FullName,
                    Role = "Employee",
                    EmployeeId = employee.EmployeeId
                });
                await _context.SaveChangesAsync();

                msg = $"เพิ่ม \"{employee.FullName}\" เรียบร้อย — Login: {username} / รหัสผ่าน: {password}";
            }
            return ServiceResult.Ok(msg);
        }

        public async Task<ServiceResult> UpdateAsync(Employee employee)
        {
            if (await _context.Employees.AnyAsync(e => e.NationalId == employee.NationalId && e.EmployeeId != employee.EmployeeId))
                return ServiceResult.Fail("เลขบัตรประชาชนนี้มีอยู่ในระบบแล้ว");

            _context.Update(employee);

            var linkedUser = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employee.EmployeeId);
            if (linkedUser != null) linkedUser.FullName = employee.FullName;

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("แก้ไขข้อมูลเรียบร้อยแล้ว");
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return ServiceResult.Fail("ไม่พบพนักงาน");

            employee.IsDeleted = true;
            employee.DeletedAt = DateTime.Now;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);
            if (user != null)
            {
                user.IsDeleted = true;
                user.DeletedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Ok($"ลบ \"{employee.FullName}\" เรียบร้อยแล้ว");
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var employees = await GetFilteredAsync(null, null);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("รายชื่อพนักงาน");

            string[] headers = { "ลำดับ", "ชื่อ-นามสกุล", "แผนก", "ตำแหน่ง", "เลขประชาชน", "เบอร์โทร", "อีเมล" };
            for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];

            var headerRow = ws.Range(1, 1, 1, headers.Length);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#3b82f6");
            headerRow.Style.Font.FontColor = XLColor.White;

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

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}