using FCG.Payments.Application.Configure;
using FCG.Payments.Infra;
using FCG.Payments.Infra.Configure;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((_, config) =>
    config.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddConfigureInfra(builder.Configuration);
builder.Services.AddApplicationConfiguration();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
