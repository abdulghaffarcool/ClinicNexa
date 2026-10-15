using System.ComponentModel.DataAnnotations;

namespace ClinicNexa.Web.ViewModels;

public class PatientProfileViewModel
{
    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = null!;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = null!;

    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    [Display(Name = "Blood Group")]
    public string? BloodGroup { get; set; }
}