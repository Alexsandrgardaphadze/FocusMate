// App.xaml.cs
using FocusMate.Models;
using FocusMate.Services;
// Correct namespaces for Dependency Injection
using Microsoft.Extensions.DependencyInjection; // Requires Microsoft.Extensions.DependencyInjection NuGet package
using Microsoft.UI.Dispatching; // For DispatcherQueue
using Microsoft.UI.Xaml;
using System; // For IServiceProvider
using System.Threading.Tasks; // For async/await


namespace FocusMate
{
    public partial class App : Application
    {
        // ... (fields and properties remain the same)
        private Window? _window;
        public static IServiceProvider? Services { get; private set; }
        public Window? MainWindow => _window; // Ensure this property exists

        public App()
        {
            InitializeComponent();
            Services = ConfigureServices();
        }

        // ... (ConfigureServices remains the same as per your latest correct version)
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // --- Register Core Services ---
            services.AddSingleton<StorageService>();
            services.AddSingleton<SettingsService>();
            services.AddSingleton<TimerService>();
            services.AddSingleton<SessionService>();
            services.AddSingleton<AnalyticsService>();
            services.AddSingleton(provider =>
            {
                var settings = provider.GetRequiredService<SettingsModel>();
                var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
                return new NotificationService(settings, dispatcherQueue);
            });
            services.AddSingleton<TrayService>();
            services.AddSingleton<FocusLockService>();

            // --- Register Data Models ---
            services.AddSingleton<SettingsModel>();

            // --- Register ViewModels ---
            services.AddTransient<ViewModels.TimerViewModel>();
            services.AddTransient<ViewModels.AnalyticsViewModel>();
            services.AddTransient<ViewModels.TasksViewModel>();
            services.AddTransient<ViewModels.SettingsViewModel>();

            return services.BuildServiceProvider();
        }


        /// <summary>
        /// The main entry point for the application. Initializes services and the main window.
        /// </summary>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            // 1. Create the main application window
            _window = new MainWindow();
            // Activate the window to make it visible and establish UI context
            _window.Activate();

            // 2. Get the StorageService instance.
            // The StorageService initializes its data folder asynchronously in its constructor via FireAndForget.
            // We don't need to await an InitializeAsync method here.
            // var storageService = Services?.GetService(typeof(StorageService)) as StorageService;
            // if (storageService != null)
            // {
            //     await storageService.InitializeAsync(); // <-- REMOVED this line
            // }

            // 3. Load Application Settings using SettingsService
            // Get the SettingsService instance (using generic GetService for cleaner syntax)
            var settingsService = Services?.GetService<SettingsService>(); // Cleaner syntax
            if (settingsService != null)
            {
                await settingsService.InitializeAsync();
                // The SettingsModel singleton is now populated by SettingsService
            }

            // 4. Initialize Services that depend on loaded Settings or other services
            // Get the TimerService instance (using generic GetService)
            var timerService = Services?.GetService<TimerService>(); // Cleaner syntax
            // Get the SettingsModel instance (using generic GetService)
            var settingsModel = Services?.GetService<SettingsModel>(); // Cleaner syntax

            if (timerService != null && settingsModel != null)
            {
                // Initialize the TimerService with the loaded default settings
                timerService.Initialize(settingsModel);
            }

            // Get the SessionService instance and initialize it (using generic GetService)
            var sessionService = Services?.GetService<SessionService>(); // Cleaner syntax
            if (sessionService != null)
            {
                await sessionService.InitializeAsync();
            }

            // 5. Initialize UI Integration Services
            // Get the TrayService instance (using generic GetService)
            var trayService = Services?.GetService<TrayService>(); // Cleaner syntax
            if (trayService != null && _window != null)
            {
                // Link the TrayService to the main application window
                trayService.Initialize(_window);
            }

            // NotificationService registration handles its own initialization
            // via its constructor and the Initialize() method.
            // It is created when first requested by the DI container.
        }
    }
}