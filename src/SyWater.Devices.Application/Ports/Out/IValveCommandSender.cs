using SyWater.Devices.Application.Valve;

namespace SyWater.Devices.Application.Ports.Out;

public interface IValveCommandSender
{
    /// <exception cref="MessagingUnavailableException">The MQTT broker is not reachable.</exception>
    Task SendAsync(string serialNumber, Guid commandId, ValveAction action, CancellationToken ct);
}

public sealed class MessagingUnavailableException(Exception? inner = null)
    : Exception("The MQTT broker is not reachable right now.", inner);
