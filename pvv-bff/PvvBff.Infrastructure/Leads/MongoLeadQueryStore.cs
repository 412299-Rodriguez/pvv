using MongoDB.Bson;
using MongoDB.Driver;
using PvvBff.Application.Abstractions;
using PvvBff.Application.Leads.Queries;
using PvvBff.Domain.Leads;

namespace PvvBff.Infrastructure.Leads;

/// <summary>
/// Reads the funnel out of MongoDB. The dashboard needs two very different
/// shapes, so each gets the tool that fits: an aggregation for the funnel counts
/// and a plain paged find for the table.
/// </summary>
public sealed class MongoLeadQueryStore : ILeadQueryStore
{
    /// <summary>The five milestones, in the order the wizard reaches them.</summary>
    private static readonly string[] StepLabels =
        ["Vehículo", "Tomador", "Cotización", "Pago", "Emisión"];

    private readonly IMongoCollection<Lead> _collection;

    public MongoLeadQueryStore(IMongoDatabase database)
    {
        _collection = database.GetCollection<Lead>(MongoLeadStore.CollectionName);
    }

    public async Task<LeadFunnelDto> GetFunnelAsync(LeadQueryFilter filter, CancellationToken ct)
    {
        // One round trip for both breakdowns: how far each lead got, and how many
        // are still open vs abandoned vs bought.
        PipelineDefinition<Lead, BsonDocument> pipeline = new[]
        {
            new BsonDocument("$match", BuildMatch(filter)),
            new BsonDocument("$facet", new BsonDocument
            {
                { "byStep", GroupBy("$" + nameof(Lead.LastStep)) },
                { "byStatus", GroupBy("$" + nameof(Lead.Status)) },
                // Where each journey ended, and how it ended: the leads table is
                // grouped by exactly this.
                { "byStepStatus", new BsonArray
                    {
                        new BsonDocument("$group", new BsonDocument
                        {
                            { "_id", new BsonDocument
                                {
                                    { "step", "$" + nameof(Lead.LastStep) },
                                    { "status", "$" + nameof(Lead.Status) },
                                }
                            },
                            { "count", new BsonDocument("$sum", 1) },
                        }),
                    }
                },
            }),
        };

        var result = await (await _collection.AggregateAsync(pipeline, cancellationToken: ct))
            .FirstOrDefaultAsync(ct);

        var stepCounts = ReadCounts(result, "byStep");
        var statusCounts = ReadCounts(result, "byStatus");

        var totalLeads = stepCounts.Values.Sum();

        // A lead "reached" step N if it got at least that far, so each rung is the
        // tail of the distribution from N upwards.
        var steps = new List<LeadFunnelStepDto>(StepLabels.Length);
        long previous = totalLeads;
        for (var step = 1; step <= StepLabels.Length; step++)
        {
            var reached = stepCounts
                .Where(entry => int.TryParse(entry.Key, out var last) && last >= step)
                .Sum(entry => entry.Value);

            steps.Add(new LeadFunnelStepDto(
                step,
                StepLabels[step - 1],
                reached,
                Percentage(reached, previous)));

            previous = reached;
        }

        var completed = Count(statusCounts, LeadStatus.Completed);
        var policiesIssued = steps[^1].Reached;

        return new LeadFunnelDto(
            TotalLeads: totalLeads,
            Active: Count(statusCounts, LeadStatus.Active),
            Abandoned: Count(statusCounts, LeadStatus.Abandoned),
            Completed: completed,
            PoliciesIssued: policiesIssued,
            OverallConversion: Percentage(completed, totalLeads),
            Steps: steps,
            StoppedAt: ReadStepBreakdown(result));
    }

    public async Task<PagedLeadsDto> GetLeadsAsync(
        LeadQueryFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var match = new BsonDocumentFilterDefinition<Lead>(BuildMatch(filter));

        var total = await _collection.CountDocumentsAsync(match, cancellationToken: ct);

        var leads = await _collection
            .Find(match)
            .SortByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return new PagedLeadsDto(leads.Select(ToListItem).ToList(), total, page, pageSize);
    }

    private static LeadListItemDto ToListItem(Lead lead)
    {
        var holder = lead.Steps.Step2;
        var fullName = string.Join(' ', new[] { holder.FirstName, holder.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        return new LeadListItemDto(
            FlowId: lead.Id,
            CreatedAt: lead.CreatedAt,
            UpdatedAt: lead.UpdatedAt,
            LastStep: lead.LastStep,
            Status: lead.Status,
            Plate: lead.Steps.Step1.Plate,
            VehicleTitle: lead.Steps.Step1.VehicleTitle,
            HolderName: string.IsNullOrWhiteSpace(fullName) ? null : fullName,
            Dni: holder.Dni,
            Email: lead.ContactEmail ?? holder.Email,
            Phone: lead.ContactPhone ?? holder.Phone,
            ProductName: lead.Steps.Step3.ProductName,
            Amount: lead.Steps.Step3.Amount,
            PaymentStatus: lead.Steps.Step4.Status,
            PolicyNumber: lead.Steps.Step5.PolicyNumber);
    }

    private static BsonDocument BuildMatch(LeadQueryFilter filter)
    {
        var match = new BsonDocument();

        if (!string.IsNullOrWhiteSpace(filter.CompanyToken))
            match[nameof(Lead.CompanyToken)] = filter.CompanyToken;

        if (filter.From.HasValue || filter.To.HasValue)
        {
            var range = new BsonDocument();
            if (filter.From.HasValue)
                range["$gte"] = filter.From.Value;
            if (filter.To.HasValue)
                range["$lte"] = filter.To.Value;
            match[nameof(Lead.CreatedAt)] = range;
        }

        if (filter.LastStep.HasValue)
            match[nameof(Lead.LastStep)] = filter.LastStep.Value;

        if (!string.IsNullOrWhiteSpace(filter.Status))
            match[nameof(Lead.Status)] = filter.Status;

        return match;
    }

    private static BsonArray GroupBy(string field) =>
        [new BsonDocument("$group", new BsonDocument
        {
            { "_id", field },
            { "count", new BsonDocument("$sum", 1) },
        })];

    private static Dictionary<string, long> ReadCounts(BsonDocument? result, string facetName)
    {
        var counts = new Dictionary<string, long>(StringComparer.Ordinal);
        if (result is null || !result.TryGetValue(facetName, out var facet) || !facet.IsBsonArray)
            return counts;

        foreach (var entry in facet.AsBsonArray.OfType<BsonDocument>())
        {
            var key = entry.GetValue("_id", BsonNull.Value);
            counts[key.IsBsonNull ? string.Empty : key.ToString()!] =
                entry.GetValue("count", 0).ToInt64();
        }

        return counts;
    }

    /// <summary>Pivots the (step, status) pairs into one row per milestone.</summary>
    private static List<LeadStepBreakdownDto> ReadStepBreakdown(BsonDocument? result)
    {
        var byStep = new Dictionary<int, Dictionary<string, long>>();

        if (result is not null
            && result.TryGetValue("byStepStatus", out var facet)
            && facet.IsBsonArray)
        {
            foreach (var entry in facet.AsBsonArray.OfType<BsonDocument>())
            {
                if (entry.GetValue("_id", BsonNull.Value) is not BsonDocument key) continue;

                var step = key.GetValue("step", 0).ToInt32();
                var status = key.GetValue("status", BsonNull.Value);

                if (!byStep.TryGetValue(step, out var statuses))
                {
                    statuses = new Dictionary<string, long>(StringComparer.Ordinal);
                    byStep[step] = statuses;
                }

                statuses[status.IsBsonNull ? string.Empty : status.ToString()!] =
                    entry.GetValue("count", 0).ToInt64();
            }
        }

        return byStep
            .OrderBy(pair => pair.Key)
            .Select(pair => new LeadStepBreakdownDto(
                Step: pair.Key,
                Active: Count(pair.Value, LeadStatus.Active),
                Abandoned: Count(pair.Value, LeadStatus.Abandoned),
                Completed: Count(pair.Value, LeadStatus.Completed),
                Total: pair.Value.Values.Sum()))
            .ToList();
    }

    private static long Count(Dictionary<string, long> counts, string key) =>
        counts.TryGetValue(key, out var value) ? value : 0;

    private static decimal Percentage(long part, long whole) =>
        whole <= 0 ? 0 : Math.Round(part * 100m / whole, 1);
}
