using FCG.Payments.Application.Interfaces;
using FCG.Payments.Application.Services;
using FCG.Payments.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Payments.Application.Configure;

public static class ApplicationConfigure
{
    public static IServiceCollection AddApplicationConfiguration(this IServiceCollection services)
    {
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddSingleton<IApprovalStrategy, RandomApprovalStrategy>();
        return services;
    }
}
