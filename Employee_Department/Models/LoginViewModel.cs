using System.ComponentModel.DataAnnotations;

namespace Employee_Department.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "กรุณาระบุชื่อผู้ใช้")]
        [Display(Name = "ชื่อผู้ใช้")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุรหัสผ่าน")]
        [DataType(DataType.Password)]
        [Display(Name = "รหัสผ่าน")]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
