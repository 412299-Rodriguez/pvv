using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PvvEmission.Worker;

namespace PvvEmission.Tests.Emission;

/// <summary>
/// Guards a trap that already cost a debugging session: the .NET configuration
/// binder APPENDS to a collection that already has entries instead of replacing it.
/// A non-empty default here would silently turn a configured [2,4] into
/// [60,120,180,2,4] — five retries at the wrong delays, and nothing would look
/// broken until a transient failure took twenty minutes to dead-letter.
/// </summary>
public class EmissionOptionsTests
{
    [Fact]
    public void The_configured_delays_default_to_empty_so_the_binder_has_nothing_to_append_to()
    {
        Assert.Empty(new EmissionOptions().RetryDelaysSeconds);
    }

    [Fact]
    public void The_fallback_backoff_is_one_two_and_three_minutes()
    {
        Assert.Equal([60, 120, 180], EmissionOptions.DefaultRetryDelaysSeconds);
    }

    [Fact]
    public void Binding_configuration_replaces_the_delays_rather_than_extending_them()
    {
        var options = Bind(new Dictionary<string, string?>
        {
            ["Emission:RetryDelaysSeconds:0"] = "2",
            ["Emission:RetryDelaysSeconds:1"] = "4",
        });

        Assert.Equal([2, 4], options.RetryDelaysSeconds);
    }

    [Fact]
    public void Without_configuration_the_delays_stay_empty_and_the_worker_falls_back()
    {
        var options = Bind([]);

        Assert.Empty(options.RetryDelaysSeconds);
    }

    private static EmissionOptions Bind(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.Configure<EmissionOptions>(configuration.GetSection(EmissionOptions.SectionName));

        return services.BuildServiceProvider()
            .GetRequiredService<IOptions<EmissionOptions>>().Value;
    }
}
