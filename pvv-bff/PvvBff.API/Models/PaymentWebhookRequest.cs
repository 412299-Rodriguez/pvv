namespace PvvBff.API.Models;

/// <summary>
/// Payment provider notification. In HU-08 it's sent by the mock-checkout page
/// standing in for Mercado Pago; HU-11 maps the real MP webhook payload here.
/// </summary>
public sealed record PaymentWebhookRequest(string TransactionId, string Status);
