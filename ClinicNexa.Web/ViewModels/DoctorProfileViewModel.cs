using System.ComponentModel.DataAnnotations;

namespace ClinicNexa.Web.ViewModels;

public class DoctorProfileViewModel
{
    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = null!;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = null!;

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

public class SpecialtyOption
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool Selected { get; set; }

}
