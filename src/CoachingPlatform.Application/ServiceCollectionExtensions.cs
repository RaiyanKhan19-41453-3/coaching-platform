using System.Reflection;
using CoachingPlatform.Application.Behaviors;
using CoachingPlatform.Application.Features.Identity;
using CoachingPlatform.Application.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CoachingPlatform.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(TenantGuardBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<TokenIssuer>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAcademicsService, AcademicsService>();
        services.AddScoped<ISchedulingService, SchedulingService>();
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IReportingService, ReportingService>();
        return services;
    }
}
