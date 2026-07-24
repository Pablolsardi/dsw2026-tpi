using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
{
    public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
    {
        builder.ToTable("AvailabilitySlots");

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        // Un doctor no puede tener dos slots en la misma fecha y hora de inicio
        builder.HasIndex(s => new { s.DoctorId, s.SlotDate, s.StartTime }).IsUnique();

        builder.HasQueryFilter(s => !s.Deleted);
    }
}