using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyWater.Devices.Api.Security;
using SyWater.Devices.Application.Ports.In;

namespace SyWater.Devices.Api.Controllers;

/// <summary>Service-to-service only (X-Internal-Key): no user token is involved, ms-iam calls it.</summary>
[ApiController]
[AllowAnonymous]
[InternalKey]
[Route("internal/users/{userId:guid}/devices")]
public sealed class InternalUserDevicesController : ControllerBase
{
    /// <summary>HU-008: releases every device linked by a user whose account was deleted. Idempotent.</summary>
    [HttpDelete]
    public async Task<ActionResult<UnlinkedDevices>> UnlinkAll(
        Guid userId, [FromServices] IUnlinkUserDevicesUseCase useCase, CancellationToken ct) =>
        Ok(new UnlinkedDevices(await useCase.ExecuteAsync(userId, ct)));

    public sealed record UnlinkedDevices(int Unlinked);
}
