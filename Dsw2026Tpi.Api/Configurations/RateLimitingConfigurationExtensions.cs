using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace Dsw2026Tpi.Api.Configurations;

public static class RateLimitingConfigurationExtensions
{
    public const string LoginPolicy = "login";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var globalPermitLimit = configuration.GetValue<int?>("RateLimiting:Global:PermitLimit") ?? 200;
        var globalWindow = configuration.GetValue<int?>("RateLimiting:Global:WindowInSeconds") ?? 60;
        var loginPermitLimit = configuration.GetValue<int?>("RateLimiting:Login:PermitLimit") ?? 10;
        var loginWindow = configuration.GetValue<int?>("RateLimiting:Login:WindowInSeconds") ?? 60;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = globalPermitLimit,
                        Window = TimeSpan.FromSeconds(globalWindow),
                        QueueLimit = 0
                    }));

            options.AddPolicy(LoginPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = loginPermitLimit,
                        Window = TimeSpan.FromSeconds(loginWindow),
                        QueueLimit = 0
                    }));

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
                    "Solicitud rechazada por límite de tasa. IP: {Ip}, Ruta: {Path}",
                    GetPartitionKey(context.HttpContext),
                    context.HttpContext.Request.Path);

                var error = new ErrorResponse(
                    nameof(ErrorCodes.TOO_MANY_REQUESTS),
                    ErrorCodes.TOO_MANY_REQUESTS);

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(error, JsonOptions),
                    cancellationToken);
            };
        });

        return services;
    }

    private static string GetPartitionKey(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}