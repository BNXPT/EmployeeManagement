using Employee_Department.Data;
using Employee_Department.Models;
using Microsoft.EntityFrameworkCore;

namespace Employee_Department.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly ApplicationDbContext _context;
        public DepartmentService(ApplicationDbContext context) => _context = context;

        public async Task<List<Department>> GetAllAsync()
            => await _context.Departments.OrderBy(d => d.DepartmentId).ToListAsync();

        //ใหม่ — return ข้อมูลพร้อม count
        public async Task<List<DepartmentInfo>> GetAllWithCountsAsync()
        {
            return await _context.Departments
                .Select(d => new DepartmentInfo
                {
                    DepartmentId = d.DepartmentId,
                    DepartmentName = d.DepartmentName,
                    EmployeeCount = _context.Employees.Count(e => e.DepartmentId == d.DepartmentId),
                    ManagerCount = _context.Managers.Count(m => m.DepartmentId == d.DepartmentId),
                    HasManager = _context.Managers.Any(m => m.DepartmentId == d.DepartmentId)
                })
                .OrderBy(x => x.DepartmentId)
                .ToListAsync();
        }

        public async Task<Department?> GetByIdAsync(int id)
            => await _context.Departments.FindAsync(id);

        public async Task<ServiceResult> CreateAsync(Department department)
        {
            if (string.IsNullOrWhiteSpace(department.DepartmentName))
                return ServiceResult.Fail("กรุณาระบุชื่อแผนก");

            if (await _context.Departments.AnyAsync(d => d.DepartmentName == department.DepartmentName))
                return ServiceResult.Fail("ชื่อแผนกนี้มีอยู่แล้ว");

            var existingDeleted = await _context.Departments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.DepartmentName == department.DepartmentName && d.IsDeleted);

            if (existingDeleted != null)
            {
                existingDeleted.IsDeleted = false;
                existingDeleted.DeletedAt = null;
                await _context.SaveChangesAsync();
                return ServiceResult.Ok($"กู้คืนแผนก \"{existingDeleted.DepartmentName}\" จากถังขยะ");
            }

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok($"เพิ่มแผนก \"{department.DepartmentName}\" เรียบร้อย");
        }

        public async Task<ServiceResult> UpdateAsync(Department department)
        {
            if (await _context.Departments.AnyAsync(d => d.DepartmentName == department.DepartmentName && d.DepartmentId != department.DepartmentId))
                return ServiceResult.Fail("ชื่อแผนกนี้มีอยู่แล้ว");

            _context.Update(department);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("แก้ไขชื่อแผนกเรียบร้อย");
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return ServiceResult.Fail("ไม่พบแผนก");

            var hasEmployees = await _context.Employees.AnyAsync(e => e.DepartmentId == id);
            var hasManager = await _context.Managers.AnyAsync(m => m.DepartmentId == id);

            if (hasEmployees || hasManager)
                return ServiceResult.Fail($"ลบไม่ได้: \"{dept.DepartmentName}\" ยังมีพนักงานหรือหัวหน้าอยู่");

            dept.IsDeleted = true;
            dept.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok($"ลบแผนก \"{dept.DepartmentName}\" เรียบร้อย");
        }
    }
}