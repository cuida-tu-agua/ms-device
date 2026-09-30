using SyWater.Devices.Application.Ports.In;

namespace SyWater.Devices.Api.Background;

/// <summary>
/// HU-013: every minute, CONNECTED devices that stopped reporting are saved as DISCONNECTED.
/// (The API already calculates the status on each read; this keeps the column honest for other readers.)
/// </summary>
public sealed class DeviceStatusSweeper(
    IServiceScopeFactory scopes,
    IConfiguration config,
    ILogger<DeviceStatusSweeper> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = config.GetValue("Devices:StatusSweepSeconds", 60);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var changed = await scope.ServiceProvider
                        .GetRequiredService<IRefreshDeviceStatusUseCase>()
                        .ExecuteAsync(stoppingToken);

                    if (changed > 0)
                        logger.LogInformation("{Count} device(s) marked as DISCONNECTED", changed);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Device status sweep failed; it will run again in {Seconds}s", seconds);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // App is stopping.
        }
    }
}
