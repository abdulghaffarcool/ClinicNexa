
using System.Security.Claims;
using ClinicNexa.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClinicNexa.Web.Controllers;

public abstract class BaseController : Controller
{
    private readonly ILogger<BaseController> _logger;

    protected BaseController(ILogger<BaseController> logger)
    {
        _logger = logger;
    }

    protected IActionResult ApplicationError(string message)
    {
        _logger.LogError(
            "{ErrorMessage} AuthenticatedUserId: {UserId}",
            message,
            User.FindFirstValue(ClaimTypes.NameIdentifier));

        Response.StatusCode = StatusCodes.Status500InternalServerError;

        return View("~/Views/Home/Error.cshtml",
            new ErrorViewModel
            {
                RequestId = HttpContext.TraceIdentifier
            });
    }
}
