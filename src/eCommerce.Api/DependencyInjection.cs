using eCommerce.Api.Abstractions.Messaging;
using eCommerce.Api.Database;
using eCommerce.Api.Enums;
using eCommerce.Api.Options;
using eCommerce.Api.Services.Auth;
using eCommerce.Api.Services.Payments.PayPal;
using eCommerce.Api.Shared.Behaviors;
using eCommerce.Api.Shared.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;

namespace eCommerce.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("EcommerceConnection")!;
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

        services.Configure<PayPalOptions>(configuration.GetSection(PayPalOptions.SectionName));
        services.AddHttpClient<IPayPalService, PayPalService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PayPalOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });
        services.AddScoped<IPayPalPaymentStore, PayPalPaymentStore>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("La sección Jwt no está configurada.");

        // Se configura la validación del JWT para futuros endpoints protegidos.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization(options =>
        {
            // Política para endpoints generales consumibles por usuarios autenticados.
            options.AddPolicy(AuthPolicies.UserAccess, policy =>
                policy.RequireRole(UserType.USER.ToString(), UserType.ADMIN.ToString()));

            // Política reservada para procesos administrativos.
            options.AddPolicy(AuthPolicies.AdminAccess, policy =>
                policy.RequireRole(UserType.ADMIN.ToString()));

            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

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
                // Registra cada handler con su interfaz correspondiente.
                services.AddScoped(handlerInterface, handlerType);
            }
        }
    }
}
