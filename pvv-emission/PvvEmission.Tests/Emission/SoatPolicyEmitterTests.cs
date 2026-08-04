using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using PvvEmission.Application.Emission;
using PvvEmission.Infrastructure.Emission;

namespace PvvEmission.Tests.Emission;

/// <summary>
/// This mapping is the retry policy. Classifying a failure wrong is expensive in
/// both directions: a permanent error read as transient burns three retries before
/// dead-lettering, and a transient one read as permanent throws away a paid sale
/// because soat happened to be restarting.
/// </summary>
public class SoatPolicyEmitterTests
{
    private const string BudgetId = "e6f1c0b2-0000-4000-8000-000000000001";

    [Fact]
    public async Task A_successful_emission_returns_the_policy_number()
    {
        var emitter = EmitterReturning(
            HttpStatusCode.OK, """{"policyNumber":"PVV-2026-000012"}""");

        var outcome = await emitter.EmitAsync(BudgetId, CancellationToken.None);

        Assert.Equal(EmitResult.Success, outcome.Result);
        Assert.Equal("PVV-2026-000012", outcome.PolicyNumber);
    }

    [Fact]
    public async Task A_success_without_a_readable_body_still_counts_as_emitted()
    {
        // Acknowledging the message matters more than the number: soat has already
        // issued the policy, so redelivering would only ask it to do so again.
        var emitter = EmitterReturning(HttpStatusCode.OK, "null");

        var outcome = await emitter.EmitAsync(BudgetId, CancellationToken.None);

        Assert.Equal(EmitResult.Success, outcome.Result);
        Assert.Equal("unknown", outcome.PolicyNumber);
    }

    [Fact]
    public async Task Conflict_means_the_policy_already_exists_and_is_treated_as_success()
    {
        // soat answers 409 when the budget was already emitted — that is the
        // idempotent path, not a failure.
        var emitter = EmitterReturning(HttpStatusCode.Conflict, "");

        var outcome = await emitter.EmitAsync(BudgetId, CancellationToken.None);

        Assert.Equal(EmitResult.AlreadyEmitted, outcome.Result);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task A_bad_or_missing_budget_is_never_retried(HttpStatusCode status)
    {
        // Retrying cannot fix data that is wrong: straight to the dead-letter queue.
        var emitter = EmitterReturning(status, "");

        var outcome = await emitter.EmitAsync(BudgetId, CancellationToken.None);

        Assert.Equal(EmitResult.InvalidData, outcome.Result);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task A_server_side_failure_is_retried(HttpStatusCode status)
    {
        var emitter = EmitterReturning(status, "");

        var outcome = await emitter.EmitAsync(BudgetId, CancellationToken.None);

        Assert.Equal(EmitResult.Transient, outcome.Result);
    }

    [Fact]
    public async Task An_unreachable_soat_is_retried_rather_than_dead_lettered()
    {
        // soat being down is the most ordinary failure there is, and the one where
        // dead-lettering would lose a sale that only needed to wait.
        var emitter = EmitterThrowing(new HttpRequestException("connection refused"));

        var outcome = await emitter.EmitAsync(BudgetId, CancellationToken.None);

        Assert.Equal(EmitResult.Transient, outcome.Result);
        Assert.Contains("connection refused", outcome.Error);
    }

    [Fact]
    public async Task A_timeout_is_retried()
    {
        var emitter = EmitterThrowing(new TaskCanceledException("timed out"));

        var outcome = await emitter.EmitAsync(BudgetId, CancellationToken.None);

        Assert.Equal(EmitResult.Transient, outcome.Result);
    }

    [Fact]
    public async Task A_real_shutdown_cancels_instead_of_being_swallowed_as_transient()
    {
        // The catch filter excludes a cancellation the caller asked for; otherwise
        // stopping the worker would look like soat timing out and be requeued.
        var emitter = EmitterThrowing(new TaskCanceledException("stopping"));
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => emitter.EmitAsync(BudgetId, cancelled.Token));
    }

    [Fact]
    public async Task The_budget_id_is_posted_to_the_emit_endpoint()
    {
        var handler = new StubHandler(_ => Respond(HttpStatusCode.OK, """{"policyNumber":"PVV-2026-000001"}"""));
        var emitter = EmitterFor(handler);

        await emitter.EmitAsync(BudgetId, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/policies/emit", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains(BudgetId, handler.LastBody);
    }

    // ---- Helpers ---------------------------------------------------------------

    private static SoatPolicyEmitter EmitterReturning(HttpStatusCode status, string body) =>
        EmitterFor(new StubHandler(_ => Respond(status, body)));

    private static SoatPolicyEmitter EmitterThrowing(Exception exception) =>
        EmitterFor(new StubHandler(_ => throw exception));

    private static SoatPolicyEmitter EmitterFor(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://soat.test") },
            NullLogger<SoatPolicyEmitter>.Instance);

    private static HttpResponseMessage Respond(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            ct.ThrowIfCancellationRequested();
            return respond(request);
        }
    }
}
