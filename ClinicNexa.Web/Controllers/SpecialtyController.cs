
using ClinicNexa.Web.Data;
using ClinicNexa.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicNexa.Web.Controllers;

[Authorize(Roles = "Admin")]
public class SpecialtyController : Controller
{
    private readonly ApplicationDbContext _context;

    public SpecialtyController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Specialties()
    {
        var specialties = await _context.Specialties
            .Include(s => s.DoctorSpecialties)
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();

        return View(specialties);
    }

    [HttpGet]
    public IActionResult CreateSpecialty()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSpecialty(
        [Bind("Name,Description")] Specialty model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var exists = await _context.Specialties
            .AnyAsync(s => s.Name == model.Name);

        if (exists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A specialty with this name already exists.");

            return View(model);
        }

        _context.Specialties.Add(model);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Specialty created successfully.";

        return RedirectToAction(nameof(Specialties));
    }

    [HttpGet]
    public async Task<IActionResult> EditSpecialty(int id)
    {
        var specialty = await _context.Specialties.FindAsync(id);

        if (specialty == null)
            return NotFound();

        return View(specialty);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSpecialty(
        int id,
        [Bind("Id,Name,Description")] Specialty model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var exists = await _context.Specialties
            .AnyAsync(s =>
                s.Id != id &&
                s.Name == model.Name);

        if (exists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A specialty with this name already exists.");

            return View(model);
        }

        var specialty = await _context.Specialties.FindAsync(id);

        if (specialty == null)
            return NotFound();

        specialty.Name = model.Name;
        specialty.Description = model.Description;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Specialty updated successfully.";

        return RedirectToAction(nameof(Specialties));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSpecialty(int id)
    {
        var specialty = await _context.Specialties
            .Include(s => s.DoctorSpecialties)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (specialty == null)
            return NotFound();

        if (specialty.DoctorSpecialties.Any())
        {
            TempData["Error"] =
                "This specialty cannot be deleted because doctors are assigned to it.";

            return RedirectToAction(nameof(Specialties));
        }

        _context.Specialties.Remove(specialty);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Specialty deleted successfully.";

        return RedirectToAction(nameof(Specialties));
    }

}
