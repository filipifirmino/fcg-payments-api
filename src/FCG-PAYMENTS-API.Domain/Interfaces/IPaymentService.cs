using FCG.Payments.Domain.Entities;

namespace FCG.Payments.Domain.Interfaces;

public interface IPaymentService
{
    Task<Payment> ProcessAsync(Guid orderId, Guid userId, Guid gameId, string gameTitle, string userEmail, decimal amount);
}
