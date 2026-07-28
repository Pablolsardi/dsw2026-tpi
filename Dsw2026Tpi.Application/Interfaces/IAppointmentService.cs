using Dsw2026Tpi.Application.Dtos;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentModel.Response> Create(AppointmentModel.Request request);
    Task<IEnumerable<AppointmentModel.Response>> GetByPatientDni(long dni);
    Task Cancel(Guid id);
}
