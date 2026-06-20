using System.ComponentModel.DataAnnotations;

namespace Employee_Department.Models
{
    public class Department : ISoftDelete
    {
        [Key]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "กรุณาระบุชื่อแผนก")]
        [StringLength(100)]
        [Display(Name = "ชื่อแผนก")]
        public string DepartmentName { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
        public virtual Manager? Manager { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
