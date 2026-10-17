using ClinicNexa.Web.Data;
using ClinicNexa.Web.Models;
using ClinicNexa.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicNexa.Web.Controllers;

[Authorize(Roles = "Doctor")]
public class DoctorController : BaseController
{
    private readonly ApplicationDbContext _context;

    public DoctorController(ApplicationDbContext context
        , ILogger<BaseController> logger) : base(logger)
    {
        _context = context;
    }
    public IActionResult Dashboard()
    {
        return View();
    }

    #region Appointments

    [HttpGet]
    public async Task<IActionResult> Appointments()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
            return ApplicationError("Doctor profile not found.");

        var appointments = await _context.Appointments
            .Where(a => a.DoctorId == doctor.Id)
            .Include(a => a.Patient)
            .AsNoTracking()
            .ToListAsync();

        var upcoming = appointments
            .Where(a =>
                a.AppointmentDate.Date >= DateTime.Today &&
                a.Status != "Cancelled" &&
                a.Status != "Completed")
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .ToList();

        var history = appointments
            .Where(a =>
                a.AppointmentDate.Date < DateTime.Today ||
                a.Status == "Cancelled" ||
                a.Status == "Completed")
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .ToList();

        ViewBag.Upcoming = upcoming;
        ViewBag.History = history;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
            return ApplicationError("Doctor profile not found.");

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.DoctorId == doctor.Id);

        if (appointment == null)
        {
            return NotFound();
        }

        if (appointment.Status == "Pending")
        {
            appointment.Status = "Confirmed";
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Appointments));

    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
            return ApplicationError("Doctor profile not found.");

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.DoctorId == doctor.Id);

        if (appointment == null)
        {
            return NotFound();
        }

        if (appointment.Status == "Confirmed")
        {
            appointment.Status = "Completed";
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Appointments));

    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
            return ApplicationError("Doctor profile not found.");

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a =>
                a.Id == id &&
                a.DoctorId == doctor.Id);

        if (appointment == null)
        {
            return NotFound();
        }

        if (appointment.Status == "Completed")
        {
            TempData["Error"] =
                "Completed appointments cannot be cancelled.";

            return RedirectToAction(nameof(Appointments));
        }

        if (appointment.Status != "Cancelled")
        {
            appointment.Status = "Cancelled";
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Appointments));

    }

    #endregion

    #region Profile    

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var doctor = await _context.Doctors
            .Include(d => d.DoctorSpecialties)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
            return ApplicationError("Doctor profile not found.");

        var specialties = await _context.Specialties
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync();

        var selectedIds = doctor.DoctorSpecialties
            .Select(ds => ds.SpecialtyId)
            .ToList();

        var model = new DoctorProfileViewModel
        {
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            Gender = doctor.Gender,
            Phone = doctor.Phone,
            Qualification = doctor.Qualification,
            ExperienceYears = doctor.ExperienceYears,
            ConsultationFee = doctor.ConsultationFee,
            Bio = doctor.Bio,
            SelectedSpecialtyIds = selectedIds,

            Specialties = specialties
                .Select(s => new SpecialtyOption
                {
                    Id = s.Id,
                    Name = s.Name,
                    Selected = selectedIds.Contains(s.Id)
                })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(
    DoctorProfileViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var doctor = await _context.Doctors
            .Include(d => d.DoctorSpecialties)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
            return ApplicationError("Doctor profile not found.");

        if (!ModelState.IsValid)
        {
            var specialties = await _context.Specialties
                .OrderBy(s => s.Name)
                .AsNoTracking()
                .ToListAsync();

            model.Specialties = specialties
                .Select(s => new SpecialtyOption
                {
                    Id = s.Id,
                    Name = s.Name,
                    Selected = model.SelectedSpecialtyIds.Contains(s.Id)
                })
                .ToList();

            return View(model);
        }

        // Update profile
        doctor.FirstName = model.FirstName;
        doctor.LastName = model.LastName;
        doctor.Gender = model.Gender;
        doctor.Phone = model.Phone;
        doctor.Qualification = model.Qualification;
        doctor.ExperienceYears = model.ExperienceYears;
        doctor.ConsultationFee = model.ConsultationFee;
        doctor.Bio = model.Bio;

        // Update specialties
        _context.DoctorSpecialties.RemoveRange(
            doctor.DoctorSpecialties);

        var validSpecialtyIds = await _context.Specialties
            .Where(s => model.SelectedSpecialtyIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        foreach (var specialtyId in validSpecialtyIds)
        {
            _context.DoctorSpecialties.Add(new DoctorSpecialty
            {
                DoctorId = doctor.Id,
                SpecialtyId = specialtyId
            });
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Profile updated successfully.";

        return RedirectToAction(nameof(Profile));
    }

    #endregion
}