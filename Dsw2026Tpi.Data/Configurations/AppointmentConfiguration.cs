using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.Property(a => a.Reason).IsRequired().HasMaxLength(300);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

        // Token de concurrencia: SQL Server actualiza esta columna en cada UPDATE
        builder.Property(a => a.RowVersion).IsRowVersion();

        // Un slot solo puede tener una cita
        builder.HasIndex(a => a.AvailabilitySlotId).IsUnique();

        builder.HasQueryFilter(a => !a.Deleted);
    }
}