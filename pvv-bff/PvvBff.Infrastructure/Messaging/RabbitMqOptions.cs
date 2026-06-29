namespace PvvBff.Infrastructure.Messaging;

/// <summary>RabbitMQ connection + queue settings, bound from "RabbitMQ".</summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VHost { get; set; } = "pvv";
    public string User { get; set; } = "guest";
    public string Pass { get; set; } = "guest";

    public string EmissionQueue { get; set; } = "pvv_emission_queue";
    public string EmissionDlq { get; set; } = "pvv_emission_dlq";
}
