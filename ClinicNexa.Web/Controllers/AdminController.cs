using ClinicNexa.Web.Data;
using ClinicNexa.Web.Models;
using ClinicNexa.Web.Services;
using ClinicNexa.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicNexa.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAdminDoctorService _adminDoctorService;
    private readonly IAdminPatientService _adminPatientService;

    public AdminController(ApplicationDbContext context
        , UserManager<ApplicationUser> userManager
        , IAdminDoctorService adminDoctorService
        , IAdminPatientService adminPatientService)
    {
        _context = context;
        _userManager = userManager;
        _adminDoctorService = adminDoctorService;
        _adminPatientService = adminPatientService;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.TotalDoctors =
            await _context.Doctors.CountAsync();

        ViewBag.ActiveDoctors =
            await _context.Doctors.CountAsync(d => d.IsActive);

        ViewBag.TotalPatients =
            await _context.Patients.CountAsync();

        ViewBag.TotalAppointments =
            await _context.Appointments.CountAsync();

        ViewBag.PendingAppointments =
            await _context.Appointments
                .CountAsync(a => a.Status == "Pending");

        ViewBag.CompletedAppointments =
            await _context.Appointments
                .CountAsync(a => a.Status == "Completed");

        ViewBag.CancelledAppointments =
            await _context.Appointments
                .CountAsync(a => a.Status == "Cancelled");

        ViewBag.TotalReviews =
            await _context.Reviews.CountAsync();

        return View();
    }


    #region Doctors

    [HttpGet]
    public async Task<IActionResult> Doctors()
    {
        var doctors = await _adminDoctorService.GetDoctorsAsync();

        return View(doctors);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDoctor(int id)
    {
        var isActive = await _adminDoctorService.ToggleDoctorAsync(id);

        if (isActive == null)
            return NotFound();

        TempData["Success"] =
            $"Doctor {(isActive.Value ? "activated" : "deactivated")} successfully.";

        return RedirectToAction(nameof(Doctors));
    }

    [HttpGet]
    public async Task<IActionResult> CreateDoctor()
    {
        var model = new AdminDoctorCreateViewModel
        {
            Specialties =
                await _adminDoctorService.GetSpecialtyOptionsAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDoctor(
        AdminDoctorCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Specialties =
                await _adminDoctorService.GetSpecialtyOptionsAsync(
                    model.SelectedSpecialtyIds);

            return View(model);
        }

        var result = await _adminDoctorService.CreateAsync(model);

        if (!result.Succeeded)
        {
            if (result.EmailAlreadyExists)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An account with this email already exists.");
            }

            if (result.InvalidSpecialtyIds)
            {
                ModelState.AddModelError(
                    nameof(model.SelectedSpecialtyIds),
                    "One or more selected specialties are invalid.");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            model.Specialties =
                await _adminDoctorService.GetSpecialtyOptionsAsync(
                    model.SelectedSpecialtyIds);

            return View(model);
        }

        TempData["Success"] = "Doctor created successfully.";

        return RedirectToAction(nameof(Doctors));
    }

    [HttpGet]
    public async Task<IActionResult> EditDoctor(int id)
    {
        var model = await _adminDoctorService.GetEditModelAsync(id);

        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDoctor(
        int id,
        AdminDoctorEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Specialties =
                await _adminDoctorService.GetSpecialtyOptionsAsync(
                    model.SelectedSpecialtyIds);

            return View(model);
        }

        var result = await _adminDoctorService.EditAsync(id, model);

        if (result == null)
            return NotFound();

        if (!result.Succeeded)
        {
            if (result.InvalidSpecialtyIds)
            {
                ModelState.AddModelError(
                    nameof(model.SelectedSpecialtyIds),
                    "One or more selected specialties are invalid.");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            model.Specialties =
                await _adminDoctorService.GetSpecialtyOptionsAsync(
                    model.SelectedSpecialtyIds);

            return View(model);
        }

        TempData["Success"] = "Doctor updated successfully.";

        return RedirectToAction(nameof(Doctors));
    }

    #endregion


    #region Patients

    [HttpGet]
    public async Task<IActionResult> Patients()
    {
        var patients = await _adminPatientService.GetPatientsAsync();

        return View(patients);
    }

    [HttpGet]
    public async Task<IActionResult> PatientDetails(int id)
    {
        var patient = await _adminPatientService.GetPatientDetailsAsync(id);

        if (patient == null)
            return NotFound();

        return View(patient);
    }

    [HttpGet]
    public IActionResult CreatePatient()
    {
        return View(new AdminPatientCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePatient(
        AdminPatientCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _adminPatientService.CreateAsync(model);

        if (!result.Succeeded)
        {
            if (result.EmailAlreadyExists)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An account with this email already exists.");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        TempData["Success"] =
            $"Patient {model.FirstName} {model.LastName} created successfully.";

        return RedirectToAction(nameof(Patients));
    }

    [HttpGet]
    public async Task<IActionResult> EditPatient(int id)
    {
        var model = await _adminPatientService.GetEditModelAsync(id);

        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPatient(
        int id,
        AdminPatientEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _adminPatientService.EditAsync(id, model);

        if (result == null)
            return NotFound();

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        TempData["Success"] =
            $"Patient {model.FirstName} {model.LastName} updated successfully.";

        return RedirectToAction(nameof(Patients));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePatient(int id)
    {
        var isActive = await _adminPatientService.TogglePatientAsync(id);

        if (isActive == null)
            return NotFound();

        TempData["Success"] =
            $"Patient {(isActive.Value ? "activated" : "deactivated")} successfully.";

        return RedirectToAction(nameof(Patients));
    }

    #endregion


    #region Appointments

    [HttpGet]
    public async Task<IActionResult> Appointments()
    {
        var appointments = await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Include(a => a.Review)
            .AsNoTracking()
            .ToListAsync();

        appointments = appointments
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .ToList();

        return View(appointments);
    }

    #endregion

    #region Users

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .ToListAsync();

        var model = new List<AdminUserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            model.Add(new AdminUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Roles = roles
            });
        }

        return View(model);
    }    

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string type, int id)
    {
        if (type != "Doctor" && type != "Patient")
            return BadRequest();

        string? userId;
        string? email;

        if (type == "Doctor")
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound();

            userId = doctor.UserId;
            email = doctor.User.Email;
        }
        else
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
                return NotFound();

            userId = patient.UserId;
            email = patient.User.Email;
        }

        if (string.IsNullOrEmpty(userId))
            return NotFound();

        var model = new AdminResetPasswordViewModel
        {
            AccountType = type,
            EntityId = id,
            Email = email ?? string.Empty
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
    string type,
    int id,
    AdminResetPasswordViewModel model)
    {
        if (type != "Doctor" && type != "Patient")
            return BadRequest();

        if (!ModelState.IsValid)
        {
            return await PrepareResetPasswordViewAsync(type, id, model);
        }

        ApplicationUser? user;

        if (type == "Doctor")
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound();

            user = doctor.User;
        }
        else
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
                return NotFound();

            user = patient.User;
        }

        // Generate a secure Identity reset token.
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var result = await _userManager.ResetPasswordAsync(
            user,
            token,
            model.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    nameof(model.NewPassword),
                    error.Description);
            }

            return await PrepareResetPasswordViewAsync(type, id, model);
        }

        TempData["Success"] =
            $"Password reset successfully for {user.Email}.";

        return RedirectToAction(
            type == "Doctor" ? nameof(Doctors) : nameof(Patients));
    }

    #endregion

    #region Helper Methods

    private async Task<List<SpecialtyOption>> GetSpecialtyOptionsAsync(
    IEnumerable<int>? selectedIds = null)
    {
        var selected = selectedIds?.ToHashSet()
                       ?? new HashSet<int>();

        var specialties = await _context.Specialties
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();

        return specialties
            .Select(s => new SpecialtyOption
            {
                Id = s.Id,
                Name = s.Name,
                Selected = selected.Contains(s.Id)
            })
            .ToList();
    }

    private async Task<IActionResult> PrepareResetPasswordViewAsync(
    string type,
    int id,
    AdminResetPasswordViewModel model)
    {
        string? email;

        if (type == "Doctor")
        {
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (doctor == null)
                return NotFound();

            email = doctor.User.Email;
        }
        else
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
                return NotFound();

            email = patient.User.Email;
        }

        model.AccountType = type;
        model.EntityId = id;
        model.Email = email ?? string.Empty;

        return View("ResetPassword", model);
    }
    #endregion

}