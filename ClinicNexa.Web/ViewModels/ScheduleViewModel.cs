using System.ComponentModel.DataAnnotations;

namespace ClinicNexa.Web.ViewModels;

public class ScheduleViewModel
{
    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    [Range(15, 120)]
    public int AppointmentDurationMinutes { get; set; } = 30;

}
