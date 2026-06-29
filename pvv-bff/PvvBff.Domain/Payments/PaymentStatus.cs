namespace PvvBff.Domain.Payments;

/// <summary>Lifecycle of a payment transaction.</summary>
public enum PaymentStatus
{
    Pending,
    Confirmed,
    Failed,
    Abandoned,
}
