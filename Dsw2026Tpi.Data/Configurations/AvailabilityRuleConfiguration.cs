using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AvailabilityRuleConfiguration : IEntityTypeConfiguration<AvailabilityRule>
{
    public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
    {
        builder.ToTable("AvailabilityRules");

        builder.Property(a => a.DoctorId).IsRequired();
        builder.Property(a => a.Month).IsRequired();
        builder.Property(a => a.Year).IsRequired();
        builder.Property(a => a.DayOfWeek).IsRequired();
        builder.Property(a => a.StartTime).IsRequired();
        builder.Property(a => a.EndTime).IsRequired();

        // Evitar reglas duplicadas para el mismo doctor, día y hora de inicio
        builder.HasIndex(a => new { a.DoctorId, a.Year, a.Month, a.DayOfWeek, a.StartTime, a.EndTime }).IsUnique();

        builder.HasQueryFilter(a => !a.Deleted);
    }
}
