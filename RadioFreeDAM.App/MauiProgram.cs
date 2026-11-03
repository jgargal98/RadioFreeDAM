using System.Net;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;

namespace RadioFreeDAM
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                // Reproducción de audio. Sin servicio en primer plano de Android: no hay notificación de reproducción.
                .UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: false)
                .ConfigureMauiHandlers(handlers =>
                {
#if WINDOWS
    				Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler.Mapper.AppendToMapping("KeyboardAccessibleCollectionView", (handler, view) =>
    				{
    					handler.PlatformView.SingleSelectionFollowsFocus = false;
    				});
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                });

#if DEBUG
    		builder.Logging.AddDebug();
    		builder.Services.AddLogging(configure => configure.AddDebug());
#endif

            // Un único HttpClient apuntando a NUESTRA API (la app nunca habla con Radio Browser ni con la BD).
            builder.Services.AddHttpClient(ApiConfig.HttpClientName, client =>
                {
                    client.BaseAddress = new Uri(ApiConfig.BaseUrl);
                    // Los tiempos los controla la política de reintentos de abajo.
                    client.Timeout = Timeout.InfiniteTimeSpan;
                })
                .AddStandardResilienceHandler(resilience =>
                {
                    // Reintenta solo cortes de red y tiempos agotados. No reintenta un 502 (la API ya ha
                    // reintentado con Radio Browser) ni los errores de validación (400, 401, 409...).
                    resilience.Retry.ShouldHandle = args => ValueTask.FromResult(args.Outcome switch
                    {
                        { Exception: HttpRequestException or Polly.Timeout.TimeoutRejectedException } => true,
                        { Result.StatusCode: HttpStatusCode.ServiceUnavailable or HttpStatusCode.RequestTimeout } => true,
                        _ => false
                    });
                    // Nunca repite un POST o DELETE: podría registrar dos veces o duplicar una favorita.
                    resilience.Retry.DisableForUnsafeHttpMethods();
                    resilience.Retry.MaxRetryAttempts = 2;
                    resilience.Retry.Delay = TimeSpan.FromMilliseconds(500);
                    resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
                    resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(25);
                });
            builder.Services.AddSingleton(sp =>
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(ApiConfig.HttpClientName));

            // Services
            builder.Services.AddSingleton<IApiStatusService, ApiStatusService>();
            builder.Services.AddSingleton<IStationApiService, StationApiService>();
            // Una sola instancia: PlayerPage la recibe como clase concreta (para entregarle el MediaElement)
            // y los ViewModels como IPlayerService.
            builder.Services.AddSingleton<MediaElementPlayerService>();
            builder.Services.AddSingleton<IPlayerService>(sp => sp.GetRequiredService<MediaElementPlayerService>());
            builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
            builder.Services.AddSingleton<IAuthApiService, AuthApiService>();
            builder.Services.AddSingleton<IFavoritesApiService, FavoritesApiService>();
            builder.Services.AddSingleton<ISessionService, SessionService>();
            builder.Services.AddSingleton<IFavoritesService, FavoritesService>();
            builder.Services.AddSingleton<IThemeService, ThemeService>();
            builder.Services.AddSingleton<IDialogService, ShellDialogService>();

            // ViewModels
            builder.Services.AddSingleton<HomeViewModel>();
            builder.Services.AddSingleton<PlayerViewModel>();
            builder.Services.AddSingleton<FavoritesViewModel>();

            // Views (pestañas)
            builder.Services.AddSingleton<HomePage>();
            builder.Services.AddSingleton<PlayerPage>();
            builder.Services.AddSingleton<FavoritesPage>();

            // Pantallas de cuenta: se apilan encima de la pestaña actual. Nuevas cada vez (formulario vacío).
            builder.Services.AddTransientWithShellRoute<LoginPage, LoginViewModel>(Routes.Login);
            builder.Services.AddTransientWithShellRoute<RegisterPage, RegisterViewModel>(Routes.Register);

            return builder.Build();
        }
    }
}
