using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
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

    public Task<SpecialityModel.Response> Create(SpecialityModel.Request request)
    {
        throw new NotImplementedException();
    }

    public Task Delete(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<Pagination<SpecialityModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {
        var specialities = await _persistence.Paginate<Speciality, string>(
            pageSize,
            pageIndex,
            s => string.IsNullOrWhiteSpace(name) || s.Name.Contains(name),
            s => s.Name);

        return specialities.Map(s => new SpecialityModel.Response(s.Id, s.Name, s.Description));
    }

    public Task<SpecialityModel.Response> Update(Guid id, SpecialityModel.Request request)
    {
        throw new NotImplementedException();
    }
}

