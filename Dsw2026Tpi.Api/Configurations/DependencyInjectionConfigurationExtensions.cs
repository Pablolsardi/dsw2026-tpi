using Dsw2026Tpi.Api.Services;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Api.Configurations;

public static class DependencyInjectionConfigurationExtensions
{
    public static IServiceCollection AddAppDependencies(this IServiceCollection services)
    {
        services.AddScoped<IPersistence, PersistenceEf>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ISignInService, SignInService>();
        services.AddScoped<ISpecialityService, SpecialityService>();
        services.AddSingleton<IHolidayService>(_ => new HolidayService(Path.Combine("Sources", "holidays.json")));
        services.AddSingleton<JwtService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        return services;
    }
}
