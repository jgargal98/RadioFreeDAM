using CommunityToolkit.Maui;
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
            builder.Services.AddSingleton(_ => new HttpClient
            {
                BaseAddress = new Uri(ApiConfig.BaseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            });

            // Services
            builder.Services.AddSingleton<IApiStatusService, ApiStatusService>();
            builder.Services.AddSingleton<IStationApiService, StationApiService>();
            // Una sola instancia: PlayerPage la recibe como clase concreta (para entregarle el MediaElement)
            // y los ViewModels como IPlayerService.
            builder.Services.AddSingleton<MediaElementPlayerService>();
            builder.Services.AddSingleton<IPlayerService>(sp => sp.GetRequiredService<MediaElementPlayerService>());
            builder.Services.AddSingleton<INavigationService, ShellNavigationService>();

            // ViewModels
            builder.Services.AddSingleton<HomeViewModel>();
            builder.Services.AddSingleton<PlayerViewModel>();
            builder.Services.AddSingleton<FavoritesViewModel>();

            // Views
            builder.Services.AddSingleton<HomePage>();
            builder.Services.AddSingleton<PlayerPage>();
            builder.Services.AddSingleton<FavoritesPage>();

            return builder.Build();
        }
    }
}
