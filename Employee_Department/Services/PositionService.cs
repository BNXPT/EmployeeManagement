using Employee_Department.Data;
using Employee_Department.Models;
using Microsoft.EntityFrameworkCore;

namespace Employee_Department.Services
{
    public class PositionService : IPositionService
    {
        private readonly ApplicationDbContext _context;
        public PositionService(ApplicationDbContext context) => _context = context;

        public async Task<List<Position>> GetAllAsync()
            => await _context.Positions.OrderBy(p => p.PositionId).ToListAsync();

        public async Task<List<PositionInfo>> GetAllWithUsageAsync(string? search = null)
        {
            var query = _context.Positions.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.PositionName.Contains(search));

            return await query
                .Select(p => new PositionInfo
                {
                    PositionId = p.PositionId,
                    PositionName = p.PositionName,
                    UsageCount = _context.Employees.Count(e => e.Position == p.PositionName)
                })
                .OrderBy(x => x.PositionId)
                .ToListAsync();
        }

        public async Task<Position?> GetByIdAsync(int id) => await _context.Positions.FindAsync(id);

        public async Task<ServiceResult> CreateAsync(Position position)
        {
            if (await _context.Positions.AnyAsync(p => p.PositionName == position.PositionName))
                return ServiceResult.Fail("ชื่อตำแหน่งนี้มีอยู่แล้ว");

            var existing = await _context.Positions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.PositionName == position.PositionName && p.IsDeleted);

            if (existing != null)
            {
                existing.IsDeleted = false;
                existing.DeletedAt = null;
                await _context.SaveChangesAsync();
                return ServiceResult.Ok($"กู้คืนตำแหน่ง \"{existing.PositionName}\" จากถังขยะ");
            }

            _context.Positions.Add(position);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok($"เพิ่มตำแหน่ง \"{position.PositionName}\" เรียบร้อย");
        }

        public async Task<ServiceResult> UpdateAsync(Position position)
        {
            var old = await _context.Positions.AsNoTracking().FirstOrDefaultAsync(p => p.PositionId == position.PositionId);

            if (await _context.Positions.AnyAsync(p => p.PositionName == position.PositionName && p.PositionId != position.PositionId))
                return ServiceResult.Fail("ชื่อตำแหน่งนี้มีอยู่แล้ว");

            _context.Update(position);

            // ซิงค์ชื่อตำแหน่งใน Employees
            if (old != null && old.PositionName != position.PositionName)
            {
                var employees = await _context.Employees.Where(e => e.Position == old.PositionName).ToListAsync();
                foreach (var e in employees) e.Position = position.PositionName;
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("แก้ไขชื่อตำแหน่งเรียบร้อย");
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var pos = await _context.Positions.FindAsync(id);
            if (pos == null) return ServiceResult.Fail("ไม่พบตำแหน่ง");

            var usage = await _context.Employees.CountAsync(e => e.Position == pos.PositionName);
            if (usage > 0) return ServiceResult.Fail($"ลบไม่ได้: ใช้โดยพนักงาน {usage} คน");

            pos.IsDeleted = true;
            pos.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok($"ลบตำแหน่ง \"{pos.PositionName}\" เรียบร้อย");
        }
    }
}