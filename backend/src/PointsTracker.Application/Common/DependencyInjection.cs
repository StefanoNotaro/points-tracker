using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PointsTracker.Application.Common.Behaviors;

namespace PointsTracker.Application.Common;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Behaviors execute in registration order (outermost first), so logging
        // wraps validation and captures validation failures with the request name.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
