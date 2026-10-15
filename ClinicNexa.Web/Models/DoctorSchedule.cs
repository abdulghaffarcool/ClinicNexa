namespace ClinicNexa.Web.Models;

public class DoctorSchedule
{
    public int Id { get; set; }

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public int AppointmentDurationMinutes { get; set; } = 30;

    public bool IsAvailable { get; set; } = true;
}