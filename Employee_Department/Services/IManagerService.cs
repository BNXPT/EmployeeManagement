using Employee_Department.Models;

namespace Employee_Department.Services
{
    public interface IManagerService
    {
        Task<List<Manager>> GetFilteredAsync(string? search);
        Task<Manager?> GetByIdAsync(int id);
        Task<ServiceResult> CreateAsync(Manager manager);
        Task<ServiceResult> UpdateAsync(Manager manager);
        Task<ServiceResult> DeleteAsync(int id);
        Task<byte[]> ExportExcelAsync();
    }
}