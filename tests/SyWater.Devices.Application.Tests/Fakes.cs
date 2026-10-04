using SyWater.Devices.Application.Events;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Application.Valve;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.Tests;

/// <summary>In-memory adapter: same rules as the SQL indexes (one device per place).</summary>
internal sealed class FakeDeviceRepository : IDeviceRepository
{
    public List<Device> Devices { get; } = [];
    public List<DeviceLink> Links { get; } = [];
    public List<(Guid DeviceId, Guid UnlinkedBy, DateTime At)> Unlinks { get; } = [];
    public int Updates { get; private set; }

    public Task<Device?> GetBySerialAsync(string serialNumber, CancellationToken ct) =>
        Task.FromResult(Devices.FirstOrDefault(d => d.SerialNumber == serialNumber));

    public Task<Device?> GetByPlaceAsync(Guid placeId, CancellationToken ct) =>
        Task.FromResult(Devices.FirstOrDefault(d => d.PlaceId == placeId));

    public Task SaveHeartbeatAsync(Device device, CancellationToken ct)
    {
        Updates++;
        return Task.CompletedTask;
    }

    public Task SaveFailedPairingAsync(Device device, PairingPolicy policy, DateTime now, CancellationToken ct)
    {
        Updates++;
        return Task.CompletedTask;
    }

    public Task LinkAsync(Device device, DeviceLink link, CancellationToken ct)
    {
        if (Devices.Count(d => d.PlaceId == device.PlaceId) > 1)
            throw new PlaceAlreadyHasDeviceException(device.PlaceId!.Value); // UX_devices_place
        Links.Add(link);
        return Task.CompletedTask;
    }

    public Task UnlinkAsync(Device device, Guid placeId, Guid unlinkedBy, DateTime unlinkedAt, CancellationToken ct)
    {
        Unlinks.Add((device.Id, unlinkedBy, unlinkedAt));
        return Task.CompletedTask;
    }

    public Task<int> MarkStaleAsDisconnectedAsync(DateTime now, CancellationToken ct) =>
        Task.FromResult(Devices.Count(d => d.Status == DeviceStatus.Connected && d.StatusAt(now) == DeviceStatus.Disconnected));
}

internal sealed class FakePlaceOwnershipChecker : IPlaceOwnershipChecker
{
    public HashSet<Guid> OwnedPlaces { get; } = [];
    public bool IsDown { get; set; }

    public Task<bool> IsOwnedByRequesterAsync(Guid placeId, CancellationToken ct) =>
        IsDown
            ? throw new ExternalServiceUnavailableException("place-service")
            : Task.FromResult(OwnedPlaces.Contains(placeId));
}

internal sealed class FakeClock(DateTime utcNow) : TimeProvider
{
    public DateTime UtcNow { get; set; } = utcNow;
    public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
}

internal sealed class FakeEventPublisher : IEventPublisher
{
    public List<IIntegrationEvent> Published { get; } = [];

    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken ct)
    {
        Published.Add(integrationEvent);
        return Task.CompletedTask;
    }
}

internal sealed class FakeValveCommandSender : IValveCommandSender
{
    public List<(string Serial, Guid CommandId, ValveAction Action)> Sent { get; } = [];
    public bool BrokerDown { get; set; }

    public Task SendAsync(string serialNumber, Guid commandId, ValveAction action, CancellationToken ct)
    {
        if (BrokerDown) throw new MessagingUnavailableException();
        Sent.Add((serialNumber, commandId, action));
        return Task.CompletedTask;
    }
}
