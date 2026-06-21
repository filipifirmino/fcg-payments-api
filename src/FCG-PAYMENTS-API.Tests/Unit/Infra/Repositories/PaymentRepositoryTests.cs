using Bogus;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using FCG.Payments.Infra;
using FCG.Payments.Infra.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FCG.Payments.Tests.Unit.Infra.Repositories;

public class PaymentRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly PaymentRepository _sut;
    private readonly Faker _faker = new("pt_BR");

    public PaymentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _sut = new PaymentRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    private Payment BuildPayment(PaymentStatus status = PaymentStatus.Approved) =>
        Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            _faker.Commerce.ProductName(),
            _faker.Internet.Email(),
            _faker.Random.Decimal(1, 500),
            status,
            status == PaymentStatus.Rejected ? "Insufficient funds (simulated)" : null);

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WhenHasPayments_ShouldReturnAll()
    {
        await _sut.AddAsync(BuildPayment());
        await _sut.AddAsync(BuildPayment());
        await _sut.AddAsync(BuildPayment());

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ShouldReturnPayment()
    {
        var payment = await _sut.AddAsync(BuildPayment());

        var result = await _sut.GetByIdAsync(payment!.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(payment.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByOrderIdAsync_WhenExists_ShouldReturnPayment()
    {
        var payment = BuildPayment();
        await _sut.AddAsync(payment);

        var result = await _sut.GetByOrderIdAsync(payment.OrderId);

        result.Should().NotBeNull();
        result!.OrderId.Should().Be(payment.OrderId);
    }

    [Fact]
    public async Task GetByOrderIdAsync_WhenNotExists_ShouldReturnNull()
    {
        var result = await _sut.GetByOrderIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistPayment()
    {
        var payment = BuildPayment();

        await _sut.AddAsync(payment);

        var persisted = await _sut.GetByIdAsync(payment.Id);
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldReturnPersistedPayment()
    {
        var payment = BuildPayment();

        var result = await _sut.AddAsync(payment);

        result.Should().NotBeNull();
        result!.Id.Should().Be(payment.Id);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemovePayment()
    {
        var payment = await _sut.AddAsync(BuildPayment());

        await _sut.DeleteAsync(payment!);

        var result = await _sut.GetByIdAsync(payment!.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDecrementCount()
    {
        await _sut.AddAsync(BuildPayment());
        var toDelete = await _sut.AddAsync(BuildPayment());
        await _sut.AddAsync(BuildPayment());

        await _sut.DeleteAsync(toDelete!);

        var result = await _sut.GetAllAsync();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByOrderIdAsync_WithMultiplePayments_ShouldReturnCorrectOne()
    {
        var target = BuildPayment();
        await _sut.AddAsync(BuildPayment());
        await _sut.AddAsync(target);
        await _sut.AddAsync(BuildPayment());

        var result = await _sut.GetByOrderIdAsync(target.OrderId);

        result.Should().NotBeNull();
        result!.OrderId.Should().Be(target.OrderId);
    }

    [Fact]
    public async Task FullLifecycle_AddGetDelete_ShouldWorkCorrectly()
    {
        var payment = BuildPayment(PaymentStatus.Rejected);

        await _sut.AddAsync(payment);
        var fetched = await _sut.GetByIdAsync(payment.Id);
        fetched.Should().NotBeNull();
        fetched!.Status.Should().Be(PaymentStatus.Rejected);

        await _sut.DeleteAsync(fetched);
        var deleted = await _sut.GetByIdAsync(payment.Id);
        deleted.Should().BeNull();
    }
}
