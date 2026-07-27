namespace PvvBff.API.Models;

/// <summary>
/// Mercado Pago's webhook body: <c>{ "type": "payment", "action": "payment.updated",
/// "data": { "id": "170770768958" } }</c>.
///
/// Note what is NOT here: no amount, no status, and no reference to our transaction. The
/// notification is a doorbell, not a letter — everything that matters is read back from
/// their API afterwards, which is precisely why none of this has to be trusted.
/// </summary>
public sealed record MercadoPagoNotification(
    string? Type,
    string? Action,
    MercadoPagoNotificationData? Data);

public sealed record MercadoPagoNotificationData(string? Id);
