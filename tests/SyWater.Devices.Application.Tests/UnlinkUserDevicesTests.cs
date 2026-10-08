using SyWater.Devices.Application.Events;
using SyWater.Devices.Application.UseCases;

namespace SyWater.Devices.Application.Tests;

/// <summary>HU-008: deleting an account releases every device the user linked.</summary>
public class UnlinkUserDevicesTests
{
    private readonly FakeDeviceRepository _devices = new();
    private readonly FakeEventPublisher _events = new();
    private readonly FakeClock _clock = new(TestDevices.Now);
    private readonly Guid _userId = Guid.NewGuid();

    private UnlinkUserDevicesUseCase UseCase() => new(_devices, _events, _clock);

    [Fact]
    public async Task Unlinks_every_device_of_the_user_and_publishes_one_event_each()
    {
        var first = TestDevices.New("SW-ESP32-000001", TestDevices.Now, Guid.NewGuid(), _userId);
        var second = TestDevices.New("SW-ESP32-000002", TestDevices.Now, Guid.NewGuid(), _userId);
        var placeOfFirst = first.PlaceId!.Value;
        _devices.Devices.AddRange([first, second]);

        var count = await UseCase().ExecuteAsync(_userId, default);

        Assert.Equal(2, count);
        Assert.All(_devices.Devices, d => Assert.False(d.IsLinked));
        Assert.Equal(2, _devices.Unlinks.Count);
        Assert.All(_devices.Unlinks, u => Assert.Equal(_userId, u.UnlinkedBy));
        var events = _events.Published.OfType<DeviceUnlinked>().ToList();
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.DeviceId == first.Id && e.PlaceId == placeOfFirst && e.UserId == _userId);
    }

    [Fact]
    public async Task Devices_of_other_users_and_free_devices_are_left_alone()
    {
        var someoneElses = TestDevices.New("SW-ESP32-000001", TestDevices.Now, Guid.NewGuid(), Guid.NewGuid());
        var free = TestDevices.New("SW-ESP32-000002", TestDevices.Now);
        _devices.Devices.AddRange([someoneElses, free]);

        var count = await UseCase().ExecuteAsync(_userId, default);

        Assert.Equal(0, count);
        Assert.True(someoneElses.IsLinked);
        Assert.Empty(_devices.Unlinks);
        Assert.Empty(_events.Published);
    }

    [Fact]
    public async Task Calling_it_twice_is_harmless()
    {
        _devices.Devices.Add(TestDevices.New(lastReportAt: TestDevices.Now, placeId: Guid.NewGuid(), linkedBy: _userId));

        Assert.Equal(1, await UseCase().ExecuteAsync(_userId, default));
        Assert.Equal(0, await UseCase().ExecuteAsync(_userId, default));
        Assert.Single(_events.Published);
    }
}
