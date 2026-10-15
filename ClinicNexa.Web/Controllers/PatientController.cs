using ClinicNexa.Web.Data;
using ClinicNexa.Web.Models;
using ClinicNexa.Web.Services;
using ClinicNexa.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ClinicNexa.Web.Filters;

namespace ClinicNexa.Web.Controllers;

[Authorize(Roles = "Patient")]
[ActivePatient]
public class PatientController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PatientController(
        ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Appointments()
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
            .Include(a => a.Review)
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

    #region Profile

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return BadRequest("Patient profile not found.");

        var model = new PatientProfileViewModel
        {
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Phone = patient.Phone,
            Address = patient.Address,
            BloodGroup = patient.BloodGroup
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(
    PatientProfileViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return BadRequest("Patient profile not found.");

        if (!ModelState.IsValid)
            return View(model);

        patient.FirstName = model.FirstName;
        patient.LastName = model.LastName;
        patient.DateOfBirth = model.DateOfBirth;
        patient.Gender = model.Gender;
        patient.Phone = model.Phone;
        patient.Address = model.Address;
        patient.BloodGroup = model.BloodGroup;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Profile updated successfully.";

        return RedirectToAction(nameof(Profile));
    }

    #endregion

    #region Helper Functions

    private async Task<Patient?> GetActivePatientAsync()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return null;

        return await _context.Patients
            .FirstOrDefaultAsync(p =>
                p.UserId == userId &&
                p.IsActive);
    }

    #endregion
}