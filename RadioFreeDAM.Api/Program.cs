using Microsoft.Extensions.Options;
using RadioFreeDAM.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Catálogo de emisoras: Radio Browser, con una caché en memoria delante.
builder.Services.Configure<RadioBrowserOptions>(builder.Configuration.GetSection(RadioBrowserOptions.SectionName));
builder.Services.Configure<StationCacheOptions>(builder.Configuration.GetSection(StationCacheOptions.SectionName));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddHttpClient<RadioBrowserStationService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<RadioBrowserOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
});

// El controlador recibe IStationService = caché que envuelve a Radio Browser.
// Para trabajar sin Internet, sustituir RadioBrowserStationService por MockStationService aquí.
builder.Services.AddScoped<IStationService>(serviceProvider =>
    ActivatorUtilities.CreateInstance<CachedStationService>(
        serviceProvider,
        serviceProvider.GetRequiredService<RadioBrowserStationService>()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
