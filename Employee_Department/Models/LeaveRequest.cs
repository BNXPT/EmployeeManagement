using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Employee_Department.Models
{
    public class LeaveRequest : ISoftDelete
    {
        [Key]
        public int LeaveRequestId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "กรุณาเลือกหรือระบุประเภทการลา")]
        [StringLength(50)]
        [Display(Name = "ประเภทการลา")]
        public string LeaveType { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุเหตุผล")]
        [StringLength(500)]
        [Display(Name = "เหตุผล")]
        public string Reason { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาเลือกวันที่เริ่ม")]
        [DataType(DataType.Date)]
        [Display(Name = "วันที่เริ่มลา")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "กรุณาเลือกวันที่สิ้นสุด")]
        [DataType(DataType.Date)]
        [Display(Name = "วันที่สิ้นสุด")]
        public DateTime EndDate { get; set; }

        [Display(Name = "จำนวนวัน")]
        public int DaysCount => Math.Max(1, (EndDate.Date - StartDate.Date).Days + 1);

        [Required]
        [StringLength(20)]
        [Display(Name = "สถานะ")]
        public string Status { get; set; } = "Pending"; // Pending | Approved | Rejected

        [Required]
        [Display(Name = "วันที่ยื่นคำขอ")]
        public DateTime RequestedAt { get; set; } = DateTime.Now;

        [Display(Name = "วันที่ตอบกลับ")]
        public DateTime? RespondedAt { get; set; }

        [StringLength(500)]
        [Display(Name = "ความเห็นจากหัวหน้า")]
        public string? ManagerComment { get; set; }

        [Required]
        [StringLength(64)]
        public string ApprovalToken { get; set; } = Guid.NewGuid().ToString("N");

        [Display(Name = "หัวหน้าผู้อนุมัติ")]
        public int? RespondedByManagerId { get; set; }

        // Navigation
        [ForeignKey(nameof(EmployeeId))]
        public virtual Employee? Employee { get; set; }

        [ForeignKey(nameof(RespondedByManagerId))]
        public virtual Manager? RespondedByManager { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
