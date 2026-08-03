using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Data.Identity;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Dsw2026Tpi.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISignInService _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IPersistence _persistence;
    public AuthenticationService(UserManager<ApplicationUser> userManager,
        ISignInService signInManager,
        RoleManager<IdentityRole> roleManager,
        JwtService jwtService,
        ILogger<AuthenticationService> logger,
        IPersistence persistence)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _logger = logger;
        _persistence = persistence;
    }
    public async Task<LoginAdminModel.Response> LoginAdmin(LoginAdminModel.Request request)
    {
        if (!request.Email.IsEmailValid()) throw new AuthenticationException();
        var user = await _userManager.FindByEmailAsync(request.Email) ?? throw new AuthenticationException();
        var result = await _signInManager.CheckPassword(user, request.Password);

        if (!result)
        {
            _logger.LogError("Intento de login fallido para: {Email}", request.Email);
            throw new AuthenticationException();
        }

        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
        var token  = _jwtService.GenerateToken(user.UserName!, role);
        return new LoginAdminModel.Response(token, role?.ToUpperInvariant());
    }
    public async Task<LoginPatientModel.Response> LoginPatient(LoginPatientModel.Request request)
    {

        if (!request.Email.IsEmailValid()) throw new ValidationException(nameof(ErrorCodes.PATIENT_LOGIN_INVALID),
            ErrorCodes.PATIENT_LOGIN_INVALID);

        if (request.Dni is < 1_000_000 or > 99_999_999)
        {
            throw new ValidationException(nameof(ErrorCodes.PATIENT_LOGIN_INVALID),
            ErrorCodes.PATIENT_LOGIN_INVALID);
        }

        var dni = request.Dni.ToString();
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            var dniEnUso = await _persistence.FirstIgnoringFilters<Patient>(p => p.Dni == dni);
            if (dniEnUso is not null)
                throw new ConflictException(nameof(ErrorCodes.PATIENT_DNI_CONFLICT),
                    ErrorCodes.PATIENT_DNI_CONFLICT);
            user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
                throw new ConflictException(nameof(ErrorCodes.PATIENT_LOGIN_CONFLICT),
                    ErrorCodes.PATIENT_LOGIN_CONFLICT);

            try
            {
                var rolAniadido = await _userManager.AddToRoleAsync(user, Roles.Patient);
                if (!rolAniadido.Succeeded)
                    throw new ConflictException(nameof(ErrorCodes.PATIENT_LOGIN_CONFLICT),
                        ErrorCodes.PATIENT_LOGIN_CONFLICT);

                await _persistence.Add(new Patient(user.Id, dni));
            }
            catch
            {
                var rollback = await _userManager.DeleteAsync(user);
                if (!rollback.Succeeded)
                    _logger.LogError("Rollback fallido, usuario huérfano: {Email}", request.Email);
                throw;
            }
            _logger.LogInformation("Paciente auto-registrado: {Email}", request.Email);
        }
        else
        {
            var patient = await _persistence.FirstIgnoringFilters<Patient>(p => p.UserId == user.Id);
            if (patient is null || patient.Deleted || patient.Dni != dni)
            {
                _logger.LogWarning("Login de paciente fallido para: {Email}", request.Email);
                throw new AuthenticationException();
            }
        }
        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
        var token = _jwtService.GenerateToken(user.UserName!, role);
        _logger.LogInformation("Login de paciente exitoso: {Email}", request.Email);
        return new LoginPatientModel.Response(token, role);
    }

    public async Task<RegisterModel.Response> Register(RegisterModel.Request request)
    {
        if (!request.Email.IsEmailValid()) throw new ValidationException(nameof(ErrorCodes.REGISTER_USER_INVALID),
            ErrorCodes.REGISTER_USER_INVALID);

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded) throw new ConflictException(nameof(ErrorCodes.REGISTER_USER_CONFLICT),
            ErrorCodes.REGISTER_USER_CONFLICT)
                .WithDetail(result.Errors.Select(e => (e.Code, e.Description)));

        _ = await _userManager.AddToRoleAsync(user, Roles.Administrator);

        _logger.LogInformation("Usuario registrado: {Email}", request.Email);

        return new RegisterModel.Response(request.Email);
    }
}
