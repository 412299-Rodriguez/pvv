using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PvvBff.Application.Abstractions;
using PvvBff.Domain.Leads;

namespace PvvBff.Application.Leads;

/// <summary>Turns a wizard event into a lead update. See <see cref="LeadProjectionService"/>.</summary>
public interface ILeadProjectionService
{
    Task ProjectAsync(LeadEvent leadEvent, CancellationToken ct);
}

/// <summary>
/// The single place where an event becomes funnel state: it appends the raw event
/// to the log and applies the matching projection to the lead. Every producer —
/// the LEAD_EVENT ingress handler, the payment handlers, the emission status
/// handler and the abandonment job — goes through here, so the mapping from event
/// to funnel step exists exactly once.
/// </summary>
public sealed class LeadProjectionService : ILeadProjectionService
{
    private const string StatusStarted = "started";
    private const string StatusCompleted = "completed";
    private const string StatusQuoted = "quoted";
    private const string StatusInitiated = "initiated";
    private const string StatusRejected = "rejected";
    private const string StatusAbandoned = "abandoned";
    private const string StatusFailed = "failed";

    private readonly ILeadStore _leads;
    private readonly IEventLogStore _eventLogs;
    private readonly ILogger<LeadProjectionService> _logger;

    public LeadProjectionService(
        ILeadStore leads,
        IEventLogStore eventLogs,
        ILogger<LeadProjectionService> logger)
    {
        _leads = leads;
        _eventLogs = eventLogs;
        _logger = logger;
    }

    public async Task ProjectAsync(LeadEvent leadEvent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(leadEvent.FlowId))
        {
            // Without a flow we cannot attribute the event to a purchase attempt.
            _logger.LogWarning("Lead event '{Event}' arrived without a flow id; ignored", leadEvent.Name);
            return;
        }

        await _eventLogs.AppendAsync(
            new EventLogEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = leadEvent.Name,
                FlowId = leadEvent.FlowId,
                CompanyToken = leadEvent.CompanyToken,
                SessionId = leadEvent.SessionId,
                Payload = SerializePayload(leadEvent.Payload),
                OccurredAt = leadEvent.OccurredAt,
            },
            ct);

        var projection = BuildProjection(leadEvent);
        await _leads.ApplyAsync(projection, ct);

        _logger.LogInformation(
            "Lead {FlowId}: applied '{Event}'", leadEvent.FlowId, leadEvent.Name);
    }

    /// <summary>Maps an event name to the fields and milestone it writes.</summary>
    private static LeadProjection BuildProjection(LeadEvent e)
    {
        var fields = new Dictionary<string, object?>(StringComparer.Ordinal);
        int? minLastStep = null;
        string? status = null;
        var onlyIfNotCompleted = false;

        switch (e.Name.ToLowerInvariant())
        {
            // ---- Step 0: the visitor opened the portal ---------------------------
            case LeadEventNames.SessionStart:
                break;

            // ---- Step 1: which vehicle -------------------------------------------
            case LeadEventNames.PlateEntered:
                fields["Steps.Step1.Status"] = StatusStarted;
                fields["Steps.Step1.Plate"] = Text(e, "plate");
                break;

            case LeadEventNames.PlateValidated:
                fields["Steps.Step1.Status"] = StatusCompleted;
                fields["Steps.Step1.Plate"] = Text(e, "plate");
                fields["Steps.Step1.VehicleTitle"] = Text(e, "vehicleTitle");
                minLastStep = 1;
                break;

            // ---- Step 2: who the policyholder is ---------------------------------
            case LeadEventNames.DocumentEntered:
                fields["Steps.Step2.Status"] = StatusStarted;
                fields["Steps.Step2.Dni"] = Text(e, "dni");
                break;

            case LeadEventNames.HolderCompleted:
                fields["Steps.Step2.Status"] = StatusCompleted;
                fields["Steps.Step2.FirstName"] = Text(e, "firstName");
                fields["Steps.Step2.LastName"] = Text(e, "lastName");
                fields["Steps.Step2.Dni"] = Text(e, "dni");
                fields["Steps.Step2.Email"] = Text(e, "email");
                fields["Steps.Step2.Phone"] = Text(e, "phone");
                // Lifted to the root so abandoned-cart recovery can filter on it.
                fields["ContactEmail"] = Text(e, "email");
                fields["ContactPhone"] = Text(e, "phone");
                minLastStep = 2;
                break;

            // ---- Step 3: product and price ---------------------------------------
            case LeadEventNames.BudgetCalculated:
                fields["Steps.Step3.Status"] = StatusQuoted;
                fields["Steps.Step3.OptionsCount"] = Integer(e, "optionsCount");
                break;

            case LeadEventNames.ProductSelected:
                fields["Steps.Step3.Status"] = StatusCompleted;
                fields["Steps.Step3.ProductId"] = Text(e, "productId");
                fields["Steps.Step3.ProductName"] = Text(e, "productName");
                fields["Steps.Step3.Amount"] = Number(e, "amount");
                minLastStep = 3;
                break;

            // ---- Step 4: the payment ---------------------------------------------
            case LeadEventNames.PaymentInitiated:
                fields["Steps.Step4.Status"] = StatusInitiated;
                fields["Steps.Step4.PaymentMethod"] = Text(e, "paymentMethod");
                fields["Steps.Step4.TransactionId"] = Text(e, "transactionId");
                fields["Steps.Step4.PreferenceId"] = Text(e, "preferenceId");
                minLastStep = 4;
                break;

            case LeadEventNames.PaymentConfirmed:
                fields["Steps.Step4.Status"] = StatusCompleted;
                minLastStep = 4;
                break;

            case LeadEventNames.PaymentRejected:
                // The lead stays active: the visitor can still retry the payment.
                fields["Steps.Step4.Status"] = StatusRejected;
                break;

            case LeadEventNames.PaymentAbandoned:
                fields["Steps.Step4.Status"] = StatusAbandoned;
                fields["AbandonedAt"] = e.OccurredAt;
                status = LeadStatus.Abandoned;
                onlyIfNotCompleted = true;
                break;

            // ---- Step 5: the policy ----------------------------------------------
            case LeadEventNames.PolicyIssued:
                fields["Steps.Step5.Status"] = StatusCompleted;
                fields["Steps.Step5.PolicyNumber"] = Text(e, "policyNumber");
                minLastStep = 5;
                status = LeadStatus.Completed;
                break;

            case LeadEventNames.EmissionFailed:
                fields["Steps.Step5.Status"] = StatusFailed;
                break;

            // ---- Not a funnel step, but worth keeping on the lead -----------------
            case LeadEventNames.WizardError:
                fields["LastError"] = Text(e, "message");
                break;
        }

        return new LeadProjection(
            e.FlowId,
            e.CompanyToken,
            e.SessionId,
            fields,
            minLastStep,
            status,
            onlyIfNotCompleted,
            // An event that does not dictate a status is the visitor still
            // working, which contradicts an earlier abandonment.
            RevivesLead: status is null);
    }

    private static string? Text(LeadEvent e, string key) =>
        e.Payload is not null && e.Payload.TryGetValue(key, out var value) && value is not null
            ? Convert.ToString(value, CultureInfo.InvariantCulture)
            : null;

    private static decimal? Number(LeadEvent e, string key)
    {
        if (e.Payload is null || !e.Payload.TryGetValue(key, out var value) || value is null)
            return null;

        return value switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            double db => (decimal)db,
            _ => decimal.TryParse(
                Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var parsed)
                ? parsed
                : null,
        };
    }

    /// <summary>Counts are stored as Int32 — the Lead model reads them back as int.</summary>
    private static int? Integer(LeadEvent e, string key) =>
        Number(e, key) is decimal value ? (int)value : null;

    private static string? SerializePayload(IReadOnlyDictionary<string, object?>? payload) =>
        payload is null || payload.Count == 0 ? null : JsonSerializer.Serialize(payload);
}
