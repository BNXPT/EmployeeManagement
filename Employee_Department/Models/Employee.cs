using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Employee_Department.Models
{
    public class Employee : ISoftDelete
    {
        [Key]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "กรุณาระบุชื่อ-นามสกุล")]
        [StringLength(100)]
        [Display(Name = "ชื่อ-นามสกุล")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุเลขบัตรประชาชน")]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "เลขบัตรประชาชนต้องมี 13 หลัก")]
        [Display(Name = "เลขบัตรประชาชน")]
        public string NationalId { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาเลือกแผนก")]
        [Display(Name = "แผนก")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "กรุณาระบุตำแหน่ง")]
        [StringLength(100)]
        [Display(Name = "ตำแหน่ง")]
        public string Position { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุเบอร์โทรศัพท์")]
        [StringLength(15)]
        [Display(Name = "เบอร์โทรศัพท์")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุอีเมล")]
        [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
        [StringLength(150)]
        [Display(Name = "อีเมล")]
        public string Email { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey(nameof(DepartmentId))]
        public virtual Department? Department { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
