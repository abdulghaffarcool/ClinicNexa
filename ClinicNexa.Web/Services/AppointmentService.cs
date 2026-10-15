using ClinicNexa.Web.Data;
using ClinicNexa.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ClinicNexa.Web.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;

    public AppointmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AppointmentSlot>> GetAvailableSlotsAsync(
        int doctorId,
        DateTime date)
    {
        var dayOfWeek = date.DayOfWeek;

        var schedules = await _context.DoctorSchedules
            .Where(s =>
                s.DoctorId == doctorId &&
                s.DayOfWeek == dayOfWeek &&
                s.IsAvailable)
            .AsNoTracking()
            .ToListAsync();

        var bookedAppointments = await _context.Appointments
            .Where(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDate.Date == date.Date &&
                a.Status != "Cancelled")
            .AsNoTracking()
            .ToListAsync();

        var availableSlots = new List<AppointmentSlot>();

        foreach (var schedule in schedules)
        {
            var currentTime = schedule.StartTime;

            while (currentTime.Add(
                       TimeSpan.FromMinutes(
                           schedule.AppointmentDurationMinutes))
                   <= schedule.EndTime)
            {
                var endTime = currentTime.Add(
                    TimeSpan.FromMinutes(
                        schedule.AppointmentDurationMinutes));

                var isBooked = bookedAppointments.Any(a =>
                    a.StartTime == currentTime &&
                    a.EndTime == endTime);

                if (!isBooked)
                {
                    availableSlots.Add(new AppointmentSlot
                    {
                        StartTime = currentTime,
                        EndTime = endTime
                    });
                }

                currentTime = endTime;
            }
        }

        return availableSlots;
    }
}