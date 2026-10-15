using ClinicNexa.Web.ViewModels;

namespace ClinicNexa.Web.Services;

public interface IAppointmentService
{
    Task<List<AppointmentSlot>> GetAvailableSlotsAsync(
        int doctorId,
        DateTime date);
}