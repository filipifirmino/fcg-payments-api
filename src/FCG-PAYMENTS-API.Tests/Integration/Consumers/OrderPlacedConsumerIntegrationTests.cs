using Bogus;
using FCG.Events;
using FCG.Payments.Domain.Enums;
using FCG.Payments.Domain.Interfaces;
using FCG.Payments.Infra;
using FCG.Payments.Tests.Integration.Config;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace FCG.Payments.Tests.Integration.Consumers;

public class OrderPlacedConsumerIntegrationTests : IClassFixture<WorkerTestFactory>, IAsyncLifetime
{
    private readonly WorkerTestFactory _factory;
    private readonly ITestHarness _harness;
    private readonly Faker _faker = new("pt_BR");

    public OrderPlacedConsumerIntegrationTests(WorkerTestFactory factory)
    {
        _factory = factory;
        _harness = factory.GetTestHarness();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private OrderPlacedEvent BuildEvent() => new()
    {
        OrderId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        GameId = Guid.NewGuid(),
        GameTitle = _faker.Commerce.ProductName(),
        UserEmail = _faker.Internet.Email(),
        Price = _faker.Random.Decimal(1, 500),
        PlacedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Consume_ValidOrderPlacedEvent_ShouldBeConsumedByConsumer()
    {
        _factory.ApprovalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var @event = BuildEvent();

        await _harness.Bus.Publish(@event);

        (await _harness.Consumed.Any<OrderPlacedEvent>()).Should().BeTrue();
    }

    [Fact]
    public async Task Consume_ValidOrderPlacedEvent_ShouldPublishPaymentProcessedEvent()
    {
        _factory.ApprovalStrategyMock.Setup(s => s.IsApproved()).Returns(true);

        await _harness.Bus.Publish(BuildEvent());

        (await _harness.Published.Any<PaymentProcessedEvent>()).Should().BeTrue();
    }

    [Fact]
    public async Task Consume_WhenApproved_ShouldPublishApprovedPaymentEvent()
    {
        _factory.ApprovalStrategyMock.Setup(s => s.IsApproved()).Returns(true);

        await _harness.Bus.Publish(BuildEvent());

        await Task.Delay(500);
        var published = _harness.Published.Select<PaymentProcessedEvent>().LastOrDefault();
        published.Should().NotBeNull();
        published!.Context.Message.Status.Should().Be(PaymentStatus.Approved);
    }

    [Fact]
    public async Task Consume_WhenRejected_ShouldPublishRejectedPaymentEvent()
    {
        _factory.ApprovalStrategyMock.Setup(s => s.IsApproved()).Returns(false);

        await _harness.Bus.Publish(BuildEvent());

        await Task.Delay(500);
        var published = _harness.Published.Select<PaymentProcessedEvent>().LastOrDefault();
        published.Should().NotBeNull();
        published!.Context.Message.Status.Should().Be(PaymentStatus.Rejected);
    }

    [Fact]
    public async Task Consume_WhenRejected_ShouldPublishEventWithReason()
    {
        _factory.ApprovalStrategyMock.Setup(s => s.IsApproved()).Returns(false);

        await _harness.Bus.Publish(BuildEvent());

        await Task.Delay(500);
        var published = _harness.Published.Select<PaymentProcessedEvent>().LastOrDefault();
        published!.Context.Message.Reason.Should().Be("Insufficient funds (simulated)");
    }

    [Fact]
    public async Task Consume_ShouldPublishEventWithMatchingOrderId()
    {
        _factory.ApprovalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var @event = BuildEvent();

        await _harness.Bus.Publish(@event);

        await Task.Delay(500);
        var published = _harness.Published.Select<PaymentProcessedEvent>().LastOrDefault();
        published!.Context.Message.OrderId.Should().Be(@event.OrderId);
    }

    [Fact]
    public async Task Consume_ShouldPublishEventWithMatchingAmount()
    {
        _factory.ApprovalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var @event = BuildEvent();

        await _harness.Bus.Publish(@event);

        await Task.Delay(500);
        var published = _harness.Published.Select<PaymentProcessedEvent>().LastOrDefault();
        published!.Context.Message.Amount.Should().Be(@event.Price);
    }

    [Fact]
    public async Task Consume_ShouldPersistPaymentToDatabase()
    {
        _factory.ApprovalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var @event = BuildEvent();

        await _harness.Bus.Publish(@event);
        await Task.Delay(500);

        using var scope = _factory.Host.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var payment = await repository.GetByOrderIdAsync(@event.OrderId);

        payment.Should().NotBeNull();
        payment!.OrderId.Should().Be(@event.OrderId);
    }

    [Fact]
    public async Task Consume_ShouldPersistPaymentWithCorrectStatus_WhenApproved()
    {
        _factory.ApprovalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var @event = BuildEvent();

        await _harness.Bus.Publish(@event);
        await Task.Delay(500);

        using var scope = _factory.Host.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var payment = await repository.GetByOrderIdAsync(@event.OrderId);

        payment!.Status.Should().Be(PaymentStatus.Approved);
    }

    [Fact]
    public async Task Consume_ShouldPersistPaymentWithCorrectStatus_WhenRejected()
    {
        _factory.ApprovalStrategyMock.Setup(s => s.IsApproved()).Returns(false);
        var @event = BuildEvent();

        await _harness.Bus.Publish(@event);
        await Task.Delay(500);

        using var scope = _factory.Host.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var payment = await repository.GetByOrderIdAsync(@event.OrderId);

        payment!.Status.Should().Be(PaymentStatus.Rejected);
    }

    [Fact]
    public async Task Consume_MultipleEvents_ShouldProcessEachIndependently()
    {
        _factory.ApprovalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var event1 = BuildEvent();
        var event2 = BuildEvent();

        await _harness.Bus.Publish(event1);
        await _harness.Bus.Publish(event2);
        await Task.Delay(1000);

        using var scope = _factory.Host.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

        var payment1 = await repository.GetByOrderIdAsync(event1.OrderId);
        var payment2 = await repository.GetByOrderIdAsync(event2.OrderId);

        payment1.Should().NotBeNull();
        payment2.Should().NotBeNull();
        payment1!.Id.Should().NotBe(payment2!.Id);
    }

    [Fact]
    public async Task Consume_WhenApproved_ShouldPublishEventWithNullReason()
    {
        _factory.ApprovalStrategyMock.Setup(s => s.IsApproved()).Returns(true);

        await _harness.Bus.Publish(BuildEvent());

        await Task.Delay(500);
        var published = _harness.Published.Select<PaymentProcessedEvent>().LastOrDefault();
        published!.Context.Message.Reason.Should().BeNull();
    }
}
