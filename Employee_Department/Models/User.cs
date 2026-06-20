using System.ComponentModel.DataAnnotations;

namespace Employee_Department.Models
{
    public class User : ISoftDelete
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "ชื่อผู้ใช้")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [Display(Name = "รหัสผ่าน")]
        public string Password { get; set; } = string.Empty; // Plain or hashed

        [Required]
        [StringLength(100)]
        [Display(Name = "ชื่อ-นามสกุล")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "บทบาท")]
        public string Role { get; set; } = "Employee"; // Admin, Manager, Employee

        public int? EmployeeId { get; set; }
        public int? ManagerId { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
