using FCG.Payments.Application.Events;
using FCG.Payments.Application.Interfaces;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;
using FCG.Payments.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FCG.Payments.Application.Services;

public class PaymentService(
    IPaymentRepository repository,
    IApprovalStrategy approvalStrategy,
    ILogger<PaymentService> logger) : IPaymentService
{
    private const string RejectionReason = "Insufficient funds (simulated)";

    public async Task<Payment> ProcessAsync(
        Guid orderId, Guid userId, Guid gameId,
        string gameTitle, string userEmail, decimal amount)
    {
        var approved = approvalStrategy.IsApproved();
        var status = approved ? PaymentStatus.Approved : PaymentStatus.Rejected;

        var payment = Payment.Create(
            orderId, userId, gameId,
            gameTitle, userEmail, amount,
            status,
            approved ? null : RejectionReason);

        await repository.AddAsync(payment);

        logger.LogInformation(
            "Payment {Status} for OrderId {OrderId}, Amount: {Amount}",
            status, orderId, amount);

        return payment;
    }
}
