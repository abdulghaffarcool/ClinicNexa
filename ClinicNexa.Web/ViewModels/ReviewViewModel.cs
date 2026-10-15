using System.ComponentModel.DataAnnotations;

namespace ClinicNexa.Web.ViewModels;

public class ReviewViewModel
{
    public int AppointmentId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}