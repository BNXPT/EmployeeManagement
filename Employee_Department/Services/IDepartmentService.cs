using Employee_Department.Models;

namespace Employee_Department.Services
{
    public interface IDepartmentService
    {
        Task<List<Department>> GetAllAsync();
        Task<List<DepartmentInfo>> GetAllWithCountsAsync();
        Task<Department?> GetByIdAsync(int id);
        Task<ServiceResult> CreateAsync(Department department);
        Task<ServiceResult> UpdateAsync(Department department);
        Task<ServiceResult> DeleteAsync(int id);
    }

    public class DepartmentInfo
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = "";
        public int EmployeeCount { get; set; }
        public int ManagerCount { get; set; }
        public bool HasManager { get; set; }
    }
}