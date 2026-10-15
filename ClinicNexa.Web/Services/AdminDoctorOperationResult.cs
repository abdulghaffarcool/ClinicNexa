
namespace ClinicNexa.Web.Services;

public sealed class AdminDoctorOperationResult
{
    public bool Succeeded { get; init; }

    public bool EmailAlreadyExists { get; init; }

    public bool InvalidSpecialtyIds { get; init; }

    public IReadOnlyList<string> Errors { get; init; }
        = Array.Empty<string>();

    public static AdminDoctorOperationResult Success()
    {
        return new AdminDoctorOperationResult
        {
            Succeeded = true
        };
    }

    public static AdminDoctorOperationResult Failure(
    IEnumerable<string>? errors = null,
    bool emailAlreadyExists = false,
    bool invalidSpecialtyIds = false)
    {
        return new AdminDoctorOperationResult
        {
            Succeeded = false,
            EmailAlreadyExists = emailAlreadyExists,
            InvalidSpecialtyIds = invalidSpecialtyIds,
            Errors = errors is null
                ? Array.Empty<string>()
                : errors.ToList()
        };
    }
}
