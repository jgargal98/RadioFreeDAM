using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RadioFreeDAM.Api;
using RadioFreeDAM.Api.Data;
using RadioFreeDAM.Api.Models;
using RadioFreeDAM.Api.Repositories;
using RadioFreeDAM.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Si falla una anotación de los DTOs ([Required], [EmailAddress]...), el "title" lleva
        // los mensajes en español para que la app pueda mostrarlos tal cual.
        options.InvalidModelStateResponseFactory = context =>
        {
            var messages = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct();

            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = string.Join(" ", messages)
            };
            return new BadRequestObjectResult(problem);
        };
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);

// ---------- Base de datos (MariaDB) ----------
// La cadena de conexión tiene la contraseña: va en user-secrets ("ConnectionStrings:RadioFreeDAM").
var connectionString = builder.Configuration.GetConnectionString(DatabaseSetup.ConnectionStringName)
    ?? throw new InvalidOperationException(
        $"Falta la cadena de conexión '{DatabaseSetup.ConnectionStringName}'. Configúrala con dotnet user-secrets (ver README).");
var serverVersion = builder.Configuration["Database:ServerVersion"] ?? "11.8.8-mariadb";

builder.Services.AddDbContext<RadioFreeDamDbContext>(options =>
    DatabaseSetup.Configure(options, connectionString, serverVersion));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();

// ---------- Usuarios y autenticación (JWT) ----------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < 32)
    throw new InvalidOperationException("Falta la clave Jwt:Key (mínimo 32 caracteres). Configúrala con dotnet user-secrets (ver README).");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Mantiene los nombres originales de los claims ("sub", "email"...).
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FavoriteService>();

// ---------- Catálogo de emisoras: Radio Browser, con una caché en memoria delante ----------
builder.Services.Configure<RadioBrowserOptions>(builder.Configuration.GetSection(RadioBrowserOptions.SectionName));
builder.Services.Configure<StationCacheOptions>(builder.Configuration.GetSection(StationCacheOptions.SectionName));
builder.Services.AddMemoryCache();

var radioBrowser = builder.Configuration.GetSection(RadioBrowserOptions.SectionName).Get<RadioBrowserOptions>()
    ?? new RadioBrowserOptions();

builder.Services.AddHttpClient<RadioBrowserStationService>((serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<RadioBrowserOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
        // El tiempo máximo lo controla la política de abajo; este es solo un tope de seguridad.
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds + 5);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
    })
    // Reintentos: si Radio Browser falla un momento (error de red, 5xx, tiempo agotado),
    // se repite la petición hasta 2 veces antes de dar el error.
    .AddStandardResilienceHandler(resilience =>
    {
        resilience.Retry.MaxRetryAttempts = 2;
        resilience.Retry.Delay = TimeSpan.FromMilliseconds(500);
        resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
        resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(radioBrowser.TimeoutSeconds);
    });

// El controlador recibe IStationService = caché que envuelve a Radio Browser.
// Para trabajar sin Internet, sustituir RadioBrowserStationService por MockStationService aquí.
builder.Services.AddScoped<IStationService>(serviceProvider =>
    ActivatorUtilities.CreateInstance<CachedStationService>(
        serviceProvider,
        serviceProvider.GetRequiredService<RadioBrowserStationService>()));

var app = builder.Build();

// Crea o actualiza las tablas rfd_* al arrancar (aplica las migraciones pendientes).
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<RadioFreeDamDbContext>().Database.Migrate();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
