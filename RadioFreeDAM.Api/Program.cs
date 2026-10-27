using Microsoft.Extensions.Options;
using RadioFreeDAM.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Catálogo de emisoras: Radio Browser. Para trabajar sin Internet, registrar MockStationService en su lugar:
// builder.Services.AddSingleton<IStationService, MockStationService>();
builder.Services.Configure<RadioBrowserOptions>(builder.Configuration.GetSection(RadioBrowserOptions.SectionName));
builder.Services.AddHttpClient<IStationService, RadioBrowserStationService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<RadioBrowserOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
