using RadioFreeDAM.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Fase 1: catálogo de prueba. En la Fase 2 se sustituye por el servicio que consulta Radio Browser.
builder.Services.AddSingleton<IStationService, MockStationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
