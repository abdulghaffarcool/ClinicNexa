using Microsoft.AspNetCore.Identity;

namespace ClinicNexa.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    public Doctor? Doctor { get; set; }

    public Patient? Patient { get; set; }
}