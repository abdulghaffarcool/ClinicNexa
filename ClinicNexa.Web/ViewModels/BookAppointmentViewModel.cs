using ClinicNexa.Web.Models;

namespace ClinicNexa.Web.ViewModels;

public class BookAppointmentViewModel
{
    public int DoctorId { get; set; }

    public Doctor Doctor { get; set; } = null!;

    public DateTime Date { get; set; }

    public List<AppointmentSlot> AvailableSlots { get; set; } = new();

    public TimeSpan? SelectedStartTime { get; set; }

    public string? Reason { get; set; }
}