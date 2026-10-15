
using ClinicNexa.Web.Data;
using ClinicNexa.Web.Models;
using ClinicNexa.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicNexa.Web.Services;

public class AdminDoctorService : IAdminDoctorService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminDoctorService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<List<Doctor>> GetDoctorsAsync()
    {
        return await _context.Doctors
            .Include(d => d.DoctorSpecialties)
                .ThenInclude(ds => ds.Specialty)
            .Include(d => d.User)
            .AsNoTracking()
            .OrderBy(d => d.FirstName)
            .ThenBy(d => d.LastName)
            .ToListAsync();
    }

    public async Task<bool?> ToggleDoctorAsync(int id)
    {
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null)
            return null;

        doctor.IsActive = !doctor.IsActive;

        await _context.SaveChangesAsync();

        return doctor.IsActive;
    }

    public async Task<List<SpecialtyOption>> GetSpecialtyOptionsAsync(
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

    public async Task<AdminDoctorOperationResult> CreateAsync(
        AdminDoctorCreateViewModel model)
    {
        var existingUser = await _userManager
            .FindByEmailAsync(model.Email);

        if (existingUser != null)
        {
            return AdminDoctorOperationResult.Failure(
                emailAlreadyExists: true);
        }

        var selectedIds = model.SelectedSpecialtyIds
            .Distinct()
            .ToHashSet();

        var validSpecialtyCount = await _context.Specialties
            .CountAsync(s => selectedIds.Contains(s.Id));

        if (validSpecialtyCount != selectedIds.Count)
        {
            return AdminDoctorOperationResult.Failure(
                invalidSpecialtyIds: true);
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
            "Doctor");

        if (!roleResult.Succeeded)
        {
            var errors = roleResult.Errors
                .Select(e => e.Description)
                .ToList();

            await _userManager.DeleteAsync(user);

            return AdminDoctorOperationResult.Failure(errors);
        }

        var doctor = new Doctor
        {
            UserId = user.Id,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Gender = model.Gender,
            Phone = model.Phone,
            Qualification = model.Qualification,
            ExperienceYears = model.ExperienceYears,
            ConsultationFee = model.ConsultationFee,
            Bio = model.Bio,
            IsActive = true
        };

        foreach (var specialtyId in selectedIds)
        {
            doctor.DoctorSpecialties.Add(new DoctorSpecialty
            {
                SpecialtyId = specialtyId
            });
        }

        _context.Doctors.Add(doctor);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Best-effort cleanup if the profile save fails.
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

    public async Task<AdminDoctorEditViewModel?> GetEditModelAsync(
        int id)
    {
        var doctor = await _context.Doctors
            .Include(d => d.User)
            .Include(d => d.DoctorSpecialties)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null)
            return null;

        var selectedIds = doctor.DoctorSpecialties
            .Select(ds => ds.SpecialtyId)
            .ToHashSet();

        return new AdminDoctorEditViewModel
        {
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            Email = doctor.User.Email!,
            Gender = doctor.Gender,
            Phone = doctor.Phone,
            Qualification = doctor.Qualification,
            ExperienceYears = doctor.ExperienceYears,
            ConsultationFee = doctor.ConsultationFee,
            Bio = doctor.Bio,
            SelectedSpecialtyIds = selectedIds.ToList(),
            Specialties = await GetSpecialtyOptionsAsync(selectedIds)
        };
    }

    public async Task<AdminDoctorOperationResult?> EditAsync(
        int id,
        AdminDoctorEditViewModel model)
    {
        var doctor = await _context.Doctors
            .Include(d => d.DoctorSpecialties)
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor == null)
            return null;

        var selectedIds = model.SelectedSpecialtyIds
            .Distinct()
            .ToHashSet();

        var validSpecialtyCount = await _context.Specialties
            .CountAsync(s => selectedIds.Contains(s.Id));

        if (validSpecialtyCount != selectedIds.Count)
        {
            return AdminDoctorOperationResult.Failure(
                invalidSpecialtyIds: true);
        }

        doctor.FirstName = model.FirstName;
        doctor.LastName = model.LastName;
        doctor.Gender = model.Gender;
        doctor.Phone = model.Phone;
        doctor.Qualification = model.Qualification;
        doctor.ExperienceYears = model.ExperienceYears;
        doctor.ConsultationFee = model.ConsultationFee;
        doctor.Bio = model.Bio;

        doctor.User.FullName =
            $"{model.FirstName} {model.LastName}";

        var specialtiesToRemove = doctor.DoctorSpecialties
            .Where(ds => !selectedIds.Contains(ds.SpecialtyId))
            .ToList();

        foreach (var specialty in specialtiesToRemove)
        {
            doctor.DoctorSpecialties.Remove(specialty);
        }

        var existingIds = doctor.DoctorSpecialties
            .Select(ds => ds.SpecialtyId)
            .ToHashSet();

        foreach (var specialtyId in selectedIds.Except(existingIds))
        {
            doctor.DoctorSpecialties.Add(new DoctorSpecialty
            {
                DoctorId = doctor.Id,
                SpecialtyId = specialtyId
            });
        }

        await _context.SaveChangesAsync();

        return AdminDoctorOperationResult.Success();
    }
}
