// App.xaml.cs
using FocusMate.Models;
using FocusMate.Services;
using FocusMate.ViewModels;
using FocusMate.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;

namespace FocusMate
{
    public partial class App : Application
    {
        private Window? _window;
        public static IServiceProvider? Services { get; private set; }

        public App()
        {
            InitializeComponent();
            Services = ConfigureServices();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Services
            services.AddSingleton<TimerService>();
            services.AddSingleton<SessionService>();
            services.AddSingleton<StorageService>();
            services.AddSingleton<NotificationService>();
            services.AddSingleton<TrayService>();
            services.AddSingleton<FocusLockService>();
            services.AddSingleton<AnalyticsService>();

            // Models
            services.AddSingleton<SettingsModel>();

            // ViewModels
            services.AddTransient<TimerViewModel>();
            services.AddTransient<AnalyticsViewModel>();
            services.AddTransient<TasksViewModel>();
            services.AddTransient<SettingsViewModel>();

            return services.BuildServiceProvider();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();

            // Initialize services
            var storageService = Services.GetService<StorageService>();
            await storageService.InitializeAsync();

            var settings = Services.GetService<SettingsModel>();
            var savedSettings = await storageService.LoadSettingsAsync();

            // Copy saved settings to current settings
            if (savedSettings != null)
            {
                settings.DefaultFocusMinutes = savedSettings.DefaultFocusMinutes;
                settings.ShortBreakMinutes = savedSettings.ShortBreakMinutes;
                settings.LongBreakMinutes = savedSettings.LongBreakMinutes;
                settings.AutoStartNext = savedSettings.AutoStartNext;
                settings.UseWindowsNotificationSound = savedSettings.UseWindowsNotificationSound;
                settings.CustomSoundPath = savedSettings.CustomSoundPath;
                settings.BlockedApps = savedSettings.BlockedApps;
                settings.BlockedSites = savedSettings.BlockedSites;
            }

            _window.Activate();

            // Initialize tray service
            var trayService = Services.GetService<TrayService>();
            trayService.Initialize(_window);
        }
    }
}