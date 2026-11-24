namespace RadioFreeDAM
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Choosing a tab (side bar on a computer, bottom bar on a phone) always shows its first screen. Without this,
        /// "Inicio" would return to a screen left open on top of it, such as the cities of a network.
        /// </summary>
        protected override async void OnNavigated(ShellNavigatedEventArgs args)
        {
            base.OnNavigated(args);

            var tabChanged = args.Source is ShellNavigationSource.ShellItemChanged
                or ShellNavigationSource.ShellSectionChanged;

            if (tabChanged)
                await PopToRootAsync();
        }

        private async Task PopToRootAsync()
        {
            if (Navigation.NavigationStack.Count <= 1)
                return;

            try
            {
                await Navigation.PopToRootAsync(animated: false);
            }
            catch (InvalidOperationException)
            {
                // The stack changed while we were popping (for example a quick double tap): nothing to do.
            }
        }

#if WINDOWS
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            // The side bar of Windows is a NavigationView.
            if (Handler?.PlatformView is Microsoft.UI.Xaml.Controls.NavigationView sideBar)
            {
                sideBar.ItemInvoked -= OnSideBarItemInvoked;
                sideBar.ItemInvoked += OnSideBarItemInvoked;
            }
        }

        /// <summary>
        /// Shell does not navigate when the tab that is already open is chosen again, so OnNavigated never runs.
        /// Choosing it again is the usual way to go back to the top of the tab.
        /// </summary>
        private async void OnSideBarItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender,
            Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            // The container of the tab that is open is already selected when it is chosen again.
            if (args.InvokedItemContainer?.IsSelected == true)
                await PopToRootAsync();
        }
#endif
    }
}
