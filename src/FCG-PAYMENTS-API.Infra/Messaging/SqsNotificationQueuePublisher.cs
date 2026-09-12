using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using FCG.Payments.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FCG.Payments.Infra.Messaging;

public class SqsNotificationQueuePublisher : INotificationQueuePublisher
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<SqsNotificationQueuePublisher> _logger;
    private readonly string? _paymentProcessedQueueUrl;

    public SqsNotificationQueuePublisher(
        IAmazonSQS sqsClient,
        IConfiguration configuration,
        ILogger<SqsNotificationQueuePublisher> logger)
    {
        _sqsClient = sqsClient;
        _logger = logger;
        _paymentProcessedQueueUrl = configuration["Aws:Sqs:PaymentProcessedQueueUrl"];
    }

    public async Task PublishPaymentProcessedAsync(
        Guid orderId, Guid userId, Guid gameId, string gameTitle, string userEmail,
        decimal amount, string status, string? reason, DateTime processedAt)
    {
        if (string.IsNullOrWhiteSpace(_paymentProcessedQueueUrl))
        {
            _logger.LogWarning(
                "Aws:Sqs:PaymentProcessedQueueUrl não configurado — pulando dual-publish para a fila da Lambda de notificações.");
            return;
        }

        // "status" chega como string, não como o enum PaymentStatus — a Lambda não
        // deve depender de um binário .NET compartilhado entre repositórios.
        var payload = JsonSerializer.Serialize(new
        {
            orderId,
            userId,
            gameId,
            gameTitle,
            userEmail,
            amount,
            status,
            reason,
            processedAt
        });

        try
        {
            await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _paymentProcessedQueueUrl,
                MessageBody = payload
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao publicar PaymentProcessed na fila SQS para o pedido {OrderId}", orderId);
        }
    }
}
