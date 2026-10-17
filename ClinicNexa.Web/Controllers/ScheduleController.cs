using System.Security.Claims;
using ClinicNexa.Web.Data;
using ClinicNexa.Web.Models;
using ClinicNexa.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicNexa.Web.Controllers;

[Authorize(Roles = "Doctor")]
public class ScheduleController : BaseController
{
    private readonly ApplicationDbContext _context;

    public ScheduleController(ApplicationDbContext context
        , ILogger<BaseController> logger) : base(logger)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Schedule()
    {
        var doctor = await GetCurrentDoctorAsync();

        if (doctor == null)
            return ApplicationError("Doctor profile not found.");

        var schedules = await _context.DoctorSchedules
            .Where(s => s.DoctorId == doctor.Id)
            .AsNoTracking()
            .ToListAsync();

        schedules = schedules
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToList();

        return View(schedules);
    }

    [HttpGet]
    public IActionResult AddSchedule()
    {
        return View(new ScheduleViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSchedule(ScheduleViewModel model)
    {
        if (model.StartTime >= model.EndTime)
        {
            ModelState.AddModelError(
                "",
                "Start time must be earlier than end time.");
        }

        var doctor = await GetCurrentDoctorAsync();

        if (doctor == null)
            return ApplicationError("Doctor profile not found.");

        var existingSchedules = await _context.DoctorSchedules
            .Where(s =>
                s.DoctorId == doctor.Id &&
                s.DayOfWeek == model.DayOfWeek)
            .ToListAsync();

        var overlaps = existingSchedules.Any(s =>
            model.StartTime < s.EndTime &&
            model.EndTime > s.StartTime);

        if (overlaps)
        {
            ModelState.AddModelError(
                "",
                "This schedule overlaps with an existing schedule.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var schedule = new DoctorSchedule
        {
            DoctorId = doctor.Id,
            DayOfWeek = model.DayOfWeek,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            AppointmentDurationMinutes =
                model.AppointmentDurationMinutes,
            IsAvailable = true
        };

        _context.DoctorSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Schedule added successfully.";

        return RedirectToAction(nameof(Schedule));
    }

    [HttpGet]
    public async Task<IActionResult> EditSchedule(int id)
    {
        var doctor = await GetCurrentDoctorAsync();

        if (doctor == null)
            return ApplicationError("Doctor profile not found.");

        var schedule = await _context.DoctorSchedules
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                s.DoctorId == doctor.Id);

        if (schedule == null)
        {
            return NotFound();
        }

        var model = new ScheduleViewModel
        {
            DayOfWeek = schedule.DayOfWeek,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            AppointmentDurationMinutes =
                schedule.AppointmentDurationMinutes
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSchedule(
        int id,
        ScheduleViewModel model)
    {
        if (model.StartTime >= model.EndTime)
        {
            ModelState.AddModelError(
                "",
                "Start time must be earlier than end time.");
        }

        var doctor = await GetCurrentDoctorAsync();

        if (doctor == null)
            return ApplicationError("Doctor profile not found.");

        var schedule = await _context.DoctorSchedules
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                s.DoctorId == doctor.Id);

        if (schedule == null)
        {
            return NotFound();
        }

        var existingSchedules = await _context.DoctorSchedules
            .Where(s =>
                s.DoctorId == doctor.Id &&
                s.DayOfWeek == model.DayOfWeek &&
                s.Id != id)
            .ToListAsync();

        var overlaps = existingSchedules.Any(s =>
            model.StartTime < s.EndTime &&
            model.EndTime > s.StartTime);

        if (overlaps)
        {
            ModelState.AddModelError(
                "",
                "This schedule overlaps with an existing schedule.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        schedule.DayOfWeek = model.DayOfWeek;
        schedule.StartTime = model.StartTime;
        schedule.EndTime = model.EndTime;
        schedule.AppointmentDurationMinutes =
            model.AppointmentDurationMinutes;

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Schedule updated successfully.";

        return RedirectToAction(nameof(Schedule));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSchedule(int id)
    {
        var doctor = await GetCurrentDoctorAsync();

        if (doctor == null)
            return ApplicationError("Doctor profile not found.");

        var schedule = await _context.DoctorSchedules
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                s.DoctorId == doctor.Id);

        if (schedule == null)
        {
            return NotFound();
        }

        schedule.IsAvailable = !schedule.IsAvailable;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            $"Schedule {(schedule.IsAvailable ? "activated" : "deactivated")} successfully.";

        return RedirectToAction(nameof(Schedule));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        var doctor = await GetCurrentDoctorAsync();

        if (doctor == null)
            return ApplicationError("Doctor profile not found.");

        var schedule = await _context.DoctorSchedules
            .FirstOrDefaultAsync(s =>
                s.Id == id &&
                s.DoctorId == doctor.Id);

        if (schedule == null)
        {
            return NotFound();
        }

        _context.DoctorSchedules.Remove(schedule);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Schedule deleted successfully.";

        return RedirectToAction(nameof(Schedule));
    }

    private async Task<Doctor?> GetCurrentDoctorAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return await _context.Doctors
            .FirstOrDefaultAsync(d => d.UserId == userId);
    }
}