using System.ComponentModel.DataAnnotations;

namespace ClinicNexa.Web.ViewModels;

public class AdminResetPasswordViewModel
{
    public string? AccountType { get; set; } = null!;

    public int EntityId { get; set; }

    public string? Email { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = null!;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword))]
    [Display(Name = "Confirm New Password")]
    public string ConfirmPassword { get; set; } = null!;
}