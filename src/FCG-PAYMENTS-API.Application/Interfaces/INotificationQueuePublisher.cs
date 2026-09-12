namespace FCG.Payments.Application.Interfaces;

/// <summary>Dual-publish: além do RabbitMQ, envia o mesmo evento para SQS, que aciona a Lambda de notificações.</summary>
public interface INotificationQueuePublisher
{
    Task PublishPaymentProcessedAsync(
        Guid orderId, Guid userId, Guid gameId, string gameTitle, string userEmail,
        decimal amount, string status, string? reason, DateTime processedAt);
}
