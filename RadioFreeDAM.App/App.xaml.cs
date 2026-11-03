namespace RadioFreeDAM
{
    public partial class App : Application
    {
        private readonly ISessionService _session;
        private readonly IFavoritesService _favorites;

        public App(ISessionService session, IFavoritesService favorites, IThemeService theme, IAccentService accent)
        {
            InitializeComponent();
            _session = session;
            _favorites = favorites;

            // Tema y color de acento elegidos por el usuario la última vez.
            theme.ApplySaved();
            accent.Initialize();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell()) { Title = "RadioFreeDAM" };
        }

        protected override async void OnStart()
        {
            base.OnStart();

            // Si el usuario marcó «Mantener la sesión iniciada», se recupera su sesión y sus favoritas.
            if (await _session.RestoreAsync())
            {
                try
                {
                    await _favorites.LoadAsync();
                }
                catch (Exception ex) when (ApiException.IsCommunicationError(ex))
                {
                    // La pantalla Favoritos mostrará el error y permitirá reintentar.
                }
            }
        }
    }
}
