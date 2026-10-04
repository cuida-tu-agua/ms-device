namespace SyWater.Devices.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string Section = "RabbitMq";

    public string Host { get; init; } = "";
    public int Port { get; init; } = 5672;
    public string Username { get; init; } = "sywater";
    public string Password { get; init; } = "";
    public string VirtualHost { get; init; } = "/";

    public string Exchange { get; init; } = "sywater.events";
}
