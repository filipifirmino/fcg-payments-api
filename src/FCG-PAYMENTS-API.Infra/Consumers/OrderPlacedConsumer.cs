using FCG.Events;
using FCG.Payments.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FCG.Payments.Infra.Consumers;

public class OrderPlacedConsumer(
    IPaymentService paymentService,
    ILogger<OrderPlacedConsumer> logger) : IConsumer<OrderPlacedEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var order = context.Message;

        logger.LogInformation("Received OrderPlacedEvent for OrderId {OrderId}", order.OrderId);

        var payment = await paymentService.ProcessAsync(
            order.OrderId, order.UserId, order.GameId,
            order.GameTitle, order.UserEmail, order.Price);

        await context.Publish(new PaymentProcessedEvent
        {
            OrderId = payment.OrderId,
            UserId = payment.UserId,
            GameId = payment.GameId,
            GameTitle = payment.GameTitle,
            UserEmail = payment.UserEmail,
            Amount = payment.Amount,
            Status = payment.Status,
            Reason = payment.Reason,
            ProcessedAt = payment.ProcessedAt
        });
    }
}
