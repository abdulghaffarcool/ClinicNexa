using ClinicNexa.Web.Data;
using ClinicNexa.Web.Filters;
using ClinicNexa.Web.Models;
using ClinicNexa.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicNexa.Web.Controllers;

[Authorize(Roles = "Patient")]
[ActivePatient]
public class ReviewsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int appointmentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return BadRequest("Patient profile not found.");

        var appointment = await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Review)
            .FirstOrDefaultAsync(a =>
                a.Id == appointmentId &&
                a.PatientId == patient.Id);

        if (appointment == null)
            return NotFound();

        if (appointment.Status != "Completed")
        {
            TempData["Error"] =
                "You can only review completed appointments.";

            return RedirectToAction(
                "Appointments",
                "Patient");
        }

        if (appointment.Review != null)
        {
            TempData["Error"] =
                "You have already reviewed this appointment.";

            return RedirectToAction(
                "Appointments",
                "Patient");
        }

        ViewBag.DoctorName =
            $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}";

        ViewBag.AppointmentDate =
            appointment.AppointmentDate;

        return View(new ReviewViewModel
        {
            AppointmentId = appointment.Id
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ReviewViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
            return BadRequest("Patient profile not found.");

        var appointment = await _context.Appointments
            .Include(a => a.Review)
            .FirstOrDefaultAsync(a =>
                a.Id == model.AppointmentId &&
                a.PatientId == patient.Id);

        if (appointment == null)
            return NotFound();

        if (appointment.Status != "Completed")
        {
            TempData["Error"] =
                "You can only review completed appointments.";

            return RedirectToAction(
                "Appointments",
                "Patient");
        }

        if (appointment.Review != null)
        {
            TempData["Error"] =
                "You have already reviewed this appointment.";

            return RedirectToAction(
                "Appointments",
                "Patient");
        }

        if (!ModelState.IsValid)
        {
            var doctor = await _context.Doctors
                .FirstAsync(d => d.Id == appointment.DoctorId);

            ViewBag.DoctorName =
                $"{doctor.FirstName} {doctor.LastName}";

            ViewBag.AppointmentDate =
                appointment.AppointmentDate;

            return View(model);
        }

        var review = new Review
        {
            AppointmentId = appointment.Id,
            DoctorId = appointment.DoctorId,
            PatientId = patient.Id,
            Rating = model.Rating,
            Comment = model.Comment
        };

        _context.Reviews.Add(review);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Thank you! Your review has been submitted.";

        return RedirectToAction(
            "Appointments",
            "Patient");
    }
}