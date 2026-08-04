using Dsw2026Tpi.Api.Configurations;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/appointments")]
public class AppointmentController : AppController
{
    private readonly IAppointmentService _service;

    public AppointmentController(IAppointmentService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Policy = Policies.PatientPolicy)]
    [EnableRateLimiting(RateLimitingConfigurationExtensions.AppointmentBookingPolicy)]
    public async Task<IActionResult> Create([FromBody] AppointmentModel.Request request)
    {
        var result = await _service.Create(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("patient")]
    [Authorize(Policy = Policies.PatientPolicy)]
    public async Task<IActionResult> GetByPatientDni([FromQuery] long dni)
    {
        var result = await _service.GetByPatientDni(dni);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.PatientPolicy)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _service.Cancel(id);
        return Ok("ok");
    }

    [HttpGet]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByDate(
        [FromQuery] DateOnly? date,
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 1)
    {
        var result = await _service.GetByDate(date, pageSize, pageIndex);
        return Ok(result);
    }

    [HttpGet("search")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 1,
        [FromQuery] Guid? specialtyId = null,
        [FromQuery] Guid? doctorId = null,
        [FromQuery] long? dni = null,
        [FromQuery] DateOnly? date = null)
    {
        var result = await _service.Search(specialtyId, doctorId, dni, date, pageSize, pageIndex);
        return Ok(result);
    }
}
