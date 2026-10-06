using MQTTnet;
using MQTTnet.Protocol;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Application.Telemetry;
using SyWater.Devices.Application.Valve;

namespace SyWater.Devices.Api.Messaging;

public sealed class MqttValveCommandSender(MqttConnectionState state) : IValveCommandSender
{
    public async Task SendAsync(string serialNumber, Guid commandId, ValveAction action, CancellationToken ct)
    {
        var client = state.Client ?? throw new MessagingUnavailableException();

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(DeviceTopics.ValveSetFor(serialNumber))
            .WithPayload(ValveCommandPayload.Build(commandId, action))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        try
        {
            var result = await client.PublishAsync(message, ct);
            if (!result.IsSuccess)
                throw new MessagingUnavailableException(new InvalidOperationException($"Broker answered {result.ReasonCode}"));
        }
        catch (Exception ex) when (ex is not MessagingUnavailableException and not OperationCanceledException)
        {
            throw new MessagingUnavailableException(ex);
        }
    }
}
