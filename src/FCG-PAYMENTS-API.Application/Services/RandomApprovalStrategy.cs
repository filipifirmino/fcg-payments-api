using FCG.Payments.Application.Interfaces;

namespace FCG.Payments.Application.Services;

public class RandomApprovalStrategy : IApprovalStrategy
{
    private const double ApprovalThreshold = 0.1;

    public bool IsApproved() => Random.Shared.NextDouble() > ApprovalThreshold;
}
