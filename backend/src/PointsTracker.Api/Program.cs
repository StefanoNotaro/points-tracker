using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using PointsTracker.Api.Endpoints;
using PointsTracker.Api.Middleware;
using PointsTracker.Application.Common;
using PointsTracker.Infrastructure;
using PointsTracker.Infrastructure.Auth;
using PointsTracker.Infrastructure.Hubs;
using PointsTracker.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

// Application + Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT Authentication via Authentik OIDC
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        var authority = builder.Configuration["Authentik:Authority"];
        var clientId = builder.Configuration["Authentik:ClientId"];

        opts.Authority = authority;
        opts.Audience = clientId;
        opts.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        opts.TokenValidationParameters.ValidateAudience = true;
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },

            // Sync the external identity into our users table once per request and
            // attach pts_id / pts_role claims, so ctx.User.GetUserId() resolves the
            // internal Guid that ownership checks compare against.
            OnTokenValidated = async context =>
            {
                if (context.Principal is null) return;

                var sync = context.HttpContext.RequestServices.GetRequiredService<UserSyncService>();
                try
                {
                    var user = await sync.SyncAsync(context.Principal, context.HttpContext.RequestAborted);

                    var identity = context.Principal.Identity as ClaimsIdentity;
                    if (identity is not null && !identity.HasClaim(c => c.Type == "pts_id"))
                    {
                        identity.AddClaim(new Claim("pts_id", user.Id.ToString()));
                        identity.AddClaim(new Claim("pts_role", user.Role));
                    }
                }
                catch (Exception ex)
                {
                    // Don't reject the token just because user-sync failed; log and continue.
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(ex, "User sync failed during token validation");
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(opts => opts.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"])
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials()));

// SignalR is also registered in Infrastructure; configure JSON here so payloads
// match the camelCase contract the Angular client expects (Counter.teamAName, etc).
builder.Services.AddSignalR()
    .AddJsonProtocol(opts =>
    {
        opts.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.PayloadSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Map SignalR's UserIdentifier to our internal pts_id so per-user broadcast
// groups (user-{OwnerUserId}) line up with what the client joins.
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, PointsTracker.Infrastructure.Hubs.PtsIdUserIdProvider>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Rate limiting
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("write", cfg =>
    {
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.PermitLimit = 60;
    });
    opts.AddFixedWindowLimiter("create", cfg =>
    {
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.PermitLimit = 10;
    });
});

var app = builder.Build();

// Run EF migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Middleware pipeline
app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoints
app.MapHealthEndpoints();
app.MapCounterEndpoints();
app.MapTournamentEndpoints();
app.MapHub<CounterHub>("/hubs/counter");
app.MapHub<TournamentHub>("/hubs/tournament");

app.Run();

public partial class Program { }
