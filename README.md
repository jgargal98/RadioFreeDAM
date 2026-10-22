# RadioFreeDAM

Plataforma de radio online multiplataforma: cliente **.NET MAUI** + API **ASP.NET Core** (.NET 10).
Proyecto 01 · DEINT · Curso 2026–2027.

## Arquitectura

```
App MAUI ──HTTP/JSON──▶ API ASP.NET Core ──▶ Radio Browser    (emisoras, Fase 2)
                                         └─▶ Base de datos    (usuarios y favoritas, Fase 3)
```

- La app **solo** habla con nuestra API: nunca con Radio Browser ni con la base de datos.
- El audio se reproduce en la app con la `StreamUrl` que entrega la API.

| Proyecto | Responsabilidad |
|---|---|
| `RadioFreeDAM.App` | Interfaz, navegación, reproducción y estado de las pantallas (MVVM). |
| `RadioFreeDAM.Api` | Lógica de negocio, validaciones, catálogo de emisoras y acceso a datos. |
| `RadioFreeDAM.Shared` | DTOs comunes (`StationDto`, `HealthDto`): el "contrato" JSON entre app y API. |

### App (MVVM)

- `Views/`: páginas XAML (Inicio, Reproductor, Favoritos). Solo presentación.
- `ViewModels/`: estado y comandos de cada pantalla.
- `Services/`: comunicación con la API (`StationApiService`, `ApiStatusService`), reproductor (`IPlayerService`) y navegación.
- `Models/`: tipos propios de la app (`PlaybackState`).

### API

- `Controllers/`: endpoints HTTP (`HealthController`, `StationsController`).
- `Services/`: obtención de emisoras (`IStationService`; en la Fase 1, `MockStationService`).

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/health` | Comprueba que el servidor responde. |
| GET | `/api/stations` | Catálogo de emisoras. |

## Cómo ejecutarlo

1. Arranca la API (escucha en el puerto **5080**):
   ```bash
   dotnet run --project RadioFreeDAM.Api
   ```
   En Visual Studio: clic derecho en la solución → *Configurar proyectos de inicio* → *Varios proyectos de inicio* → API y App en *Iniciar*.
2. Arranca la app (Windows Machine o emulador Android).

La dirección de la API se elige en `RadioFreeDAM.App/Services/ApiConfig.cs`:

- Windows → `http://localhost:5080/`
- Emulador Android → `http://10.0.2.2:5080/`
- Móvil Android real → `http://<IP del ordenador>:5080/`. Cambia `LocalNetworkHost` por la IPv4 de `ipconfig`; el móvil y el ordenador tienen que estar en la misma red, y el Firewall de Windows debe permitir el puerto 5080.

## Fases

- [x] **Fase 1** · Arranque, interfaz y arquitectura: navegación por pestañas, 12 emisoras de prueba servidas por la API y reproductor simulado.
- [ ] **Fase 2** · Emisoras reales (Radio Browser) y reproducción en streaming.
- [ ] **Fase 3** · Usuarios y favoritas persistentes.
- [ ] **Fase 4** · Consolidación, mejora y entrega.
