namespace ClinicNexa.Web.ViewModels;

public class AdminUserViewModel
{
    public string Id { get; set; } = null!;

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public IList<string> Roles { get; set; } = new List<string>();
}