# ms-devices (device-service)

Microservicio de **Dispositivos** (BC-03) de Sy Water. Arquitectura hexagonal en .NET 10, puerto **3003**.

| HU | Qué hace | Endpoint |
|----|----------|----------|
| HU-012 | Vincular un ESP32 a un lugar (serial + código de emparejamiento) | `POST /api/places/{placeId}/device` |
| HU-013 | Ver el estado (Conectado / Desconectado / Nunca ha reportado) | `GET /api/places/{placeId}/device` |
| HU-014 | Desvincular (el historial se conserva) | `DELETE /api/places/{placeId}/device` |

Los ESP32 envían un **heartbeat** por MQTT (Mosquitto) a `sywater/devices/{serial}/heartbeat` con `{"token":"...","fw":"..."}`.

## Requisitos

- `ms-iam-db`, `ms-places-db` y `ms-devices-db` aplicados; ms-iam (8081) y ms-places (3002) corriendo.
- Docker (Mosquitto) y .NET SDK 10.

## Puesta en marcha (resumen)

```powershell
docker compose up -d mosquitto
dotnet user-secrets --project src/SyWater.Devices.Api set "ConnectionStrings:Devices" "<cadena devices_app>"
dotnet user-secrets --project src/SyWater.Devices.Api set "Mqtt:Password" "<password de ms-devices>"
Copy-Item ..\ms-iam\keys\public.pem src\SyWater.Devices.Api\keys\iam-public.pem
dotnet test
dotnet run --project src/SyWater.Devices.Api
```

La guía completa (con ramas y commits) está en `claude/guia-device-service-completa.md` del proyecto.
