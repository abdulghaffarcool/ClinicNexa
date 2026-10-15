using ClinicNexa.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ClinicNexa.Web.Filters;

public class ActivePatientAttribute : TypeFilterAttribute
{
    public ActivePatientAttribute()
        : base(typeof(ActivePatientFilter))
    {
    }
}

public class ActivePatientFilter : IAsyncAuthorizationFilter
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<Models.ApplicationUser> _userManager;
    private readonly SignInManager<Models.ApplicationUser> _signInManager;
    private readonly ITempDataDictionaryFactory _tempDataFactory;

    public ActivePatientFilter(
        ApplicationDbContext context,
        UserManager<Models.ApplicationUser> userManager,
        SignInManager<Models.ApplicationUser> signInManager,
        ITempDataDictionaryFactory tempDataFactory)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _tempDataFactory = tempDataFactory;
    }

    public async Task OnAuthorizationAsync(
        AuthorizationFilterContext context)
    {
        var userId = _userManager.GetUserId(context.HttpContext.User);

        if (userId == null)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var patient = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.UserId == userId);

        if (patient == null)
        {
            context.Result = new ForbidResult();
            return;
        }

        if (!patient.IsActive)
        {
            await _signInManager.SignOutAsync();            
            
            var tempData = _tempDataFactory.GetTempData(context.HttpContext);

            tempData["Error"] =
                "Your patient account is inactive. Please contact the administrator.";

            context.Result = new RedirectToPageResult(
                "/Account/Login",
                new
                {
                    area = "Identity"
                });
        }
    }
}