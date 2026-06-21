using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FCG.Payments.Infra;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.OrderId).IsRequired();
            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.GameId).IsRequired();
            entity.Property(p => p.GameTitle).IsRequired().HasMaxLength(200);
            entity.Property(p => p.UserEmail).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Amount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(p => p.Status)
                .IsRequired()
                .HasConversion(new EnumToStringConverter<PaymentStatus>());
            entity.Property(p => p.Reason).HasMaxLength(500);
            entity.Property(p => p.ProcessedAt).IsRequired();

            entity.HasIndex(p => p.OrderId);
        });
    }
}
