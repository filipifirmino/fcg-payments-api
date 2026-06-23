using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FCG.Payments.Infra.Repositories;

public class PaymentRepository : RepositoryBase<Payment>, IPaymentRepository
{
    private readonly AppDbContext _context;

    public PaymentRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId)
        => await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
}
