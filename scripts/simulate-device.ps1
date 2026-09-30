<#
.SYNOPSIS
  Pretends to be an ESP32: publishes heartbeats to the local Mosquitto container.

.EXAMPLE
  # One heartbeat of the dev device 1 (seed of ms-devices-db)
  .\scripts\simulate-device.ps1 -Serial SW-ESP32-000001 -Token 1gQn7MIHy0iGmO2hDpGM1n_uAdZRQUtJ -Once

.EXAMPLE
  # A heartbeat every 30 s until Ctrl+C
  .\scripts\simulate-device.ps1 -Serial SW-ESP32-000001 -Token 1gQn7MIHy0iGmO2hDpGM1n_uAdZRQUtJ
#>
param(
    [Parameter(Mandatory)] [string]$Serial,
    [Parameter(Mandatory)] [string]$Token,
    [string]$BrokerPassword = $env:MQTT_DEVICE_PASSWORD,
    [int]$Seconds = 30,
    [string]$Firmware = 'sim-1.0.0',
    [switch]$Once
)

if (-not $BrokerPassword) {
    throw 'Pass -BrokerPassword or set $env:MQTT_DEVICE_PASSWORD (password of the broker user sywater-device).'
}

$topic = "sywater/devices/$Serial/heartbeat"
$payload = @{ token = $Token; fw = $Firmware } | ConvertTo-Json -Compress

do {
    # The JSON goes through stdin (-s): passing quotes as arguments breaks in Windows PowerShell 5.1
    $payload | docker exec -i sy_water_mosquitto mosquitto_pub -u sywater-device -P $BrokerPassword -q 1 -t $topic -s
    if ($LASTEXITCODE -ne 0) { throw "mosquitto_pub failed (exit $LASTEXITCODE). Is the container sy_water_mosquitto running?" }
    Write-Host ("{0:HH:mm:ss}  heartbeat -> {1}" -f (Get-Date), $topic)
    if (-not $Once) { Start-Sleep -Seconds $Seconds }
} while (-not $Once)
