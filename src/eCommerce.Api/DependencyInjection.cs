using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Shared.Behaviors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace eCommerce.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("EcommerceConnection")!;
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<HandlerExecutor>();
        services.AddScoped<IValidationService, ValidationService>();

        services.AddHandlersFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IDispatcher, Dispatcher>();
        return services;
    }

    private static void AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                     i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))));

        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                     i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)));

            foreach (var handlerInterface in interfaces)
            {
                // Registra cada handler con su interfaz correspondiente
                services.AddScoped(handlerInterface, handlerType);
            }
        }
    }
}
