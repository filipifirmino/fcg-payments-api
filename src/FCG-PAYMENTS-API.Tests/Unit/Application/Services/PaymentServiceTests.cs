using Bogus;
using FCG.Events;
using FCG.Payments.Application.Interfaces;
using FCG.Payments.Application.Services;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using FCG.Payments.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FCG.Payments.Tests.Unit.Application.Services;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _repositoryMock = new();
    private readonly Mock<IApprovalStrategy> _approvalStrategyMock = new();
    private readonly Mock<ILogger<PaymentService>> _loggerMock = new();
    private readonly PaymentService _sut;
    private readonly Faker _faker = new("pt_BR");

    public PaymentServiceTests()
    {
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);

        _sut = new PaymentService(_repositoryMock.Object, _approvalStrategyMock.Object, _loggerMock.Object);
    }

    private (Guid orderId, Guid userId, Guid gameId, string gameTitle, string userEmail, decimal amount) BuildOrderArgs()
        => (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            _faker.Commerce.ProductName(), _faker.Internet.Email(), _faker.Random.Decimal(1, 500));

    [Fact]
    public async Task ProcessAsync_WhenApproved_ShouldReturnPaymentWithApprovedStatus()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var (orderId, userId, gameId, title, email, amount) = BuildOrderArgs();

        var result = await _sut.ProcessAsync(orderId, userId, gameId, title, email, amount);

        result.Status.Should().Be(PaymentStatus.Approved);
    }

    [Fact]
    public async Task ProcessAsync_WhenRejected_ShouldReturnPaymentWithRejectedStatus()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(false);
        var (orderId, userId, gameId, title, email, amount) = BuildOrderArgs();

        var result = await _sut.ProcessAsync(orderId, userId, gameId, title, email, amount);

        result.Status.Should().Be(PaymentStatus.Rejected);
    }

    [Fact]
    public async Task ProcessAsync_WhenApproved_ShouldReturnPaymentWithNullReason()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var (orderId, userId, gameId, title, email, amount) = BuildOrderArgs();

        var result = await _sut.ProcessAsync(orderId, userId, gameId, title, email, amount);

        result.Reason.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_WhenRejected_ShouldReturnPaymentWithReason()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(false);
        var (orderId, userId, gameId, title, email, amount) = BuildOrderArgs();

        var result = await _sut.ProcessAsync(orderId, userId, gameId, title, email, amount);

        result.Reason.Should().Be("Insufficient funds (simulated)");
    }

    [Fact]
    public async Task ProcessAsync_ShouldMapOrderIdToPayment()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var orderId = Guid.NewGuid();
        var (_, userId, gameId, title, email, amount) = BuildOrderArgs();

        var result = await _sut.ProcessAsync(orderId, userId, gameId, title, email, amount);

        result.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task ProcessAsync_ShouldMapUserIdToPayment()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var userId = Guid.NewGuid();
        var (orderId, _, gameId, title, email, amount) = BuildOrderArgs();

        var result = await _sut.ProcessAsync(orderId, userId, gameId, title, email, amount);

        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task ProcessAsync_ShouldMapGameIdToPayment()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var gameId = Guid.NewGuid();
        var (orderId, userId, _, title, email, amount) = BuildOrderArgs();

        var result = await _sut.ProcessAsync(orderId, userId, gameId, title, email, amount);

        result.GameId.Should().Be(gameId);
    }

    [Fact]
    public async Task ProcessAsync_ShouldMapAmountToPayment()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var amount = 59.90m;
        var (orderId, userId, gameId, title, email, _) = BuildOrderArgs();

        var result = await _sut.ProcessAsync(orderId, userId, gameId, title, email, amount);

        result.Amount.Should().Be(amount);
    }

    [Fact]
    public async Task ProcessAsync_ShouldMapGameTitleToPayment()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var gameTitle = "The Last of Us Part II";
        var (orderId, userId, gameId, _, email, amount) = BuildOrderArgs();

        var result = await _sut.ProcessAsync(orderId, userId, gameId, gameTitle, email, amount);

        result.GameTitle.Should().Be(gameTitle);
    }

    [Fact]
    public async Task ProcessAsync_ShouldMapUserEmailToPayment()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var userEmail = "player@fcg.com";
        var (orderId, userId, gameId, title, _, amount) = BuildOrderArgs();

        var result = await _sut.ProcessAsync(orderId, userId, gameId, title, userEmail, amount);

        result.UserEmail.Should().Be(userEmail);
    }

    [Fact]
    public async Task ProcessAsync_ShouldSetProcessedAtToApproximatelyNow()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var before = DateTime.UtcNow;
        var (orderId, userId, gameId, title, email, amount) = BuildOrderArgs();

        var result = await _sut.ProcessAsync(orderId, userId, gameId, title, email, amount);

        result.ProcessedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task ProcessAsync_ShouldGenerateNewPaymentId()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var (orderId, userId, gameId, title, email, amount) = BuildOrderArgs();

        var result = await _sut.ProcessAsync(orderId, userId, gameId, title, email, amount);

        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task ProcessAsync_ShouldCallRepositoryAddAsyncOnce()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var (orderId, userId, gameId, title, email, amount) = BuildOrderArgs();

        await _sut.ProcessAsync(orderId, userId, gameId, title, email, amount);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ShouldCallApprovalStrategyOnce()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var (orderId, userId, gameId, title, email, amount) = BuildOrderArgs();

        await _sut.ProcessAsync(orderId, userId, gameId, title, email, amount);

        _approvalStrategyMock.Verify(s => s.IsApproved(), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_TwoConsecutiveCalls_ShouldGenerateDifferentPaymentIds()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var (orderId1, userId, gameId, title, email, amount) = BuildOrderArgs();
        var orderId2 = Guid.NewGuid();

        var result1 = await _sut.ProcessAsync(orderId1, userId, gameId, title, email, amount);
        var result2 = await _sut.ProcessAsync(orderId2, userId, gameId, title, email, amount);

        result1.Id.Should().NotBe(result2.Id);
    }

    [Fact]
    public async Task ProcessAsync_WithZeroAmount_ShouldCreatePaymentSuccessfully()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        var (orderId, userId, gameId, title, email, _) = BuildOrderArgs();

        var result = await _sut.ProcessAsync(orderId, userId, gameId, title, email, 0m);

        result.Amount.Should().Be(0m);
        result.Status.Should().Be(PaymentStatus.Approved);
    }
}
