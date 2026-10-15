using ClinicNexa.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicNexa.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<DoctorSpecialty> DoctorSpecialties => Set<DoctorSpecialty>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Doctor → ApplicationUser (1:1)
        builder.Entity<Doctor>()
            .HasOne(d => d.User)
            .WithOne(u => u.Doctor)
            .HasForeignKey<Doctor>(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Patient → ApplicationUser (1:1)
        builder.Entity<Patient>()
            .HasOne(p => p.User)
            .WithOne(u => u.Patient)
            .HasForeignKey<Patient>(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // DoctorSpecialty composite key
        builder.Entity<DoctorSpecialty>()
            .HasKey(ds => new { ds.DoctorId, ds.SpecialtyId });

        // Doctor → Specialty
        builder.Entity<DoctorSpecialty>()
            .HasOne(ds => ds.Doctor)
            .WithMany(d => d.DoctorSpecialties)
            .HasForeignKey(ds => ds.DoctorId);

        builder.Entity<DoctorSpecialty>()
            .HasOne(ds => ds.Specialty)
            .WithMany(s => s.DoctorSpecialties)
            .HasForeignKey(ds => ds.SpecialtyId);

        // Doctor → Schedules
        builder.Entity<DoctorSchedule>()
            .HasOne(s => s.Doctor)
            .WithMany(d => d.Schedules)
            .HasForeignKey(s => s.DoctorId);

        // Doctor → Appointments
        builder.Entity<Appointment>()
            .HasOne(a => a.Doctor)
            .WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Patient → Appointments
        builder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Appointment → Review (1:0/1)
        builder.Entity<Review>()
            .HasOne(r => r.Appointment)
            .WithOne(a => a.Review)
            .HasForeignKey<Review>(r => r.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Review → Doctor
        builder.Entity<Review>()
            .HasOne(r => r.Doctor)
            .WithMany(d => d.Reviews)
            .HasForeignKey(r => r.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Review → Patient
        builder.Entity<Review>()
            .HasOne(r => r.Patient)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Constraints
        builder.Entity<Doctor>()
            .HasIndex(d => d.UserId)
            .IsUnique();

        builder.Entity<Patient>()
            .HasIndex(p => p.UserId)
            .IsUnique();

        builder.Entity<Specialty>()
            .HasIndex(s => s.Name)
            .IsUnique();

        builder.Entity<Review>()
            .HasIndex(r => r.AppointmentId)
            .IsUnique();

        // For a given doctor + date + start time, only one non-cancelled
        // appointment can exist
        builder.Entity<Appointment>()
            .HasIndex(a => new
            {
                a.DoctorId,
                a.AppointmentDate,
                a.StartTime
            })
            .IsUnique()
            .HasFilter("Status <> 'Cancelled'");
    }
}