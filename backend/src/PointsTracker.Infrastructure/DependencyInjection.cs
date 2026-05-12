using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Interfaces;
using PointsTracker.Infrastructure.Auth;
using PointsTracker.Infrastructure.Hubs;
using PointsTracker.Infrastructure.Persistence;
using PointsTracker.Infrastructure.Repositories;
using PointsTracker.Infrastructure.Services;

namespace PointsTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("Default"),
                npg => npg.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));

        services.AddScoped<ICounterRepository, CounterRepository>();
        services.AddScoped<IShareTokenRepository, ShareTokenRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddSingleton<IShareTokenService, ShareTokenService>();
        services.AddScoped<ICounterAuthorizationService, CounterAuthorizationService>();
        services.AddScoped<ICounterMapper, CounterMapper>();
        services.AddScoped<UserSyncService>();

        services.AddSignalR();

        return services;
    }
}
