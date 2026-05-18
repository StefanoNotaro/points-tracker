using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PointsTracker.Application.Services;
using PointsTracker.Domain.Interfaces;
using PointsTracker.Infrastructure.Auth;
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
        services.AddScoped<IRoleAuditLogRepository, RoleAuditLogRepository>();
        services.AddScoped<ICleanupAuditLogRepository, CleanupAuditLogRepository>();
        services.AddScoped<ITournamentRepository, TournamentRepository>();
        services.AddScoped<IMatchScorerLinkRepository, MatchScorerLinkRepository>();

        services.AddSingleton<IShareTokenService, ShareTokenService>();
        services.AddScoped<ICounterAuthorizationService, CounterAuthorizationService>();
        services.AddScoped<ICounterMapper, CounterMapper>();
        services.AddScoped<ITournamentAuthorizationService, TournamentAuthorizationService>();
        services.AddScoped<ITournamentMapper, TournamentMapper>();
        services.AddScoped<ITournamentLiveBridge, TournamentLiveBridge>();
        services.AddScoped<IUserSyncService, UserSyncService>();

        // SignalR is registered in the API layer so its JsonProtocol options
        // can be configured alongside the rest of the API JSON pipeline.

        services.Configure<CounterCleanupOptions>(config.GetSection(CounterCleanupOptions.SectionName));
        services.AddScoped<ICleanupService, CleanupService>();
        services.AddHostedService<CounterCleanupService>();

        return services;
    }
}
