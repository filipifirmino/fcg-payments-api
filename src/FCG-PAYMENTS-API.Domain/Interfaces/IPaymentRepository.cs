using FCG.Payments.Domain.Entities;

namespace FCG.Payments.Domain.Interfaces;

public interface IPaymentRepository : IRepositoryBase<Payment>
{
    Task<Payment?> GetByOrderIdAsync(Guid orderId);
}
