using ClinicNexa.Web.Data;
using ClinicNexa.Web.Filters;
using ClinicNexa.Web.Models;
using ClinicNexa.Web.Services;
using ClinicNexa.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicNexa.Web.Controllers;

[Authorize(Roles = "Patient")]
[ActivePatient]
public class AppointmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(
        ApplicationDbContext context,
        IAppointmentService appointmentService)
    {
        _context = context;
        _appointmentService = appointmentService;
    }

    [HttpGet]
    public async Task<IActionResult> Book(
        int doctorId,
        DateTime? date)
    {
        var selectedDate = date?.Date ?? DateTime.Today;

        var doctor = await _context.Doctors
            .Where(d => d.Id == doctorId && d.IsActive)
            .Include(d => d.DoctorSpecialties)
                .ThenInclude(ds => ds.Specialty)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (doctor == null)
        {
            return NotFound();
        }

        var slots = await _appointmentService
            .GetAvailableSlotsAsync(doctorId, selectedDate);
        
        var model = new BookAppointmentViewModel
        {
            DoctorId = doctor.Id,
            Doctor = doctor,
            Date = selectedDate,
            AvailableSlots = slots
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookAppointmentViewModel model)
    {
        // Load doctor from database
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d =>
                d.Id == model.DoctorId &&
                d.IsActive);

        if (doctor == null)
        {
            return NotFound();
        }

        // We don't need Doctor entity validation from the POST
        ModelState.Remove("Doctor");

        if (!model.SelectedStartTime.HasValue)
        {
            ModelState.AddModelError(
                "SelectedStartTime",
                "Please select an appointment slot.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableSlots =
                await _appointmentService.GetAvailableSlotsAsync(
                    model.DoctorId,
                    model.Date);

            return View(model);
        }        

        // Find logged-in patient's profile
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
        {
            return BadRequest("Patient profile not found.");
        }        

        var schedules = await _context.DoctorSchedules
            .Where(s =>
                s.DoctorId == doctor.Id &&
                s.DayOfWeek == model.Date.DayOfWeek &&
                s.IsAvailable)
            .ToListAsync();

        var selectedStart = model.SelectedStartTime.Value;

        var schedule = schedules.FirstOrDefault(s =>
            s.StartTime <= selectedStart &&
            s.EndTime >= selectedStart.Add(
                TimeSpan.FromMinutes(s.AppointmentDurationMinutes)));

        if (schedule == null)
        {
            ModelState.AddModelError(
                "",
                "The selected time is outside the doctor's schedule.");

            model.AvailableSlots =
                await _appointmentService.GetAvailableSlotsAsync(
                    doctor.Id,
                    model.Date);

            return View(model);
        }

        var endTime = model.SelectedStartTime.Value.Add(
            TimeSpan.FromMinutes(schedule.AppointmentDurationMinutes));

        // Prevent double booking
        var alreadyBooked = await _context.Appointments.AnyAsync(a =>
            a.DoctorId == model.DoctorId &&
            a.AppointmentDate.Date == model.Date.Date &&
            a.StartTime == model.SelectedStartTime.Value &&
            a.Status != "Cancelled");

        if (alreadyBooked)
        {
            ModelState.AddModelError(
                "",
                "Sorry, this appointment slot has already been booked.");

            model.AvailableSlots =
                await _appointmentService.GetAvailableSlotsAsync(
                    doctor.Id,
                    model.Date);

            return View(model);
        }

        var appointment = new Appointment
        {
            DoctorId = model.DoctorId,
            PatientId = patient.Id,
            AppointmentDate = model.Date.Date,
            StartTime = model.SelectedStartTime.Value,
            EndTime = endTime,
            Status = "Pending",
            Reason = model.Reason
        };

        try
        {
            _context.Appointments.Add(appointment);

            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Error"] =
                "Sorry, this appointment slot was just booked by another patient.";

            return View(model);
        }

        return RedirectToAction(nameof(MyAppointments));
        
    }    

    [HttpGet]
    public async Task<IActionResult> MyAppointments()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
        {
            return BadRequest("Patient profile not found.");
        }

        var appointments = await _context.Appointments
            .Where(a => a.PatientId == patient.Id)
            .Include(a => a.Doctor)
            .AsNoTracking()
            .ToListAsync();

        // SQLLite don't support Timespan ordering
        appointments = appointments
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .ToList();

        return View(appointments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
        {
            return BadRequest("Patient profile not found.");
        }

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.PatientId == patient.Id);

        if (appointment == null)
        {
            return NotFound();
        }

        if (appointment.Status == "Completed")
        {
            TempData["Error"] = "Completed appointments cannot be cancelled.";
            return RedirectToAction(nameof(MyAppointments));
        }

        if (appointment.Status == "Cancelled")
        {
            return RedirectToAction(nameof(MyAppointments));
        }

        appointment.Status = "Cancelled";

        await _context.SaveChangesAsync();

        TempData["Success"] = "Appointment cancelled successfully.";

        return RedirectToAction(nameof(MyAppointments));
    }
}