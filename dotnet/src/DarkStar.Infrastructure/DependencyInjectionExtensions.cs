using DarkStar.Application.Abstractions;
using DarkStar.Infrastructure.Crypto;
using DarkStar.Infrastructure.Options;
using DarkStar.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DarkStar.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddDarkStarInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DarkStarStorageOptions>(options =>
        {
            var envHome = Environment.GetEnvironmentVariable("DARKSTAR_HOME");
            var configuredHome = configuration["Storage:HomePath"];

            options.HomePath =
                !string.IsNullOrWhiteSpace(envHome)
                    ? envHome
                    : !string.IsNullOrWhiteSpace(configuredHome)
                        ? configuredHome
                        : options.HomePath;
        });

        services.AddScoped<IEncryptionEngine, AesGcmEncryptionEngine>();
        services.AddScoped<ISignatureEngine, HmacSignatureEngine>();
        services.AddScoped<IAuditRepository, FileAuditRepository>();
        services.AddScoped<IKeyRepository, FileKeyRepository>();
        services.AddScoped<IContactRepository, FileContactRepository>();

        return services;
    }
}
