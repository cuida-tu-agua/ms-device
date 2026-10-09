using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyWater.Devices.Api.Security;
using SyWater.Devices.Application.Devices;
using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Domain.Devices;

namespace SyWater.Devices.Api.Controllers;

/// <summary>Body of POST api/admin/devices: how many new serials of the sequence, or one specific serial.</summary>
public sealed record RegisterDevicesRequest(int? Count, string? SerialNumber);

/// <summary>
/// Admin panel of meters (ADMIN role only): list with filters, detail, factory registration, new credentials and
/// decommission. The plain token and pairing code are in the answer of the registration / new credentials ONLY.
/// </summary>
[ApiController]
[Route("api/admin/devices")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public sealed class AdminDevicesController : ControllerBase
{
    /// <param name="status">ALL, CONNECTED, DISCONNECTED, NEVER_REPORTED or DECOMMISSIONED.</param>
    /// <param name="link">ALL, LINKED or FREE.</param>
    [HttpGet]
    public async Task<ActionResult<AdminDevicePage>> List(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? link,
        [FromQuery] int? page, [FromQuery] int? size,
        [FromServices] IListAdminDevicesUseCase useCase, CancellationToken ct) =>
        Ok(await useCase.ExecuteAsync(
            AdminDeviceQuery.Normalize(search, ParseStatus(status), ParseLink(link), page, size), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminDeviceDetail>> Get(
        Guid id, [FromServices] IGetAdminDeviceUseCase useCase, CancellationToken ct) =>
        Ok(await useCase.ExecuteAsync(id, ct));

    /// <summary>Factory registration. 201 with the credentials: it is the only time they are shown.</summary>
    [HttpPost]
    public async Task<ActionResult<IReadOnlyList<FactoryDeviceView>>> Register(
        RegisterDevicesRequest request, [FromServices] IRegisterDevicesUseCase useCase, CancellationToken ct)
    {
        var created = await useCase.ExecuteAsync(User.GetAdmin(), new RegisterDevicesCommand(request.Count, request.SerialNumber), ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>New token and pairing code. 409 if somebody has the device linked or it was decommissioned.</summary>
    [HttpPost("{id:guid}/credentials")]
    public async Task<ActionResult<FactoryDeviceView>> RegenerateCredentials(
        Guid id, [FromServices] IRegenerateCredentialsUseCase useCase, CancellationToken ct) =>
        Ok(await useCase.ExecuteAsync(User.GetAdmin(), id, ct));

    /// <summary>Final. 409 if somebody has the device linked or it was already decommissioned.</summary>
    [HttpPost("{id:guid}/decommission")]
    public async Task<ActionResult<AdminDeviceDetail>> Decommission(
        Guid id, [FromServices] IDecommissionDeviceUseCase useCase, CancellationToken ct) =>
        Ok(await useCase.ExecuteAsync(User.GetAdmin(), id, ct));

    private static AdminStatusFilter ParseStatus(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        null or "" or "ALL" => AdminStatusFilter.All,
        "CONNECTED" => AdminStatusFilter.Connected,
        "DISCONNECTED" => AdminStatusFilter.Disconnected,
        "NEVER_REPORTED" => AdminStatusFilter.NeverReported,
        "DECOMMISSIONED" => AdminStatusFilter.Decommissioned,
        _ => throw new InvalidDeviceDataException("status must be ALL, CONNECTED, DISCONNECTED, NEVER_REPORTED or DECOMMISSIONED."),
    };

    private static AdminLinkFilter ParseLink(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        null or "" or "ALL" => AdminLinkFilter.All,
        "LINKED" => AdminLinkFilter.Linked,
        "FREE" => AdminLinkFilter.Free,
        _ => throw new InvalidDeviceDataException("link must be ALL, LINKED or FREE."),
    };
}
