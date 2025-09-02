using CodeUI.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CodeUI.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeUiCoreServices(this IServiceCollection services)
    {
        // CLI execution services
        services.AddScoped<ICliExecutor, CliExecutor>();

        // File system services
        services.AddScoped<IFileSystemService, FileSystemService>();

        // Git and Diff services
        services.AddScoped<IGitService, GitService>();
        services.AddScoped<IDiffService, DiffService>();

        return services;
    }
}

