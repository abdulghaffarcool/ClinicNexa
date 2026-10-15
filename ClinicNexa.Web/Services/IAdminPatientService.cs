
using ClinicNexa.Web.Models;
using ClinicNexa.Web.ViewModels;

namespace ClinicNexa.Web.Services;

public interface IAdminPatientService
{
    Task<List<Patient>> GetPatientsAsync();

    Task<Patient?> GetPatientDetailsAsync(int id);

    Task<AdminDoctorOperationResult> CreateAsync(
        AdminPatientCreateViewModel model);

    Task<AdminPatientEditViewModel?> GetEditModelAsync(int id);

    Task<AdminDoctorOperationResult?> EditAsync(
        int id,
        AdminPatientEditViewModel model);

    Task<bool?> TogglePatientAsync(int id);
}
