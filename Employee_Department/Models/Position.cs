using System.ComponentModel.DataAnnotations;

namespace Employee_Department.Models
{
    public class Position : ISoftDelete
    {
        [Key]
        public int PositionId { get; set; }

        [Required(ErrorMessage = "กรุณาระบุชื่อตำแหน่ง")]
        [StringLength(100)]
        [Display(Name = "ชื่อตำแหน่ง")]
        public string PositionName { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}