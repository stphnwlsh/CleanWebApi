namespace CleanWebApi.Application;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Common.Behaviours;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        _ = services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient);
        services.TryAddSingleton(TimeProvider.System);

        _ = services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        _ = services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        return services;
    }
}
