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


        builder.Property(a => a.RowVersion).IsRowVersion();


        builder.HasIndex(a => a.AvailabilitySlotId).IsUnique();

        builder.HasQueryFilter(a => !a.Deleted);
    }
}