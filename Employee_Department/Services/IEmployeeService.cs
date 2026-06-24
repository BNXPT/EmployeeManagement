using Employee_Department.Models;

namespace Employee_Department.Services
{
    public interface IEmployeeService
    {
        Task<List<Employee>> GetFilteredAsync(string? search, int? departmentId);
        Task<Employee?> GetByIdAsync(int id);
        Task<ServiceResult> CreateAsync(Employee employee);
        Task<ServiceResult> UpdateAsync(Employee employee);
        Task<ServiceResult> DeleteAsync(int id);
        Task<byte[]> ExportExcelAsync();
    }
}