namespace Project01_movie_lease_system.Models;
using System.ComponentModel.DataAnnotations;
public class UpdateMemberPassword
{
    [Required(ErrorMessage = "請輸入目前密碼")]
    public string OldPassword { get; set; }
    [Required(ErrorMessage = "請輸入新密碼")]
    public string NewPassword { get; set; }
    [Compare("NewPassword", ErrorMessage = "新密碼與確認密碼不相符")]
    public string ConfirmNewPassword { get; set; }
}
