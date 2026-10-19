
using ClinicNexa.Web.Models;

namespace ClinicNexa.Tests;

public class AppointmentTests
{
    [Fact]
    public void NewAppointment_HasPendingStatus()
    {
        // Arrange & Act
        var appointment = new Appointment();

        // Assert
        Assert.Equal("Pending", appointment.Status);
    }

    [Fact]
    public void NewAppointment_SetsCreatedAt()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var appointment = new Appointment();

        // Assert
        var afterCreation = DateTime.UtcNow;

        Assert.InRange(
            appointment.CreatedAt,
            beforeCreation,
            afterCreation);
    }
}
