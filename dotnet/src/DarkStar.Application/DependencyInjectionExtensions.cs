using DarkStar.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DarkStar.Application;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddDarkStarApplication(this IServiceCollection services)
    {
        services.AddScoped<CryptoApplicationService>();
        services.AddScoped<AuditApplicationService>();
        services.AddScoped<BackupApplicationService>();
        services.AddScoped<KeyApplicationService>();
        services.AddScoped<ContactApplicationService>();
        services.AddScoped<WorkspaceApplicationService>();
        return services;
    }
}
