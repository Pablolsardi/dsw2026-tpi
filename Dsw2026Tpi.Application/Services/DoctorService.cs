using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;

    public DoctorService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {
        var doctors = await _persistence.Paginate<Doctor, string>(pageSize, pageIndex, d => string.IsNullOrWhiteSpace(name) ||
                                                   d.Name.Contains(name), x => x.Name, nameof(Doctor.Speciality));

        return doctors.Map(ToResponse);
    }


    public async Task<DoctorModel.Response> Create(DoctorModel.Request request)
    {
        Validate(request);

        var speciality = await _persistence.GetById<Speciality>(request.SpecialtyId);
        if (speciality == null)
        {
            throw new EntityNotFoundException(nameof(Speciality));
        }

        var doctor = new Doctor(request.Name, request.LicenseNumber, speciality);
        await _persistence.Add(doctor);

        return ToResponse(doctor);
    }



    public async Task<DoctorModel.Response> Update(Guid id, DoctorModel.Request request)
    {
        Validate(request);

        var doctor = await _persistence.GetById<Doctor>(id);
        if (doctor == null)
        {
            throw new EntityNotFoundException(nameof(Doctor));
        }

        var speciality = await _persistence.GetById<Speciality>(request.SpecialtyId);
        if (speciality == null)
        {
            throw new EntityNotFoundException(nameof(Speciality));
        }

        doctor.Update(request.Name, request.LicenseNumber, speciality);
        await _persistence.Update(doctor);

        return ToResponse(doctor);
    }


    public async Task Delete(Guid id)
    {
        var doctor = await _persistence.GetById<Doctor>(id);
        if (doctor == null)
        {
            throw new EntityNotFoundException(nameof(Doctor));
        }
        doctor.Deleted = true;
        await _persistence.Update(doctor);
    }


    public async Task<IEnumerable<DoctorModel.AvailabilityResponse>> GetAvailabilities(Guid id)
    {
        var doctor = await _persistence.GetById<Doctor>(id);
        if (doctor == null)
        {
            throw new EntityNotFoundException(nameof(Doctor));
        }

        var hoy = DateTime.Now;
        var rules = await _persistence.GetFiltered<AvailabilityRule>(
            r => r.DoctorId == id && r.Month == (byte)hoy.Month && r.Year == (short)hoy.Year);

        if (rules == null) return Array.Empty<DoctorModel.AvailabilityResponse>();

        return rules.Select(r => new DoctorModel.AvailabilityResponse(
            r.Id,
            ToSpanishDay(r.DayOfWeek),
            r.StartTime.ToString("HH:mm"),
            r.EndTime.ToString("HH:mm")
        )).ToList();
    }

    private static void Validate(DoctorModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 3 || request.Name.Length > 100)
        {
            throw new ValidationException(nameof(ErrorCodes.DOCTOR_NAME_INVALID), ErrorCodes.DOCTOR_NAME_INVALID)
                .WithDetail("name", "length_between_3_and_100");
        }
    }


    private static DoctorModel.Response ToResponse(Doctor doctor)
        => new(doctor.Id, doctor.Name, doctor.LicenseNumber,
            new DoctorModel.SpecialtyDto(doctor.Speciality?.Id, doctor.Speciality?.Name));

    private static string ToSpanishDay(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => "LUNES",
        DayOfWeek.Tuesday => "MARTES",
        DayOfWeek.Wednesday => "MIÉRCOLES",
        DayOfWeek.Thursday => "JUEVES",
        DayOfWeek.Friday => "VIERNES",
        DayOfWeek.Saturday => "SÁBADO",
        DayOfWeek.Sunday => "DOMINGO",
        _ => string.Empty
    };

}

