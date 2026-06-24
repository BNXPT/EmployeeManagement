using Employee_Department.Models;

namespace Employee_Department.Services
{
    public interface IPositionService
    {
        Task<List<Position>> GetAllAsync();
        Task<List<PositionInfo>> GetAllWithUsageAsync(string? search = null);
        Task<Position?> GetByIdAsync(int id);
        Task<ServiceResult> CreateAsync(Position position);
        Task<ServiceResult> UpdateAsync(Position position);
        Task<ServiceResult> DeleteAsync(int id);
    }

    public class PositionInfo
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; } = "";
        public int UsageCount { get; set; }
    }
}