using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;


namespace Dsw2026Tpi.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IPersistence _persistence;
    private readonly ILogger<AppointmentService> _logger;
    public AppointmentService(IPersistence persistence, ILogger<AppointmentService> logger)
    {
        _persistence = persistence;
        _logger = logger;
    }
    public async Task<IEnumerable<AppointmentModel.Response>> GetByPatientDni(long dni)
    {
        var dniString = dni.ToString();

        var appointments = (await _persistence.GetFiltered<Appointment>(
            a => a.Patient != null && a.Patient.Dni == dniString && a.Status == AppointmentStatus.Booked,
            nameof(Appointment.Patient),
            nameof(Appointment.AvailabilitySlot)))?.ToList() ?? new List<Appointment>();

        if (appointments.Count == 0) return Array.Empty<AppointmentModel.Response>();

        var doctorIds = appointments
            .Where(a => a.AvailabilitySlot is not null)
            .Select(a => a.AvailabilitySlot!.DoctorId)
            .Distinct()
            .ToList();

        var doctores = (await _persistence.GetFiltered<Doctor>(
            d => doctorIds.Contains(d.Id), nameof(Doctor.Speciality)))?.ToList() ?? new List<Doctor>();

        var porId = doctores.ToDictionary(d => d.Id);

        return appointments
            .Where(a => a.AvailabilitySlot is not null && porId.ContainsKey(a.AvailabilitySlot.DoctorId))
            .Select(a => ToResponse(a, a.AvailabilitySlot!, porId[a.AvailabilitySlot!.DoctorId]))
            .ToList();
    }

   
    public Task<Pagination<AppointmentModel.AdminResponse>> GetByDate(DateOnly? date, int pageSize, int pageIndex)
    {
        if (date is null)
            throw (ValidationException)new ValidationException()
                .WithDetail(new[] { ("date", "required") });

        return Search(null, null, null, date, pageSize, pageIndex);
    }

    public async Task<Pagination<AppointmentModel.AdminResponse>> Search(
        Guid? specialtyId,
        Guid? doctorId,
        long? dni,
        DateOnly? date,
        int pageSize,
        int pageIndex)
    {
        var dniString = dni?.ToString();

        var filtraPorEspecialidad = specialtyId is not null;
        var doctorIdsDeEspecialidad = Array.Empty<Guid>();

        if (filtraPorEspecialidad)
        {
            var doctoresDeEspecialidad = await _persistence.GetFiltered<Doctor>(
                d => d.SpecialityId == specialtyId);

            doctorIdsDeEspecialidad = doctoresDeEspecialidad?.Select(d => d.Id).ToArray()
                                      ?? Array.Empty<Guid>();

            if (doctorIdsDeEspecialidad.Length == 0)
                return new Pagination<AppointmentModel.AdminResponse>(pageSize, pageIndex, 0, []);
        }

        Expression<Func<Appointment, bool>> predicate = a =>
            a.Patient != null &&
            a.AvailabilitySlot != null &&
            (doctorId == null || a.AvailabilitySlot.DoctorId == doctorId) &&
            (!filtraPorEspecialidad || doctorIdsDeEspecialidad.Contains(a.AvailabilitySlot.DoctorId)) &&
            (dniString == null || a.Patient.Dni == dniString) &&
            (date == null || a.AvailabilitySlot.SlotDate == date);

        var page = await _persistence.Paginate<Appointment, DateOnly>(
            pageSize,
            pageIndex,
            predicate,
            a => a.AvailabilitySlot!.SlotDate,
            nameof(Appointment.Patient),
            nameof(Appointment.AvailabilitySlot));

        var appointments = page.Data.ToList();
        if (appointments.Count == 0)
            return new Pagination<AppointmentModel.AdminResponse>(page.PageSize, page.PageIndex, page.Total, []);

        var idsPagina = appointments
            .Select(a => a.AvailabilitySlot!.DoctorId)
            .Distinct()
            .ToList();

        var doctores = (await _persistence.GetFiltered<Doctor>(
            d => idsPagina.Contains(d.Id), nameof(Doctor.Speciality)))?.ToList() ?? new List<Doctor>();

        var porId = doctores.ToDictionary(d => d.Id);

        var data = appointments
            .Where(a => porId.ContainsKey(a.AvailabilitySlot!.DoctorId))
            .OrderBy(a => a.AvailabilitySlot!.SlotDate)
            .ThenBy(a => a.AvailabilitySlot!.StartTime)
            .Select(a => ToAdminResponse(a, a.AvailabilitySlot!, porId[a.AvailabilitySlot!.DoctorId]))
            .ToList();

        return new Pagination<AppointmentModel.AdminResponse>(page.PageSize, page.PageIndex, page.Total, data);
    }

    private static AppointmentModel.AdminResponse ToAdminResponse(
        Appointment appointment, AvailabilitySlot slot, Doctor doctor)
    {
        var specialty = doctor.Speciality is null
            ? null
            : new AppointmentModel.SpecialtySummary(doctor.Speciality.Id, doctor.Speciality.Name);

        var dni = long.TryParse(appointment.Patient?.Dni, out var valor) ? valor : 0;

        return new AppointmentModel.AdminResponse(
            appointment.Id,
            ToStatusText(appointment.Status),
            new AppointmentModel.PatientSummary(dni, appointment.Patient?.FullName),
            new AppointmentModel.DoctorSummary(doctor.Id, doctor.Name, specialty));
    }

    
    public async Task Cancel(Guid id)
    {
        var appointment = await _persistence.GetById<Appointment>(id);
        if (appointment == null)
            throw new EntityNotFoundException(nameof(Appointment));
        if (appointment.Status != AppointmentStatus.Booked)
            throw new BusinessRuleException(nameof(ErrorCodes.APPOINTMENT_CANCEL_INVALID), ErrorCodes.APPOINTMENT_CANCEL_INVALID);
        var slot = await _persistence.GetById<AvailabilitySlot>(appointment.AvailabilitySlotId)
    ?? throw new EntityNotFoundException(nameof(AvailabilitySlot));
        try
        {
            await _persistence.ExecuteInTransaction(async () =>
            {
                appointment.Cancel();
                slot.Release();
                await _persistence.Update(appointment);
                await _persistence.Update(slot);
            });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            throw new ConflictException(nameof(ErrorCodes.APPOINTMENT_CONFLICT), ErrorCodes.APPOINTMENT_CONFLICT);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            throw new ConflictException(nameof(ErrorCodes.APPOINTMENT_CONFLICT), ErrorCodes.APPOINTMENT_CONFLICT);
        }
        _logger.LogInformation("Turno cancelado: {AppointmentId}", id);
    }

    public async Task<AppointmentModel.Response> Create(AppointmentModel.Request request)
    {

        var errors = new List<(string, string)>();

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 5)
            errors.Add((nameof(request.Reason), "min_length_5"));

        var dniLength = request.Patient.Dni.ToString().Length;
        if (dniLength < 7 || dniLength > 10)
            errors.Add((nameof(request.Patient.Dni), "length_between_7_and_10"));
        if (errors.Count > 0)
            throw (ValidationException)new ValidationException().WithDetail(errors);
        var doctor = await _persistence.GetById<Doctor>(request.DoctorId, nameof(Doctor.Speciality));
        if (doctor == null)
            throw new EntityNotFoundException(nameof(Doctor));
        var dniString = request.Patient.Dni.ToString();
        var patient = await _persistence.First<Patient>(p => p.Dni == dniString);
        if (patient == null)
            throw new EntityNotFoundException(nameof(Patient));
        var slot = await _persistence.GetById<AvailabilitySlot>(request.AvailabilitySlotId);
        if (slot == null)
            throw new EntityNotFoundException(nameof(AvailabilitySlot));
        if (slot.DoctorId != request.DoctorId)
            throw (ValidationException)new ValidationException().WithDetail(new[] { (nameof(request.AvailabilitySlotId), nameof(ErrorCodes.APPOINTMENT_SLOT_MISMATCH)) });
        if (slot.Status != SlotStatus.Available)
            throw new ConflictException(nameof(ErrorCodes.APPOINTMENT_CONFLICT), ErrorCodes.APPOINTMENT_CONFLICT);
        var slotDateTime = slot.SlotDate.ToDateTime(slot.StartTime);
        if (slotDateTime <= DateTime.Now)
            throw new BusinessRuleException(nameof(ErrorCodes.APPOINTMENT_PAST_DATE), ErrorCodes.APPOINTMENT_PAST_DATE);
        var appointment = new Appointment(slot.Id, patient.Id, request.Reason);
        try
        {
            await _persistence.ExecuteInTransaction(async () =>
            {
                slot.Book();
                await _persistence.Update(slot);
                await _persistence.Add(appointment);
            });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            throw new ConflictException(nameof(ErrorCodes.APPOINTMENT_CONFLICT), ErrorCodes.APPOINTMENT_CONFLICT);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            throw new ConflictException(nameof(ErrorCodes.APPOINTMENT_CONFLICT), ErrorCodes.APPOINTMENT_CONFLICT);
        }

        _logger.LogInformation("Turno reservado: {AppointmentId} para paciente {Dni}", appointment.Id, request.Patient.Dni);

        return ToResponse(appointment, slot, doctor);
    }

    private AppointmentModel.Response ToResponse(Appointment appointment, AvailabilitySlot slot, Doctor doctor)
    {
        return new AppointmentModel.Response(
            appointment.Id,
            appointment.AvailabilitySlotId,
            slot.SlotDate,
            slot.StartTime,
            slot.EndTime,
            doctor.Name,
            doctor.Speciality?.Name,
            ToStatusText(appointment.Status),
            appointment.Reason);
    }
    private static string ToStatusText(AppointmentStatus status) => status switch
    {
        AppointmentStatus.NoShow => "NO_SHOW",
        _ => status.ToString().ToUpperInvariant()
    };

} 