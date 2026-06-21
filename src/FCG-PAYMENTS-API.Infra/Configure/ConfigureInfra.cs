using FCG.Payments.Domain.Interfaces;
using FCG.Payments.Infra.Consumers;
using FCG.Payments.Infra.Repositories;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Payments.Infra.Configure;

public static class ConfigureInfra
{
    public static IServiceCollection AddConfigureInfra(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddRepositories();
        services.AddRabbitMq(config);
        return services;
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));
        services.AddScoped<IPaymentRepository, PaymentRepository>();
    }

    private static void AddRabbitMq(this IServiceCollection services, IConfiguration config)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<OrderPlacedConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(config["RabbitMq:Host"] ?? "localhost", h =>
                {
                    h.Username(config["RabbitMq:Username"] ?? "guest");
                    h.Password(config["RabbitMq:Password"] ?? "guest");
                });

                cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                cfg.ConfigureEndpoints(ctx);
            });
        });
    }
}
