using FCG.Payments.Application.Configure;
using FCG.Payments.Application.Interfaces;
using FCG.Payments.Domain.Interfaces;
using FCG.Payments.Infra;
using FCG.Payments.Infra.Consumers;
using FCG.Payments.Infra.Repositories;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Testcontainers.PostgreSql;

namespace FCG.Payments.Tests.Integration.Config;

public class WorkerTestFactory : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("fcg_payments_test")
        .WithUsername("fcg")
        .WithPassword("fcg_secret")
        .Build();

    public IHost Host { get; private set; } = null!;
    public Mock<IApprovalStrategy> ApprovalStrategyMock { get; } = new();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddDbContext<AppDbContext>(opts =>
                    opts.UseNpgsql(_dbContainer.GetConnectionString())
                        .UseSnakeCaseNamingConvention());

                services.AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));
                services.AddScoped<IPaymentRepository, PaymentRepository>();

                // AddApplicationConfiguration registra RandomApprovalStrategy;
                // o AddSingleton seguinte sobrescreve com o mock para testes determinísticos.
                services.AddApplicationConfiguration();
                services.AddSingleton<IApprovalStrategy>(ApprovalStrategyMock.Object);

                services.AddMassTransitTestHarness(x =>
                    x.AddConsumer<OrderPlacedConsumer>());
            })
            .Build();

        using var scope = Host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        await Host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Host.StopAsync();
        Host.Dispose();
        await _dbContainer.DisposeAsync();
    }

    public ITestHarness GetTestHarness()
        => Host.Services.GetRequiredService<ITestHarness>();

    public async Task ResetDatabaseAsync()
    {
        using var scope = Host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Payments.RemoveRange(db.Payments);
        await db.SaveChangesAsync();
    }
}
