using Bogus;
using FCG.Payments.Application.Interfaces;
using FCG.Payments.Application.Services;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using FCG.Payments.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Reqnroll;

namespace FCG.Payments.Tests.BDD.StepDefinitions;

[Binding]
public class PaymentServiceSteps
{
    private readonly Mock<IPaymentRepository> _repositoryMock = new();
    private readonly Mock<IApprovalStrategy> _approvalStrategyMock = new();
    private readonly Mock<ILogger<PaymentService>> _loggerMock = new();
    private readonly Faker _faker = new("pt_BR");

    private PaymentService _sut = null!;
    private Payment _result = null!;
    private Payment _secondResult = null!;

    private Guid _orderId;
    private Guid _userId;
    private Guid _gameId;
    private string _gameTitle = string.Empty;
    private string _userEmail = string.Empty;
    private decimal _price;

    private Guid _secondOrderId;

    [BeforeScenario]
    public void Setup()
    {
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);

        _orderId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        _gameId = Guid.NewGuid();
        _gameTitle = _faker.Commerce.ProductName();
        _userEmail = _faker.Internet.Email();

        _sut = new PaymentService(_repositoryMock.Object, _approvalStrategyMock.Object, _loggerMock.Object);
    }

    [Given("um pedido válido com preço de (.*)")]
    public void GivenUmPedidoValidoComPreco(decimal preco)
    {
        _price = preco;
    }

    [Given("um pedido com OrderId específico")]
    public void GivenUmPedidoComOrderIdEspecifico()
    {
        _orderId = Guid.NewGuid();
        _price = _faker.Random.Decimal(1, 500);
    }

    [Given("um pedido com UserId específico")]
    public void GivenUmPedidoComUserIdEspecifico()
    {
        _userId = Guid.NewGuid();
        _price = _faker.Random.Decimal(1, 500);
    }

    [Given("um pedido com GameId específico")]
    public void GivenUmPedidoComGameIdEspecifico()
    {
        _gameId = Guid.NewGuid();
        _price = _faker.Random.Decimal(1, 500);
    }

    [Given("um pedido com GameTitle \"(.*)\"")]
    public void GivenUmPedidoComGameTitle(string title)
    {
        _gameTitle = title;
        _price = _faker.Random.Decimal(1, 500);
    }

    [Given("um pedido com UserEmail \"(.*)\"")]
    public void GivenUmPedidoComUserEmail(string email)
    {
        _userEmail = email;
        _price = _faker.Random.Decimal(1, 500);
    }

    [Given("dois pedidos válidos distintos")]
    public void GivenDoisPedidosValidosDistintos()
    {
        _orderId = Guid.NewGuid();
        _secondOrderId = Guid.NewGuid();
        _price = _faker.Random.Decimal(1, 500);
    }

    [When("o serviço processa o pagamento com resultado aprovado")]
    public async Task WhenOServicoProcessaOPagamentoComResultadoAprovado()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        _result = await _sut.ProcessAsync(_orderId, _userId, _gameId, _gameTitle, _userEmail, _price);
    }

    [When("o serviço processa o pagamento com resultado rejeitado")]
    public async Task WhenOServicoProcessaOPagamentoComResultadoRejeitado()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(false);
        _result = await _sut.ProcessAsync(_orderId, _userId, _gameId, _gameTitle, _userEmail, _price);
    }

    [When("o serviço processa ambos os pagamentos com resultado aprovado")]
    public async Task WhenOServicoProcessaAmbosOsPagamentos()
    {
        _approvalStrategyMock.Setup(s => s.IsApproved()).Returns(true);
        _result = await _sut.ProcessAsync(_orderId, _userId, _gameId, _gameTitle, _userEmail, _price);
        _secondResult = await _sut.ProcessAsync(_secondOrderId, _userId, _gameId, _gameTitle, _userEmail, _price);
    }

    [Then("o Payment deve ter status \"(.*)\"")]
    public void ThenOPaymentDeveTerStatus(string status)
    {
        var expectedStatus = Enum.Parse<PaymentStatus>(status);
        _result.Status.Should().Be(expectedStatus);
    }

    [Then("o Amount do Payment deve ser (.*)")]
    public void ThenOAmountDoPaymentDeveSer(decimal amount)
    {
        _result.Amount.Should().Be(amount);
    }

    [Then("o Reason do Payment deve ser nulo")]
    public void ThenOReasonDoPaymentDeveSerNulo()
    {
        _result.Reason.Should().BeNull();
    }

    [Then("o Reason do Payment deve ser \"(.*)\"")]
    public void ThenOReasonDoPaymentDeveSer(string reason)
    {
        _result.Reason.Should().Be(reason);
    }

    [Then("o Payment deve preservar o OrderId do pedido")]
    public void ThenOPaymentDevePreservarOOrderId()
    {
        _result.OrderId.Should().Be(_orderId);
    }

    [Then("o Payment deve preservar o UserId do pedido")]
    public void ThenOPaymentDevePreservarOUserId()
    {
        _result.UserId.Should().Be(_userId);
    }

    [Then("o Payment deve preservar o GameId do pedido")]
    public void ThenOPaymentDevePreservarOGameId()
    {
        _result.GameId.Should().Be(_gameId);
    }

    [Then("o Payment deve ter GameTitle \"(.*)\"")]
    public void ThenOPaymentDeveTerGameTitle(string title)
    {
        _result.GameTitle.Should().Be(title);
    }

    [Then("o Payment deve ter UserEmail \"(.*)\"")]
    public void ThenOPaymentDeveTerUserEmail(string email)
    {
        _result.UserEmail.Should().Be(email);
    }

    [Then("o Payment deve ter um Id não vazio")]
    public void ThenOPaymentDeveTerUmIdNaoVazio()
    {
        _result.Id.Should().NotBe(Guid.Empty);
    }

    [Then("o repositório deve ter persistido o Payment exatamente uma vez")]
    public void ThenORepositorioDeveTermPersistidoOPayment()
    {
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Once);
    }

    [Then("o ProcessedAt do Payment deve ser próximo do momento atual")]
    public void ThenOProcessedAtDeveSerProximoDoMomentoAtual()
    {
        _result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Then("os dois Payments devem ter Ids diferentes")]
    public void ThenOsDoisPaymentsDevemTerIdsDiferentes()
    {
        _result.Id.Should().NotBe(_secondResult.Id);
    }

    [Then("a strategy de aprovação deve ter sido consultada exatamente uma vez")]
    public void ThenAStrategyDeAprovacaoDeveTermSidoConsultada()
    {
        _approvalStrategyMock.Verify(s => s.IsApproved(), Times.Once);
    }
}
