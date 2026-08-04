using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentModel.Response> Create(AppointmentModel.Request request);
    Task<IEnumerable<AppointmentModel.Response>> GetByPatientDni(long dni);
    Task Cancel(Guid id);
    Task<Pagination<AppointmentModel.AdminResponse>> GetByDate(DateOnly? date, int pageSize, int pageIndex);
    Task<Pagination<AppointmentModel.AdminResponse>> Search(
        Guid? specialtyId, Guid? doctorId, long? dni, DateOnly? date, int pageSize, int pageIndex);
}
