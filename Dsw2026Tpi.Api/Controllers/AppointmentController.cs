using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        return Ok("Ok");
    }
}
