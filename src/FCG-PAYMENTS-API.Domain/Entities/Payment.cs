using FCG.Payments.Domain.Enums;

namespace FCG.Payments.Domain.Entities;

public class Payment
{
    private Payment() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid GameId { get; private set; }
    public string GameTitle { get; private set; } = string.Empty;
    public string UserEmail { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    public static Payment Create(
        Guid orderId,
        Guid userId,
        Guid gameId,
        string gameTitle,
        string userEmail,
        decimal amount,
        PaymentStatus status,
        string? reason = null)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            UserId = userId,
            GameId = gameId,
            GameTitle = gameTitle,
            UserEmail = userEmail,
            Amount = amount,
            Status = status,
            Reason = reason,
            ProcessedAt = DateTime.UtcNow
        };
    }
}
