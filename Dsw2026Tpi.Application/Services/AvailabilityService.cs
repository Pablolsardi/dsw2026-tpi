using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Helpers;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly IPersistence _persistence;
    private readonly IHolidayService _holidayService;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(IPersistence persistence, IHolidayService holidayService,
        ILogger<AvailabilityService> logger)
    {
        _persistence = persistence;
        _holidayService = holidayService;
        _logger = logger;
    }

    public Task<AvailabilityModel.Response> Create(AvailabilityModel.Request request) => Generate(request, overwrite: false);
    public Task<AvailabilityModel.Response> Update(AvailabilityModel.Request request) => Generate(request, overwrite: true);

    private async Task<AvailabilityModel.Response> Generate(AvailabilityModel.Request request, bool overwrite)
    {
        var errors = new List<(string, string)>();
        var parsed = new List<(DayOfWeek Day, TimeOnly Start, TimeOnly End)>();

        if (request.Days is null || request.Days.Count == 0)
        {
            errors.Add(("days", "required"));
        }
        else
        {
            foreach (var d in request.Days)
            {
                if (!AvailabilityHelper.TryParseDay(d.Day, out var dow))
                {
                    errors.Add(("day", "invalid_day"));
                    continue;
                }

                if (d.StartTime >= d.EndTime)
                {
                    errors.Add(("startTime", "must_be_before_endtime"));
                    continue;
                }

                if (!AvailabilityHelper.IsValidRange(d.StartTime, d.EndTime))
                {
                    errors.Add(("startTime", "must_be_multiple_of_30"));
                    continue;
                }

                if (parsed.Any(p => p.Day == dow && d.StartTime < p.End && p.Start < d.EndTime))
                    errors.Add(("days", "overlapping_ranges"));
                else
                    parsed.Add((dow, d.StartTime, d.EndTime));
            }
        }

        if (errors.Count > 0)
            throw (ValidationException)new ValidationException().WithDetail(errors);

        var doctor = await _persistence.GetById<Doctor>(request.DoctorId);
        if (doctor is null)
            throw new EntityNotFoundException(nameof(Doctor));

        var hoy = DateOnly.FromDateTime(DateTime.Now);
        byte mes = (byte)hoy.Month;
        short anio = (short)hoy.Year;

        var reglasExistentes = (await _persistence.GetFiltered<AvailabilityRule>(r =>
                r.DoctorId == request.DoctorId && r.Month == mes && r.Year == anio))
            ?.ToList() ?? new List<AvailabilityRule>();

        var ultimoDia = new DateOnly(anio, mes, DateTime.DaysInMonth(anio, mes));

        var slotsExistentes = (await _persistence.GetFilteredIgnoringFilters<AvailabilitySlot>(s =>
            s.DoctorId == request.DoctorId && s.SlotDate >= hoy && s.SlotDate <= ultimoDia)).ToList();

        var indice = slotsExistentes.ToDictionary(s => (s.SlotDate, s.StartTime));

        var reglasNuevas = new List<AvailabilityRule>();
        var reglasARestaurar = new List<AvailabilityRule>();
        var slotsNuevos = new List<AvailabilitySlot>();
        var slotsARestaurar = new List<AvailabilitySlot>();
        var targetKeys = new HashSet<(DateOnly, TimeOnly)>();

        foreach (var p in parsed)
        {
            var regla = reglasExistentes.FirstOrDefault(r =>
                r.DayOfWeek == p.Day && r.StartTime == p.Start && r.EndTime == p.End);

            if (regla is null)
            {
                regla = new AvailabilityRule(request.DoctorId, mes, anio, p.Day, p.Start, p.End);
                reglasNuevas.Add(regla);
            }
            else if (regla.Deleted)
            {
                regla.Deleted = false;
                reglasARestaurar.Add(regla);
            }

            var bloques = AvailabilityHelper.GetSlots(p.Start, p.End);

            foreach (var fecha in AvailabilityHelper.GetDatesInMonth(p.Day, hoy))
            {
                if (fecha < hoy) continue;
                if (_holidayService.IsHoliday(fecha)) continue;

                foreach (var b in bloques)
                {
                    targetKeys.Add((fecha, b.Start));

                    if (!indice.TryGetValue((fecha, b.Start), out var existente))
                        slotsNuevos.Add(new AvailabilitySlot(regla.Id, request.DoctorId, fecha, b.Start, b.End));
                    else if (existente.Deleted)
                    {
                        existente.Deleted = false;
                        slotsARestaurar.Add(existente);
                    }
                }
            }
        }

        var slotsABorrar = overwrite
            ? slotsExistentes.Where(s => !s.Deleted
                    && s.Status == SlotStatus.Available
                    && s.SlotDate >= hoy
                    && !targetKeys.Contains((s.SlotDate, s.StartTime)))
                .ToList()
            : new List<AvailabilitySlot>();

        var reglasABorrar = overwrite
            ? reglasExistentes.Where(r => !r.Deleted
                    && !parsed.Any(p => p.Day == r.DayOfWeek && p.Start == r.StartTime && p.End == r.EndTime))
                .ToList()
            : new List<AvailabilityRule>();

        foreach (var s in slotsABorrar) s.Deleted = true;
        foreach (var r in reglasABorrar) r.Deleted = true;

        try
        {
            await _persistence.ExecuteInTransaction(async () =>
            {
                if (reglasNuevas.Count > 0)
                    await _persistence.AddRange(reglasNuevas);

                var reglasAActualizar = reglasARestaurar.Concat(reglasABorrar).ToList();
                if (reglasAActualizar.Count > 0)
                    await _persistence.UpdateRange(reglasAActualizar);

                if (slotsNuevos.Count > 0)
                    await _persistence.AddRange(slotsNuevos);

                var slotsAActualizar = slotsARestaurar.Concat(slotsABorrar).ToList();
                if (slotsAActualizar.Count > 0)
                    await _persistence.UpdateRange(slotsAActualizar);
            });
        }
        catch (DbUpdateException)
        {
            throw new ConflictException(nameof(ErrorCodes.AVAILABILITY_CONFLICT), ErrorCodes.AVAILABILITY_CONFLICT);
        }

        _logger.LogInformation(
            "Disponibilidad {Modo} para doctor {DoctorId}: {Nuevos} slots creados, {Borrados} liberados",
            overwrite ? "actualizada" : "creada", request.DoctorId, slotsNuevos.Count, slotsABorrar.Count);

        var diasResponse = parsed
            .Select(p => new AvailabilityModel.DayDto(AvailabilityHelper.FormatDay(p.Day), p.Start, p.End))
            .ToList();

        return new AvailabilityModel.Response(
            request.DoctorId, mes, anio,
            slotsNuevos.Count + slotsARestaurar.Count,
            diasResponse);
    }
}