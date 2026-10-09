# ms-devices (device-service)

Microservicio de **Dispositivos** (BC-03) de Sy Water. Arquitectura hexagonal en .NET 10, puerto **3003**.

| HU | Qué hace | Endpoint |
|----|----------|----------|
| HU-012 | Vincular un ESP32 a un lugar (serial + código de emparejamiento) | `POST /api/places/{placeId}/device` |
| HU-013 | Ver el estado (Conectado / Desconectado / Nunca ha reportado) | `GET /api/places/{placeId}/device` |
| HU-014 | Desvincular (el historial se conserva) | `DELETE /api/places/{placeId}/device` |

### Panel de administración de medidores (solo rol ADMIN)

Requiere `ms-device-db` v1.1 (tabla `device_admin_log` y permiso de INSERT en `devices.devices`).

| Qué hace | Endpoint |
|----------|----------|
| Lista con búsqueda por serial, filtros y contadores (`status`=ALL/CONNECTED/DISCONNECTED/NEVER_REPORTED/DECOMMISSIONED, `link`=ALL/LINKED/FREE, `page`, `size`) | `GET /api/admin/devices` |
| Detalle: datos, lugares donde ha estado e historial de administración | `GET /api/admin/devices/{id}` |
| **Alta de fábrica**: 1 a 50 serials siguientes (`{"count":n}`) o uno específico (`{"serialNumber":"..."}`). Devuelve el **token y el código en claro UNA sola vez**; en la BD solo queda su hash | `POST /api/admin/devices` |
| Credenciales nuevas (las anteriores dejan de servir). 409 `device.still_linked` si alguien lo tiene vinculado | `POST /api/admin/devices/{id}/credentials` |
| Dar de baja (definitivo: el servidor ignora sus mensajes y nadie puede vincularlo). 409 si está vinculado o ya estaba de baja | `POST /api/admin/devices/{id}/decommission` |

Cada alta, cambio de credenciales y baja deja una fila en `device_admin_log` (quién y cuándo; no se puede editar ni borrar). Errores: `device.not_found` 404, `device.serial_exists` / `device.still_linked` / `device.already_decommissioned` 409, `device.invalid` 400.

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
