namespace Dsw2026Tpi.Application.Dtos;

public record AppointmentModel
{
    public record Request(Guid DoctorId, Guid AvailabilityId, PatientDto Patient, string Reason);

    public record PatientDto(long Dni);

    public record Response(
        Guid Id,
        DateOnly Date,
        TimeOnly StartTime,
        TimeOnly EndTime,
        string Doctor,
        string? Speciality,
        string Status,
        string Reason);
}