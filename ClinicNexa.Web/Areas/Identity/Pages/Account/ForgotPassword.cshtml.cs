using ClinicNexa.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ClinicNexa.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ForgotPasswordModel(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.FindByEmailAsync(Input.Email);

        // Don't reveal whether an account exists.
        //if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
        if (user == null)
        {
            return RedirectToPage("./ForgotPasswordConfirmation");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var resetUrl = Url.Page(
            "/Account/ResetPassword",
            null,
            new
            {
                area = "Identity",
                code = token,
                email = user.Email
            },
            Request.Scheme);

        // Temporary development behavior.
        // Later this will be replaced with a real email sender.
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("PASSWORD RESET LINK");
        Console.WriteLine(resetUrl);
        Console.WriteLine("========================================");
        Console.WriteLine();

        return RedirectToPage("./ForgotPasswordConfirmation");
    }
}