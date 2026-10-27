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
- `Services/`: comunicación con la API (`StationApiService`, `ApiStatusService`), reproductor (`IPlayerService`, implementado con MediaElement en `MediaElementPlayerService`) y navegación.
- `Models/`: tipos propios de la app (`PlaybackState`: detenido, conectando, reproduciendo, error).

### API

- `Controllers/`: endpoints HTTP (`HealthController`, `StationsController`).
- `Services/`: obtención de emisoras (`IStationService`). `RadioBrowserStationService` consulta Radio Browser; `MockStationService` sirve datos de prueba sin Internet; `CachedStationService` los envuelve con una caché en memoria.
- `Models/`: formato externo de Radio Browser (`RadioBrowserStation`), que se convierte a `StationDto` antes de salir de la API.

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/health` | Comprueba que el servidor responde. |
| GET | `/api/stations` | Las 20 emisoras más populares de España. |
| GET | `/api/stations?name=jazz` | Busca por nombre en todo Radio Browser (hasta 30). `400` si la búsqueda tiene menos de 2 caracteres. |

Si Radio Browser no responde, la API devuelve `502 Bad Gateway` y la app muestra un mensaje distinto al de "API apagada".
La configuración de Radio Browser (servidor, país, límites, timeout) está en la sección `RadioBrowser` de `appsettings.json`.

### Caché de emisoras

La API no consulta Radio Browser en cada petición: guarda en memoria las populares durante 10 minutos y cada búsqueda durante 5 (sección `StationCache`).
Si Radio Browser falla y hay una copia guardada de menos de 24 horas, se devuelve esa copia en lugar del error.
La caché se vacía al reiniciar la API.

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
- [x] **Fase 2** · Emisoras reales de Radio Browser, búsqueda por nombre, reproducción en streaming con MediaElement y mensajes de error de catálogo, conexión y audio.
- [ ] **Fase 3** · Usuarios y favoritas persistentes.
- [ ] **Fase 4** · Consolidación, mejora y entrega.
