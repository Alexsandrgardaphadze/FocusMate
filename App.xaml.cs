// App.xaml.cs
using FocusMate.Models;
using FocusMate.Services;
using Microsoft.Extensions.DependencyInjection; // Requires Microsoft.Extensions.DependencyInjection NuGet package
using Microsoft.UI;
using Microsoft.UI.Dispatching; // For DispatcherQueue
using Microsoft.UI.Windowing; // For AppWindow
using Microsoft.UI.Xaml;
using System; // For IServiceProvider, Exception
using System.Threading.Tasks; // For async/await
using WinRT; // For WindowNative interop

namespace FocusMate
{
    public partial class App : Application
    {
        private Window? _window;
        private AppWindow? _appWindow;

        // --- Adopted Static Properties ---
        public static IServiceProvider? Services { get; private set; }
        public static new App Current => (App)Application.Current; // Allows App.Current as App
        public Window? MainWindow => _window; // Safe access to main Window
        public AppWindow? AppMainWindow => _appWindow; // Safe access to AppWindow

        public App()
        {
            InitializeComponent();
            Services = ConfigureServices();
            // --- Adopted Exception Handler ---
            UnhandledException += App_UnhandledException;
        }

        /// <summary>
        /// Configures the application's dependency injection container.
        /// Registers services, models, and view models with appropriate lifetimes.
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // --- Register Core Services ---
            services.AddSingleton<StorageService>();
            services.AddSingleton<SettingsService>(); // Register SettingsService
            services.AddSingleton<TimerService>();
            services.AddSingleton<SessionService>(); // Depends on StorageService & TimerService
            services.AddSingleton<AnalyticsService>(); // Depends on SessionService
            // NotificationService needs SettingsModel and DispatcherQueue
            services.AddSingleton<NotificationService>((provider) =>
            {
                // --- Improved/Direct Injection of SettingsModel ---
                var settingsModel = provider.GetRequiredService<SettingsModel>();
                var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
                return new NotificationService(settingsModel, dispatcherQueue);
            });
            services.AddSingleton<TrayService>((provider) =>
            {
                // TrayService constructor takes TimerService
                var timerService = provider.GetRequiredService<TimerService>();
                return new TrayService(timerService);
            });
            services.AddSingleton<FocusLockService>(); // Depends on TimerService, NotificationService, SettingsService, SessionService

            // --- Register Data Models ---
            // --- Improved: Register SettingsModel sourced from SettingsService ---
            // This allows ViewModels/Services to depend directly on SettingsModel
            services.AddSingleton<SettingsModel>((provider) =>
            {
                // Get the SettingsModel instance managed by SettingsService
                var settingsService = provider.GetRequiredService<SettingsService>();
                return settingsService.Settings; // Return the SettingsModel instance
            });

            // --- Register ViewModels ---
            // ViewModels are typically created per page/view.
            services.AddTransient<ViewModels.TimerViewModel>(); // Depends on TimerService, SessionService, NotificationService, SettingsModel
            services.AddTransient<ViewModels.AnalyticsViewModel>(); // Depends on AnalyticsService
            services.AddTransient<ViewModels.TasksViewModel>(); // Depends on StorageService
            services.AddTransient<ViewModels.SettingsViewModel>(); // Depends on StorageService, FocusLockService, SettingsModel

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// The main entry point for the application. Initializes services and the main window.
        /// </summary>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            // --- Adopted Try-Catch for Robustness ---
            try
            {
                // 1. Create the main application window
                _window = new MainWindow();

                // --- Adopted AppWindow Acquisition ---
                // Get the AppWindow for proper window management (e.g., in TrayService if updated)
                var hwnd = WindowNative.GetWindowHandle(_window);
                var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                _appWindow = AppWindow.GetFromWindowId(windowId);

                // Activate the window to make it visible and establish UI context
                _window.Activate();

                // 2. Initialize foundational services
                var storageService = Services?.GetService<StorageService>();
                // StorageService initializes its data folder asynchronously in its constructor via FireAndForget.
                // We don't need to await an InitializeAsync method here.
                // await storageService?.InitializeAsync(); // If it had one needed in OnLaunched

                // 3. Load Application Settings using SettingsService
                // Get the SettingsService instance (using generic GetService for cleaner syntax)
                var settingsService = Services?.GetService<SettingsService>();
                if (settingsService != null)
                {
                    // SettingsService is responsible for loading settings from StorageService
                    // and applying/populating the SettingsModel singleton.
                    await settingsService.InitializeAsync();
                    // The SettingsModel singleton is now populated by SettingsService
                }

                // 4. Initialize Services that depend on loaded Settings or other services
                // Get the TimerService instance (using generic GetService)
                var timerService = Services?.GetService<TimerService>();
                // Get the SettingsModel instance (managed/populated by SettingsService)
                var settingsModel = Services?.GetService<SettingsModel>();

                if (timerService != null && settingsModel != null)
                {
                    // Initialize the TimerService with the loaded default settings
                    // obtained from SettingsService.
                    timerService.Initialize(settingsModel);
                }

                // Get the SessionService instance and initialize it (using generic GetService)
                var sessionService = Services?.GetService<SessionService>();
                if (sessionService != null)
                {
                    // SessionService has InitializeAsync which should be called.
                    await sessionService.InitializeAsync();
                }

                // Note: AnalyticsService, FocusLockService are registered as Singletons.
                // Their constructors handle initial setup/subscriptions based on injected dependencies.
                // No explicit initialization call is needed here beyond their creation by the DI container.
                // --- Fixed: Removed incorrect FocusLockService.Initialize call ---

                // 5. Initialize UI Integration Services
                // Get the TrayService instance (using generic GetService)
                var trayService = Services?.GetService<TrayService>();
                if (trayService != null && _window != null)
                {
                    // Link the TrayService to the main application window
                    trayService.Initialize(_window);
                }

                // NotificationService registration handles its own initialization
                // via its constructor and the Initialize() method.
                // It is created when first requested by the DI container.
                // var notificationService = Services?.GetService<NotificationService>();
                // notificationService?.Initialize(); // If it had an Initialize() method needed.

            }
            // --- Adopted Try-Catch for Robustness ---
            catch (Exception ex)
            {
                // --- Improved Error Handling ---
                // Log the detailed exception for debugging
                System.Diagnostics.Debug.WriteLine($"[App.OnLaunched] Fatal startup error: {ex}");

                // TODO: Implement user-facing error reporting
                // Example: Show a simple MessageBox or a dedicated error page
                // This prevents the app from silently crashing or hanging.
                // Consider using a logging framework like Serilog or NLog for production.
            }
        }

        // --- Adopted Exception Handler ---
        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // TODO: Add proper error logging (e.g., to a file)
            System.Diagnostics.Debug.WriteLine($"[App] Unhandled exception: {e.Exception}");
            // Prevent the app from crashing the process unexpectedly in release mode
            e.Handled = true;
        }
    }
}