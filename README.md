# RadioFreeDAM

Plataforma de radio online multiplataforma: cliente **.NET MAUI** + API **ASP.NET Core** (.NET 10).
Proyecto 01 · DEINT · Curso 2026–2027.

## Arquitectura

```
App MAUI ──HTTP/JSON──▶ API ASP.NET Core ──▶ Radio Browser      (catálogo de emisoras)
                                         └─▶ MariaDB remota     (usuarios y favoritas)
```

- La app **solo** habla con nuestra API: nunca con Radio Browser ni con la base de datos.
- El audio se reproduce en la app con la `StreamUrl` que entrega la API.

| Proyecto | Responsabilidad |
|---|---|
| `RadioFreeDAM.App` | Interfaz, navegación, reproducción, sesión en memoria y estado de las pantallas (MVVM). |
| `RadioFreeDAM.Api` | Lógica de negocio, validaciones, autenticación, catálogo de emisoras y acceso a datos. |
| `RadioFreeDAM.Shared` | DTOs comunes (`StationDto`, `AuthResponse`, `FavoriteDto`...): el "contrato" JSON entre app y API. |

### App (MVVM)

- `Views/`: páginas XAML (Inicio, Reproductor, Favoritos, Ajustes, Iniciar sesión, Crear cuenta) y los controles `MiniPlayerView`, `SoundWavesView` y `BackHeaderView`. Solo presentación.
- `ViewModels/`: estado y comandos de cada pantalla.
- `Services/`:
  - Comunicación con la API: `StationApiService`, `ApiStatusService`, `AuthApiService`, `FavoritesApiService`.
  - Estado compartido por varias pantallas: `IPlayerService` (reproducción, con MediaElement), `ISessionService` (usuario y token) e `IFavoritesService` (favoritas del usuario).
  - Navegación: `INavigationService`.
- `Models/`: tipos propios de la app (`PlaybackState`: detenido, conectando, reproduciendo, error).

### API

- `Controllers/`: endpoints HTTP (`HealthController`, `StationsController`, `AuthController`, `FavoritesController`).
- `Services/`: lógica de negocio.
  - Catálogo: `IStationService`. `RadioBrowserStationService` consulta Radio Browser; `MockStationService` sirve datos de prueba sin Internet; `CachedStationService` los envuelve con una caché en memoria.
  - Usuarios: `AuthService` (registro y login, contraseñas con hash PBKDF2) y `TokenService` (tokens JWT).
  - Favoritas: `FavoriteService` (reglas: usuario existente, sin duplicados, emisora existente).
- `Repositories/`: acceso a datos (`UserRepository`, `FavoriteRepository`).
- `Data/`: `RadioFreeDamDbContext` (EF Core) y las migraciones que crean las tablas.
- `Models/`: entidades de la base de datos (`User`, `Favorite`) y formato externo de Radio Browser (`RadioBrowserStation`).
- `ApiExceptionHandler`: convierte los errores de negocio en respuestas 400/401/404/409/502.

### Diseño

- **Ajustes** (engranaje en Inicio y Favoritos): cuenta (iniciar o cerrar sesión), tema y color de acento.
- Tema **claro y oscuro**: sigue al sistema o se elige en Ajustes; la elección se recuerda.
- **Color de acento** (`AccentService`): por defecto, el color del logo de la emisora que suena (`StationColorService`, con SkiaSharp); también 6 colores fijos o cualquier tono con una barra deslizante. Se publica como recursos dinámicos (`Primary`, `OnPrimary`, `PrimaryText`, `PrimarySoft`) y `AccentPalette` lo adapta a cada tema para que siempre se lea bien (por ejemplo, elige texto negro o blanco encima del color según el contraste).
- **Reproductor**: fondo con el color de acento suavizado y **ondas de sonido** (`SoundWavesView`, dibujadas con `GraphicsView`) que se mueven mientras suena. Son decorativas: MediaElement no da acceso al volumen real.
- Icono y pantalla de carga con una radio clásica.

## Base de datos

MariaDB remota del centro (servidor, base de datos y usuario proporcionados por el profesor), compartida con otro proyecto. Por eso todas las tablas llevan el prefijo `rfd_`:

| Tabla | Contenido |
|---|---|
| `rfd_users` | Id, nombre, correo (**único**), hash de la contraseña y fecha de alta. |
| `rfd_favorites` | Id, usuario (**clave foránea** a `rfd_users`), Id de la emisora en Radio Browser y fecha. Índice **único** (usuario, emisora): sin duplicados. |
| `rfd_ef_migrations` | Migraciones aplicadas (la gestiona EF Core). |

Las favoritas guardan **solo el Id** de la emisora; al listarlas, la API pide los datos a Radio Browser en una sola llamada (`byuuid`), pasando por la caché.
Las tablas se crean solas al arrancar la API (`Database.Migrate()`).

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/health` | Comprueba que el servidor responde. |
| GET | `/api/stations` | Las 20 emisoras más populares de España. |
| GET | `/api/stations?name=jazz` | Busca por nombre en todo Radio Browser (hasta 30). `400` si la búsqueda tiene menos de 2 caracteres. |
| POST | `/api/auth/register` | Crea una cuenta y devuelve un token. `400` datos no válidos, `409` correo ya registrado. |
| POST | `/api/auth/login` | Devuelve un token. `401` si el correo o la contraseña no son correctos. |
| GET | `/api/auth/me` 🔒 | Usuario del token. La app lo usa para validar una sesión recordada. `401` si ya no vale. |
| GET | `/api/favorites` 🔒 | Favoritas del usuario, con los datos actuales de cada emisora. |
| POST | `/api/favorites` 🔒 | Añade `{ "stationId": "..." }`. `404` emisora inexistente, `409` ya era favorita. |
| DELETE | `/api/favorites/{stationId}` 🔒 | Quita una favorita. `404` si no estaba. |

🔒 = necesita la cabecera `Authorization: Bearer <token>`; sin ella, `401`. El usuario sale del token, nunca de la URL.

Las peticiones a Radio Browser se reintentan hasta 2 veces si fallan por un corte momentáneo. Si aun así no responde, la API devuelve `502 Bad Gateway` y la app muestra un mensaje distinto al de "API apagada".
La configuración de Radio Browser (servidor, país, límites, timeout) está en la sección `RadioBrowser` de `appsettings.json`.

### Caché de emisoras

La API no consulta Radio Browser en cada petición: guarda en memoria las populares durante 10 minutos y cada búsqueda durante 5 (sección `StationCache`).
También guarda cada emisora por su Id, así que las favoritas que ya aparecieron en Inicio o en una búsqueda no necesitan otra llamada.
Si Radio Browser falla y hay una copia guardada de menos de 24 horas, se devuelve esa copia en lugar del error.
La caché se vacía al reiniciar la API.

## Cómo ejecutarlo

### 1. Secretos (solo la primera vez)

Los datos de conexión a la base de datos y la clave de los tokens **no están en el repositorio**: se guardan con `dotnet user-secrets` en tu usuario de Windows.

```bash
dotnet user-secrets set "ConnectionStrings:RadioFreeDAM" "Server=SERVIDOR;Port=3306;Database=BASE_DE_DATOS;User=USUARIO;Password=CONTRASEÑA" --project RadioFreeDAM.Api
```

```bash
dotnet user-secrets set "Jwt:Key" "UNA_CADENA_ALEATORIA_DE_AL_MENOS_32_CARACTERES" --project RadioFreeDAM.Api
```

### 2. Arrancar

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

### Pruebas automáticas

`RadioFreeDAM.Api.Tests` (xUnit) comprueba las reglas de negocio con una base de datos SQLite en memoria que tiene los mismos índices únicos y claves foráneas que MariaDB: correo único, contraseña con hash, login incorrecto, favoritas sin duplicados, de un usuario existente y aisladas por usuario, y el comportamiento de la caché.

```bash
dotnet test RadioFreeDAM.Api.Tests
```

### Cambios en la base de datos

Las migraciones se generan con la herramienta local `dotnet-ef` (versión fijada en `dotnet-tools.json`):

```bash
dotnet tool restore
```

```bash
dotnet tool run dotnet-ef migrations add NombreDelCambio --project RadioFreeDAM.Api --output-dir Data/Migrations
```

## Fases

- [x] **Fase 1** · Arranque, interfaz y arquitectura: navegación por pestañas, 12 emisoras de prueba servidas por la API y reproductor simulado.
- [x] **Fase 2** · Emisoras reales de Radio Browser, búsqueda por nombre, reproducción en streaming con MediaElement y mensajes de error de catálogo, conexión y audio.
- [x] **Fase 3** · Usuarios (registro e inicio de sesión con JWT) y favoritas persistentes en MariaDB.
- [x] **Fase 4** · Consolidación y mejoras:
  - **Mini reproductor** en Inicio y Favoritos.
  - **Recordar la sesión** (casilla «Mantener la sesión iniciada»; token en SecureStorage, validado con `api/auth/me`).
  - **Búsqueda mientras se escribe** con mensaje de «sin resultados».
  - Reintentos automáticos ante cortes de red (app → API y API → Radio Browser) y pruebas automáticas de la API.
- [ ] **Entrega** · Vídeo de demostración (3 de diciembre).
