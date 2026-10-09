using SyWater.Devices.Application.Devices;
using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Application.Ports.Out;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Application.UseCases;

public sealed class ListAdminDevicesUseCase(IAdminDeviceRepository devices, TimeProvider clock) : IListAdminDevicesUseCase
{
    public Task<AdminDevicePage> ExecuteAsync(AdminDeviceQuery query, CancellationToken ct) =>
        devices.ListAsync(query, clock.GetUtcNow().UtcDateTime, ct);
}

public sealed class GetAdminDeviceUseCase(IAdminDeviceRepository devices, TimeProvider clock) : IGetAdminDeviceUseCase
{
    public async Task<AdminDeviceDetail> ExecuteAsync(Guid id, CancellationToken ct) =>
        await devices.GetDetailAsync(id, clock.GetUtcNow().UtcDateTime, ct) ?? throw new DeviceNotFoundException(id);
}

public sealed class RegisterDevicesUseCase(IAdminDeviceRepository devices, TimeProvider clock) : IRegisterDevicesUseCase
{
    public async Task<IReadOnlyList<FactoryDeviceView>> ExecuteAsync(AdminActor admin, RegisterDevicesCommand command, CancellationToken ct)
    {
        IReadOnlyList<string> serials;
        if (!string.IsNullOrWhiteSpace(command.SerialNumber))
        {
            serials = [DeviceSecrets.NormalizeSerial(command.SerialNumber)];
        }
        else
        {
            var last = await devices.GetLastFactorySerialAsync(ct);
            serials = FactorySerials.Next(last, command.Count ?? 1);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var created = serials.Select(serial =>
        {
            var credentials = FactoryCredentials.Generate();
            return (Device: Device.Register(serial, credentials, now), Credentials: credentials);
        }).ToList();

        await devices.AddAsync(created.Select(c => c.Device).ToList(), new AdminLogEntry(AdminAction.Registered, admin.UserId, admin.Name, now), ct);

        return created.Select(c => new FactoryDeviceView(c.Device.Id, c.Device.SerialNumber, c.Credentials.PairingCodeLabel, c.Credentials.AuthToken)).ToList();
    }
}

public sealed class RegenerateCredentialsUseCase(IAdminDeviceRepository devices, TimeProvider clock) : IRegenerateCredentialsUseCase
{
    public async Task<FactoryDeviceView> ExecuteAsync(AdminActor admin, Guid id, CancellationToken ct)
    {
        var device = await devices.GetByIdAsync(id, ct) ?? throw new DeviceNotFoundException(id);
        var now = clock.GetUtcNow().UtcDateTime;
        var credentials = FactoryCredentials.Generate();

        device.RegenerateCredentials(credentials, now);   // throws if it is linked or decommissioned
        await devices.SaveCredentialsAsync(device, new AdminLogEntry(AdminAction.CredentialsRegenerated, admin.UserId, admin.Name, now), ct);

        return new FactoryDeviceView(device.Id, device.SerialNumber, credentials.PairingCodeLabel, credentials.AuthToken);
    }
}

public sealed class DecommissionDeviceUseCase(IAdminDeviceRepository devices, TimeProvider clock) : IDecommissionDeviceUseCase
{
    public async Task<AdminDeviceDetail> ExecuteAsync(AdminActor admin, Guid id, CancellationToken ct)
    {
        var device = await devices.GetByIdAsync(id, ct) ?? throw new DeviceNotFoundException(id);
        var now = clock.GetUtcNow().UtcDateTime;

        device.Decommission(now);   // throws if it is linked or already decommissioned
        await devices.SaveDecommissionAsync(device, new AdminLogEntry(AdminAction.Decommissioned, admin.UserId, admin.Name, now), ct);

        return await devices.GetDetailAsync(id, now, ct) ?? throw new DeviceNotFoundException(id);
    }
}
