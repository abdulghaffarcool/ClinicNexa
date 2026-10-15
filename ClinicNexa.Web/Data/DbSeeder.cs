using ClinicNexa.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicNexa.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Make sure database is ready
        await context.Database.MigrateAsync();

        // -----------------------------
        // Roles
        // -----------------------------
        string[] roles = { "Admin", "Doctor", "Patient" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // -----------------------------
        // Admin
        // -----------------------------
        var admin = await CreateUserAsync(
            userManager,
            "admin@clinicnexa.com",
            "Admin User",
            "Admin@123");

        await EnsureRoleAsync(userManager, admin, "Admin");

        // -----------------------------
        // Doctor
        // -----------------------------
        var doctorUser = await CreateUserAsync(
            userManager,
            "doctor@clinicnexa.com",
            "Dr. Ahmed Khan",
            "Doctor@123");

        await EnsureRoleAsync(userManager, doctorUser, "Doctor");

        var doctor = await context.Doctors
            .FirstOrDefaultAsync(d => d.UserId == doctorUser.Id);

        if (doctor == null)
        {
            doctor = new Doctor
            {
                UserId = doctorUser.Id,
                FirstName = "Ahmed",
                LastName = "Khan",
                Gender = "Male",
                Phone = "03001234567",
                Qualification = "MBBS, FCPS",
                ExperienceYears = 10,
                ConsultationFee = 2500,
                Bio = "Experienced general physician.",
                IsActive = true
            };

            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();
        }

        // -----------------------------
        // Patient
        // -----------------------------
        var patientUser = await CreateUserAsync(
            userManager,
            "patient@clinicnexa.com",
            "Ali Raza",
            "Patient@123");

        await EnsureRoleAsync(userManager, patientUser, "Patient");

        var patient = await context.Patients
            .FirstOrDefaultAsync(p => p.UserId == patientUser.Id);

        if (patient == null)
        {
            patient = new Patient
            {
                UserId = patientUser.Id,
                FirstName = "Ali",
                LastName = "Raza",
                Gender = "Male",
                Phone = "03111234567",
                DateOfBirth = new DateTime(1995, 5, 15),
                Address = "Karachi",
                BloodGroup = "O+"
            };

            context.Patients.Add(patient);
            await context.SaveChangesAsync();
        }

        // -----------------------------
        // Specialties
        // -----------------------------
        var generalMedicine = await GetOrCreateSpecialtyAsync(
            context,
            "General Medicine",
            "Diagnosis and treatment of common medical conditions.");

        var cardiology = await GetOrCreateSpecialtyAsync(
            context,
            "Cardiology",
            "Medical care related to the heart and cardiovascular system.");

        // -----------------------------
        // Doctor Specialty
        // -----------------------------
        if (!await context.DoctorSpecialties.AnyAsync(
                ds => ds.DoctorId == doctor.Id &&
                      ds.SpecialtyId == generalMedicine.Id))
        {
            context.DoctorSpecialties.Add(new DoctorSpecialty
            {
                DoctorId = doctor.Id,
                SpecialtyId = generalMedicine.Id
            });

            await context.SaveChangesAsync();
        }

        // -----------------------------
        // Doctor Schedule
        // -----------------------------
        if (!await context.DoctorSchedules.AnyAsync(
                s => s.DoctorId == doctor.Id))
        {
            context.DoctorSchedules.AddRange(
                new DoctorSchedule
                {
                    DoctorId = doctor.Id,
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(13, 0, 0),
                    AppointmentDurationMinutes = 30,
                    IsAvailable = true
                },
                new DoctorSchedule
                {
                    DoctorId = doctor.Id,
                    DayOfWeek = DayOfWeek.Wednesday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(13, 0, 0),
                    AppointmentDurationMinutes = 30,
                    IsAvailable = true
                },
                new DoctorSchedule
                {
                    DoctorId = doctor.Id,
                    DayOfWeek = DayOfWeek.Friday,
                    StartTime = new TimeSpan(14, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0),
                    AppointmentDurationMinutes = 30,
                    IsAvailable = true
                });

            await context.SaveChangesAsync();
        }

        // -----------------------------
        // Sample Appointment
        // -----------------------------
        if (!await context.Appointments.AnyAsync())
        {
            var nextMonday = GetNextDayOfWeek(
                DateTime.Today,
                DayOfWeek.Monday);

            context.Appointments.Add(new Appointment
            {
                DoctorId = doctor.Id,
                PatientId = patient.Id,
                AppointmentDate = nextMonday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(9, 30, 0),
                Status = "Confirmed",
                Reason = "Routine checkup"
            });

            await context.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string fullName,
        string password)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user != null)
            return user;

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            throw new Exception(
                $"Failed to create user {email}: " +
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return user;
    }

    private static async Task EnsureRoleAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string role)
    {
        if (!await userManager.IsInRoleAsync(user, role))
        {
            var result = await userManager.AddToRoleAsync(user, role);

            if (!result.Succeeded)
            {
                throw new Exception(
                    $"Failed to assign role {role}: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private static async Task<Specialty> GetOrCreateSpecialtyAsync(
        ApplicationDbContext context,
        string name,
        string description)
    {
        var specialty = await context.Specialties
            .FirstOrDefaultAsync(s => s.Name == name);

        if (specialty != null)
            return specialty;

        specialty = new Specialty
        {
            Name = name,
            Description = description
        };

        context.Specialties.Add(specialty);
        await context.SaveChangesAsync();

        return specialty;
    }

    private static DateTime GetNextDayOfWeek(
        DateTime date,
        DayOfWeek day)
    {
        int daysUntil = ((int)day - (int)date.DayOfWeek + 7) % 7;

        if (daysUntil == 0)
            daysUntil = 7;

        return date.Date.AddDays(daysUntil);
    }
}