using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace Dsw2026Tpi.Api.Configurations;

public static class RateLimitingConfigurationExtensions
{
    public const string AdminLoginPolicy = "admin-login";
    public const string PatientLoginPolicy = "patient-login";
    public const string AppointmentBookingPolicy = "appointment-booking";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IServiceCollection AddAppRateLimiting(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            var (limiteGlobal, ventanaGlobal) = LeerPolitica(configuration, "Global", 100);
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => Particionar(ClaveUsuarioOIp(context), limiteGlobal, ventanaGlobal));

            AgregarPolitica(options, configuration, AdminLoginPolicy, "AdminLogin", 5, ClaveIp);
            AgregarPolitica(options, configuration, PatientLoginPolicy, "PatientLogin", 10, ClaveIp);

            AgregarPolitica(options, configuration, AppointmentBookingPolicy,
                "AppointmentBooking", 5, ClaveUsuarioOIp);

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("RateLimiting");

                logger.LogWarning(
                    "Solicitud rechazada por limite de tasa. Particion: {Particion}, Ruta: {Path}",
                    ClaveUsuarioOIp(context.HttpContext),
                    context.HttpContext.Request.Path);

                var error = new ErrorResponse(
                    nameof(ErrorCodes.TOO_MANY_REQUESTS),
                    ErrorCodes.TOO_MANY_REQUESTS);

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(error, JsonOptions), cancellationToken);
            };
        });

        return services;
    }

    private static void AgregarPolitica(
        RateLimiterOptions options,
        IConfiguration configuration,
        string nombrePolitica,
        string seccion,
        int limitePorDefecto,
        Func<HttpContext, string> clave)
    {
        var (limite, ventana) = LeerPolitica(configuration, seccion, limitePorDefecto);
        options.AddPolicy(nombrePolitica,
            context => Particionar(clave(context), limite, ventana));
    }

    private static RateLimitPartition<string> Particionar(
        string clave, int limite, int ventanaEnSegundos)
        => RateLimitPartition.GetFixedWindowLimiter(clave, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limite,
            Window = TimeSpan.FromSeconds(ventanaEnSegundos),
            QueueLimit = 0
        });

    private static (int Limite, int Ventana) LeerPolitica(
        IConfiguration configuration, string seccion, int limitePorDefecto)
        => (configuration.GetValue<int?>($"RateLimiting:{seccion}:PermitLimit") ?? limitePorDefecto,
            configuration.GetValue<int?>($"RateLimiting:{seccion}:WindowInSeconds") ?? 60);

    private static string ClaveIp(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

    private static string ClaveUsuarioOIp(HttpContext context)
    {
        var usuario = context.User?.FindFirst(ClaimTypes.Name)?.Value;
        return string.IsNullOrWhiteSpace(usuario)
            ? "ip:" + ClaveIp(context)
            : "usuario:" + usuario;
    }
}