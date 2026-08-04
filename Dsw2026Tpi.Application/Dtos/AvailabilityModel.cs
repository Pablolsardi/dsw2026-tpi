
namespace Dsw2026Tpi.Application.Dtos
{
    public record AvailabilityModel
    {
        public record Request(Guid DoctorId, List<DayDto> Days);
        public record DayDto(string Day, TimeOnly StartTime, TimeOnly EndTime);
        public record Response(
            Guid DoctorId,
            byte Month,
            short Year,
            int SlotsGenerated,
            List<DayDto> Days);
    }
}
