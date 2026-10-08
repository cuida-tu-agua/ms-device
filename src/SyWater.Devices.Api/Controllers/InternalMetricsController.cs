using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyWater.Devices.Api.Security;
using SyWater.Devices.Application.Devices;
using SyWater.Devices.Application.Ports.In;

namespace SyWater.Devices.Api.Controllers;

/// <summary>Service-to-service only (X-Internal-Key): ms-iam builds the administrator's metrics from it.</summary>
[ApiController]
[AllowAnonymous]
[InternalKey]
[Route("internal/metrics")]
public sealed class InternalMetricsController : ControllerBase
{
    /// <summary>HU-062</summary>
    [HttpGet]
    public async Task<ActionResult<DeviceMetrics>> Get([FromServices] IGetDeviceMetricsUseCase useCase, CancellationToken ct) =>
        Ok(await useCase.ExecuteAsync(ct));
}
