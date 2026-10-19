
using ClinicNexa.Web.Data;
using ClinicNexa.Web.Models;
using ClinicNexa.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ClinicNexa.Tests;

public class AppointmentServiceTests
{
    [Fact]
    public async Task GetAvailableSlotsAsync_ReturnsSlotsWithinSchedule()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new ApplicationUser
        {
            Id = "doctor-1",
            UserName = "doctor@example.com",
            NormalizedUserName = "DOCTOR@EXAMPLE.COM",
            Email = "doctor@example.com",
            NormalizedEmail = "DOCTOR@EXAMPLE.COM"
        };

        var doctor = new Doctor
        {
            UserId = user.Id,
            User = user,
            FirstName = "Test",
            LastName = "Doctor"
        };

        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();
        
        var date = new DateTime(2026, 10, 19); // Monday

        context.DoctorSchedules.Add(new DoctorSchedule
        {
            DoctorId = doctor.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            AppointmentDurationMinutes = 30,
            IsAvailable = true
        });

        await context.SaveChangesAsync();

        var service = new AppointmentService(context);

        var slots = await service.GetAvailableSlotsAsync(doctor.Id, date);

        Assert.Equal(2, slots.Count);
        Assert.Equal(TimeSpan.FromHours(9), slots[0].StartTime);
        Assert.Equal(TimeSpan.FromHours(9.5), slots[0].EndTime);
        Assert.Equal(TimeSpan.FromHours(9.5), slots[1].StartTime);
        Assert.Equal(TimeSpan.FromHours(10), slots[1].EndTime);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ExcludesBookedSlots()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new ApplicationUser
        {
            Id = "doctor-1",
            UserName = "doctor@example.com",
            NormalizedUserName = "DOCTOR@EXAMPLE.COM",
            Email = "doctor@example.com",
            NormalizedEmail = "DOCTOR@EXAMPLE.COM"
        };

        var doctor = new Doctor
        {
            UserId = user.Id,
            User = user,
            FirstName = "Test",
            LastName = "Doctor"
        };

        var patientUser = new ApplicationUser
        {
            Id = "patient-1",
            UserName = "patient@example.com",
            NormalizedUserName = "PATIENT@EXAMPLE.COM",
            Email = "patient@example.com",
            NormalizedEmail = "PATIENT@EXAMPLE.COM"
        };

        var patient = new Patient
        {
            UserId = patientUser.Id,
            User = patientUser,
            FirstName = "Test",
            LastName = "Patient"
        };

        context.Doctors.Add(doctor);
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var date = new DateTime(2026, 10, 19); // Monday

        context.DoctorSchedules.Add(new DoctorSchedule
        {
            DoctorId = doctor.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            AppointmentDurationMinutes = 30,
            IsAvailable = true
        });

        context.Appointments.Add(new Appointment
        {
            DoctorId = doctor.Id,
            PatientId = patient.Id,
            AppointmentDate = date,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(9.5),
            Status = "Confirmed"
        });

        await context.SaveChangesAsync();

        var service = new AppointmentService(context);

        var slots = await service.GetAvailableSlotsAsync(doctor.Id, date);

        var slot = Assert.Single(slots);
        Assert.Equal(TimeSpan.FromHours(9.5), slot.StartTime);
        Assert.Equal(TimeSpan.FromHours(10), slot.EndTime);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_IncludesCancelledAppointmentSlot()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var doctorUser = new ApplicationUser
        {
            Id = "doctor-1",
            UserName = "doctor@example.com",
            NormalizedUserName = "DOCTOR@EXAMPLE.COM"
        };

        var doctor = new Doctor
        {
            UserId = doctorUser.Id,
            User = doctorUser,
            FirstName = "Test",
            LastName = "Doctor"
        };

        var patientUser = new ApplicationUser
        {
            Id = "patient-1",
            UserName = "patient@example.com",
            NormalizedUserName = "PATIENT@EXAMPLE.COM"
        };

        var patient = new Patient
        {
            UserId = patientUser.Id,
            User = patientUser,
            FirstName = "Test",
            LastName = "Patient"
        };

        context.Doctors.Add(doctor);
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var date = new DateTime(2026, 10, 19);

        context.DoctorSchedules.Add(new DoctorSchedule
        {
            DoctorId = doctor.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            AppointmentDurationMinutes = 30,
            IsAvailable = true
        });

        context.Appointments.Add(new Appointment
        {
            DoctorId = doctor.Id,
            PatientId = patient.Id,
            AppointmentDate = date,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(9.5),
            Status = "Cancelled"
        });

        await context.SaveChangesAsync();

        var service = new AppointmentService(context);

        var slots = await service.GetAvailableSlotsAsync(doctor.Id, date);

        Assert.Equal(2, slots.Count);
        Assert.Equal(TimeSpan.FromHours(9), slots[0].StartTime);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ReturnsNoSlotsWhenScheduleUnavailable()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new ApplicationUser
        {
            Id = "doctor-1",
            UserName = "doctor@example.com",
            NormalizedUserName = "DOCTOR@EXAMPLE.COM"
        };

        var doctor = new Doctor
        {
            UserId = user.Id,
            User = user,
            FirstName = "Test",
            LastName = "Doctor"
        };

        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();

        var date = new DateTime(2026, 10, 19);

        context.DoctorSchedules.Add(new DoctorSchedule
        {
            DoctorId = doctor.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            AppointmentDurationMinutes = 30,
            IsAvailable = false
        });

        await context.SaveChangesAsync();

        var service = new AppointmentService(context);

        var slots = await service.GetAvailableSlotsAsync(doctor.Id, date);

        Assert.Empty(slots);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_DifferentDateAppointmentDoesNotBlockSlot()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var doctorUser = new ApplicationUser
        {
            Id = "doctor-1",
            UserName = "doctor@example.com",
            NormalizedUserName = "DOCTOR@EXAMPLE.COM"
        };

        var doctor = new Doctor
        {
            UserId = doctorUser.Id,
            User = doctorUser,
            FirstName = "Test",
            LastName = "Doctor"
        };

        var patientUser = new ApplicationUser
        {
            Id = "patient-1",
            UserName = "patient@example.com",
            NormalizedUserName = "PATIENT@EXAMPLE.COM"
        };

        var patient = new Patient
        {
            UserId = patientUser.Id,
            User = patientUser,
            FirstName = "Test",
            LastName = "Patient"
        };

        context.Doctors.Add(doctor);
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var requestedDate = new DateTime(2026, 10, 19);

        context.DoctorSchedules.Add(new DoctorSchedule
        {
            DoctorId = doctor.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            AppointmentDurationMinutes = 30,
            IsAvailable = true
        });

        context.Appointments.Add(new Appointment
        {
            DoctorId = doctor.Id,
            PatientId = patient.Id,
            AppointmentDate = requestedDate.AddDays(7),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(9.5),
            Status = "Confirmed"
        });

        await context.SaveChangesAsync();

        var service = new AppointmentService(context);

        var slots = await service.GetAvailableSlotsAsync(
            doctor.Id,
            requestedDate);

        Assert.Equal(2, slots.Count);
        Assert.Equal(TimeSpan.FromHours(9), slots[0].StartTime);
        Assert.Equal(TimeSpan.FromHours(9.5), slots[0].EndTime);
    }
}
