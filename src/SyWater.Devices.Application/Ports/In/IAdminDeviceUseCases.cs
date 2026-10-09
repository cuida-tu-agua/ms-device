using SyWater.Devices.Application.Devices;

namespace SyWater.Devices.Application.Ports.In;

public interface IListAdminDevicesUseCase
{
    Task<AdminDevicePage> ExecuteAsync(AdminDeviceQuery query, CancellationToken ct);
}

public interface IGetAdminDeviceUseCase
{
    Task<AdminDeviceDetail> ExecuteAsync(Guid id, CancellationToken ct);
}

/// <summary>Factory registration from the panel. Returns the plain credentials: it is the ONLY time they exist.</summary>
public interface IRegisterDevicesUseCase
{
    Task<IReadOnlyList<FactoryDeviceView>> ExecuteAsync(AdminActor admin, RegisterDevicesCommand command, CancellationToken ct);
}

public interface IRegenerateCredentialsUseCase
{
    Task<FactoryDeviceView> ExecuteAsync(AdminActor admin, Guid id, CancellationToken ct);
}

public interface IDecommissionDeviceUseCase
{
    Task<AdminDeviceDetail> ExecuteAsync(AdminActor admin, Guid id, CancellationToken ct);
}
