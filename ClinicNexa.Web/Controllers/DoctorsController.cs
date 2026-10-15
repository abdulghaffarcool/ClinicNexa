using ClinicNexa.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicNexa.Web.Controllers;

public class DoctorsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DoctorsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? specialtyId)
    {
        var query = _context.Doctors
            .Where(d => d.IsActive)
            .Include(d => d.DoctorSpecialties)
                .ThenInclude(ds => ds.Specialty)
            .AsNoTracking()
            .AsQueryable();

        if (specialtyId.HasValue)
        {
            query = query.Where(d =>
                d.DoctorSpecialties.Any(ds =>
                    ds.SpecialtyId == specialtyId.Value));
        }

        var doctors = await query
            .OrderBy(d => d.FirstName)
            .ThenBy(d => d.LastName)
            .ToListAsync();

        ViewBag.Specialties = await _context.Specialties
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.SelectedSpecialtyId = specialtyId;

        return View(doctors);
    }

    public async Task<IActionResult> Details(int id)
    {
        var doctor = await _context.Doctors
            .Where(d => d.Id == id && d.IsActive)
            .Include(d => d.DoctorSpecialties)
                .ThenInclude(ds => ds.Specialty)
            .Include(d => d.Schedules)
            .Include(d => d.Reviews)
            .ThenInclude(r => r.Patient)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (doctor == null)
        {
            return NotFound();
        }

        return View(doctor);
    }
}