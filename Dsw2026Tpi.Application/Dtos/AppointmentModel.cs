namespace Dsw2026Tpi.Application.Dtos;

public record AppointmentModel
{
    public record Request(Guid DoctorId, Guid AvailabilitySlotId, PatientDto Patient, string Reason);

    public record PatientDto(long Dni);

    public record Response(
        Guid Id,
        Guid AvailabilitySlotId,
        DateOnly Date,
        TimeOnly StartTime,
        TimeOnly EndTime,
        string Doctor,
        string? Speciality,
        string Status,
        string Reason);
    public record AdminResponse(
        Guid AppointmentsId,
        string AppointmentsStatus,
        PatientSummary Patient,
        DoctorSummary Doctor
        );
    public record PatientSummary(long Dni, string? FullName);
    public record DoctorSummary(Guid DoctorId, string? Name, SpecialtySummary? Specialty);
    public record SpecialtySummary(Guid SpecialtyId, string Name);
}