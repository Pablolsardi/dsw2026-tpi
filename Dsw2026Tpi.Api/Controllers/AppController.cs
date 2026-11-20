using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

/// <summary>
/// Clase base para configuraciones generales de controladores
/// </summary>
[ApiController]
public abstract class AppController : ControllerBase
{
    // DNI del paciente logueado, sacado del token (no de la URL ni del body)
    protected long GetPatientDni()
    {
        var value = User.FindFirst(JwtService.DniClaim)?.Value;

        if (!long.TryParse(value, out var dni))
            throw new AuthorizationException(); // token viejo o sin DNI → 403

        return dni;
    }
}

