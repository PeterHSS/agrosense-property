using Api.Common;
using Api.Common.Middlewares;
using Api.Domain.Abstractions.UseCases;
using Api.Features.Plot.Create;
using Api.Features.Plot.Delete;
using Api.Features.Plot.Update;
using Api.Features.Property;
using Api.Features.Property.Create;
using Api.Features.Property.GetPropertiesFromProducer;
using Api.Infrastructure.Persistence.Contexts;
using Api.Infrastructure.Providers;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Api;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddDependecyInjection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);
        services.AddApplication();
        services.AddPresentation(configuration);
        return services;
    }

    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddOpenApi();
        services.AddJwtAuthenticationAndAuthorization(configuration);

        return services;
    }

    private static IServiceCollection AddJwtAuthenticationAndAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services
            .AddAuthorizationBuilder()
            .AddPolicy(Policies.UserOnly, policy => policy.RequireRole(nameof(Roles.User)))
            .AddPolicy(Policies.AdministratorOnly, policy => policy.RequireRole(nameof(Roles.Admin)));

        return services;
    }

    public static void ApplyMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();

        using var context = scope.ServiceProvider.GetRequiredService<AgroSenseDbContext>();

        context.Database.Migrate();
    }

    private static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddContexts(configuration);
        services.AddProviders();
        services.AddHttpContextAccessor();

        return services;
    }

    private static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidators();
        services.AddUseCases();

        return services;
    }

    private static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<IUseCase<CreatePropertyRequest>, CreatePropertyUseCase>();
        services.AddScoped<IUseCase<GetFromCurrentUser, IEnumerable<PropertyResponse>>, GetPropertiesFromProducerUseCase>();

        services.AddScoped<IUseCase<CreatePlotRequest>, CreatePlotUseCase>();
        services.AddScoped<IUseCase<DeletePlotRequest>, DeletePlotUseCase>();
        services.AddScoped<IUseCase<UpdatePlotRequest>, UpdatePlotUseCase>();

        return services;
    }

    private static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjectionExtension).Assembly, ServiceLifetime.Scoped, includeInternalTypes: true);

        return services;
    }

    private static IServiceCollection AddProviders(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();

        return services;
    }

    private static IServiceCollection AddContexts(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionString not configured.");

        services.AddDbContext<AgroSenseDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
