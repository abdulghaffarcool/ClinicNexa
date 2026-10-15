using System.ComponentModel.DataAnnotations;

namespace ClinicNexa.Web.ViewModels;

public class AdminDoctorCreateViewModel
{
    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = null!;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = null!;

    public string? Gender { get; set; }

    public string? Phone { get; set; }

    public string? Qualification { get; set; }

    [Range(0, 100)]
    public int ExperienceYears { get; set; }

    [Range(0, 1000000)]
    public decimal ConsultationFee { get; set; }

    public string? Bio { get; set; }

    public List<int> SelectedSpecialtyIds { get; set; } = new();

    public List<SpecialtyOption> Specialties { get; set; } = new();
}