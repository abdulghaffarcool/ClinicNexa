
using ClinicNexa.Web.Models;
using ClinicNexa.Web.ViewModels;

namespace ClinicNexa.Web.Services;

public interface IAdminDoctorService
{
    Task<List<Doctor>> GetDoctorsAsync();

    Task<bool?> ToggleDoctorAsync(int id);

    Task<List<SpecialtyOption>> GetSpecialtyOptionsAsync(
        IEnumerable<int>? selectedIds = null);

    Task<AdminDoctorOperationResult> CreateAsync(
        AdminDoctorCreateViewModel model);

    Task<AdminDoctorEditViewModel?> GetEditModelAsync(int id);

    Task<AdminDoctorOperationResult?> EditAsync(
        int id,
        AdminDoctorEditViewModel model);
}
