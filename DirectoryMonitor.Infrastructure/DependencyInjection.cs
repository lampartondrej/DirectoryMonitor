using DirectoryMonitor.Application.Interfaces;
using DirectoryMonitor.Application.Services;
using DirectoryMonitor.Infrastructure.Persistence;
using DirectoryMonitor.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DirectoryMonitor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string snapshotStoragePath)
    {
        // Infrastructure
        services.AddScoped<IFileScanner, FileScanner>();
        services.AddScoped<IHashingService, HashingService>();
        services.AddScoped<ISnapshotRepository>(sp =>
            new SnapshotRepository(
                snapshotStoragePath,
                sp.GetRequiredService<ILogger<SnapshotRepository>>()));

        // Application services
        services.AddScoped<ISnapshotBuilder, SnapshotBuilder>();
        services.AddScoped<IChangeDetectionService, ChangeDetectionService>();
        services.AddScoped<IDirectoryAnalysisService, DirectoryAnalysisService>();

        return services;
    }
}
