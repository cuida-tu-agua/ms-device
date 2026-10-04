using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SyWater.Devices.Api.Contracts;
using SyWater.Devices.Api.Security;
using SyWater.Devices.Application.Devices;
using SyWater.Devices.Application.Ports.In;
using SyWater.Devices.Application.Valve;

namespace SyWater.Devices.Api.Controllers;

[ApiController]
[Route("api/places/{placeId:guid}/device")]
public sealed class PlaceDevicesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DeviceView>> Get(
        Guid placeId, [FromServices] IGetPlaceDeviceUseCase useCase, CancellationToken ct) =>
        Ok(await useCase.ExecuteAsync(User.GetUserId(), placeId, ct));

    [HttpPost]
    [EnableRateLimiting(PairingRateLimiter.Policy)] // 10 attempts per user every 15 min
    public async Task<ActionResult<DeviceView>> Link(
        Guid placeId, LinkDeviceRequest request, [FromServices] ILinkDeviceUseCase useCase, CancellationToken ct)
    {
        var view = await useCase.ExecuteAsync(
            new LinkDeviceCommand(User.GetUserId(), placeId, request.SerialNumber!, request.PairingCode!), ct);

        return CreatedAtAction(nameof(Get), new { placeId }, view);
    }

    [HttpDelete]
    public async Task<IActionResult> Unlink(
        Guid placeId, [FromServices] IUnlinkDeviceUseCase useCase, CancellationToken ct)
    {
        await useCase.ExecuteAsync(User.GetUserId(), placeId, ct);
        return NoContent();
    }

    [HttpPost("valve-commands")]
    [InternalKey]
    public async Task<ActionResult<ValveCommandSent>> SendValveCommand(
        Guid placeId, ValveCommandRequest request, [FromServices] ISendValveCommandUseCase useCase, CancellationToken ct)
    {
        var sent = await useCase.ExecuteAsync(User.GetUserId(), placeId, request.CommandId!.Value, request.Action!.Value, ct);
        return Accepted(sent);
    }
}
