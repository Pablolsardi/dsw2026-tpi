using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class SpecialityService : ISpecialityService
{
    private readonly IPersistence _persistence;

    public SpecialityService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<SpecialityModel.Response> Create(SpecialityModel.Request request)
    {
        Validate(request);

        var existente = await _persistence.FirstIgnoringFilters<Speciality>(s => s.Name == request.Name);

        if (existente is not null)
        {
            if (!existente.Deleted)
            {
                throw new ConflictException(nameof(ErrorCodes.SPECIALITY_DUPLICATED), ErrorCodes.SPECIALITY_DUPLICATED);
            }

            existente.Restore();
            existente.Update(request.Name, request.Description);
            await _persistence.Update(existente);

            return ToResponse(existente);
        }

        var speciality = new Speciality(request.Name, request.Description);
        await _persistence.Add(speciality);

        return ToResponse(speciality);
    }

    public async Task Delete(Guid id)
    {
        var speciality = await _persistence.GetById<Speciality>(id);

        if (speciality is null)
        {
            throw new EntityNotFoundException(nameof(Speciality));
        }
        speciality.Deleted = true;
        await _persistence.Update(speciality);
    }

    public async Task<Pagination<SpecialityModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {
        var specialities = await _persistence.Paginate<Speciality, string>(
            pageSize,
            pageIndex,
            s => string.IsNullOrWhiteSpace(name) || s.Name.Contains(name),
            s => s.Name);

        return specialities.Map(ToResponse);
    }

    public async Task<SpecialityModel.Response> Update(Guid id, SpecialityModel.Request request)
    {
        Validate(request);

        var speciality = await _persistence.GetById<Speciality>(id);
        if (speciality is null)
        {
            throw new EntityNotFoundException(nameof(Speciality));
        }

        var duplicada = await _persistence.FirstIgnoringFilters<Speciality>(
            s => s.Name == request.Name && s.Id != id);

        if (duplicada is not null)
        {
            throw new ConflictException(
                nameof(ErrorCodes.SPECIALITY_DUPLICATED),
                ErrorCodes.SPECIALITY_DUPLICATED);
        }

        speciality.Update(request.Name, request.Description);
        await _persistence.Update(speciality);

        return ToResponse(speciality);
    }

    private static void Validate(SpecialityModel.Request request)
    {
        var errors = new List<(string, string)>();

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 3 || request.Name.Length > 100)
            errors.Add((nameof(request.Name), "length_between_3_and_100"));

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length < 10 || request.Description.Length > 100)
            errors.Add((nameof(request.Description), "length_between_10_and_100"));

        if (errors.Count > 0)
            throw (ValidationException)new ValidationException().WithDetail(errors);
    }

    private static SpecialityModel.Response ToResponse(Speciality speciality)
        => new(speciality.Id, speciality.Name, speciality.Description);
}

