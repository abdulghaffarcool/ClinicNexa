
using ClinicNexa.Web.Data;
using ClinicNexa.Web.Models;
using ClinicNexa.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicNexa.Web.Services;

public class AdminPatientService : IAdminPatientService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminPatientService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<List<Patient>> GetPatientsAsync()
    {
        return await _context.Patients
            .Include(p => p.User)
            .Include(p => p.Appointments)
            .AsNoTracking()
            .OrderBy(p => p.FirstName)
            .ThenBy(p => p.LastName)
            .ToListAsync();
    }

    public async Task<Patient?> GetPatientDetailsAsync(int id)
    {
        return await _context.Patients
            .Include(p => p.User)
            .Include(p => p.Appointments)
                .ThenInclude(a => a.Doctor)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<AdminDoctorOperationResult> CreateAsync(
        AdminPatientCreateViewModel model)
    {
        var existingUser = await _userManager
            .FindByEmailAsync(model.Email);

        if (existingUser != null)
        {
            return AdminDoctorOperationResult.Failure(
                emailAlreadyExists: true);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = $"{model.FirstName} {model.LastName}"
        };

        var createResult = await _userManager.CreateAsync(
            user,
            model.Password);

        if (!createResult.Succeeded)
        {
            return AdminDoctorOperationResult.Failure(
                createResult.Errors.Select(e => e.Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            "Patient");

        if (!roleResult.Succeeded)
        {
            var errors = roleResult.Errors
                .Select(e => e.Description)
                .ToList();

            await _userManager.DeleteAsync(user);

            return AdminDoctorOperationResult.Failure(errors);
        }

        var patient = new Patient
        {
            UserId = user.Id,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DateOfBirth = model.DateOfBirth,
            Gender = model.Gender,
            Phone = model.Phone,
            Address = model.Address,
            BloodGroup = model.BloodGroup,
            IsActive = true
        };

        _context.Patients.Add(patient);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Best-effort cleanup if the patient profile cannot be saved.
            try
            {
                await _userManager.DeleteAsync(user);
            }
            catch
            {
                // Preserve the original database exception.
            }

            throw;
        }

        return AdminDoctorOperationResult.Success();
    }

    public async Task<AdminPatientEditViewModel?> GetEditModelAsync(
        int id)
    {
        var patient = await _context.Patients
            .Include(p => p.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null)
            return null;

        return new AdminPatientEditViewModel
        {
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            Email = patient.User.Email!,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Phone = patient.Phone,
            Address = patient.Address,
            BloodGroup = patient.BloodGroup
        };
    }

    public async Task<AdminDoctorOperationResult?> EditAsync(
        int id,
        AdminPatientEditViewModel model)
    {
        var patient = await _context.Patients
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null)
            return null;

        patient.FirstName = model.FirstName;
        patient.LastName = model.LastName;
        patient.DateOfBirth = model.DateOfBirth;
        patient.Gender = model.Gender;
        patient.Phone = model.Phone;
        patient.Address = model.Address;
        patient.BloodGroup = model.BloodGroup;

        patient.User.FullName =
            $"{model.FirstName} {model.LastName}";

        await _context.SaveChangesAsync();

        return AdminDoctorOperationResult.Success();
    }

    public async Task<bool?> TogglePatientAsync(int id)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null)
            return null;

        patient.IsActive = !patient.IsActive;

        await _context.SaveChangesAsync();

        return patient.IsActive;
    }
}
