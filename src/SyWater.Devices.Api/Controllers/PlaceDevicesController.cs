using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SyWater.Devices.Api.Contracts;
using SyWater.Devices.Api.Security;
using SyWater.Devices.Application.Devices;
using SyWater.Devices.Application.Ports.In;

namespace SyWater.Devices.Api.Controllers;

/// <summary>
/// Inbound adapter: the device of a place. A place has at most one device, so the resource is
/// singular: /api/places/{placeId}/device. No business logic here.
/// Every endpoint requires a valid token (fallback policy in Program.cs).
/// </summary>
[ApiController]
[Route("api/places/{placeId:guid}/device")]
public sealed class PlaceDevicesController : ControllerBase
{
    /// <summary>HU-013: device of the place with its status. 404 device.not_linked when there is none.</summary>
    [HttpGet]
    public async Task<ActionResult<DeviceView>> Get(
        Guid placeId, [FromServices] IGetPlaceDeviceUseCase useCase, CancellationToken ct) =>
        Ok(await useCase.ExecuteAsync(User.GetUserId(), placeId, ct));

    /// <summary>HU-012: link a device (serial + pairing code on the box). Returns 201.</summary>
    [HttpPost]
    [EnableRateLimiting(PairingRateLimiter.Policy)] // 10 attempts per user every 15 min
    public async Task<ActionResult<DeviceView>> Link(
        Guid placeId, LinkDeviceRequest request, [FromServices] ILinkDeviceUseCase useCase, CancellationToken ct)
    {
        var view = await useCase.ExecuteAsync(
            new LinkDeviceCommand(User.GetUserId(), placeId, request.SerialNumber!, request.PairingCode!), ct);

        return CreatedAtAction(nameof(Get), new { placeId }, view);
    }

    /// <summary>HU-014: unlink. The history stays in the database. Returns 204.</summary>
    [HttpDelete]
    public async Task<IActionResult> Unlink(
        Guid placeId, [FromServices] IUnlinkDeviceUseCase useCase, CancellationToken ct)
    {
        await useCase.ExecuteAsync(User.GetUserId(), placeId, ct);
        return NoContent();
    }
}
