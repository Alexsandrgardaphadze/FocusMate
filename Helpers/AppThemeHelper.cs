// Helpers/AppThemeHelper.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;

namespace FocusMate.Helpers
{
    public static class AppThemeHelper
    {
        public static void EnforceDarkTheme(Window window)
        {
            // Set the requested theme for the window
            if (window.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = ElementTheme.Dark;
            }

            // Subscribe to system theme changes to maintain dark theme
            SystemThemeChangedHelper.Instance.ThemeChanged += OnSystemThemeChanged;
        }

        public static void ApplySystemAccentColor(ResourceDictionary resources)
        {
            // Use system accent color for key elements
            if (resources.TryGetValue("SystemAccentColor", out object accentColor))
            {
                // Apply accent color to various resources
                resources["PrimaryBrush"] = new SolidColorBrush((Windows.UI.Color)accentColor);
                resources["ButtonBackgroundPressed"] = new SolidColorBrush((Windows.UI.Color)accentColor);
            }
        }

        private static void OnSystemThemeChanged(object sender, SystemThemeChangedEventArgs e)
        {
            // Re-apply dark theme if system theme changes
            // This ensures our app stays in dark mode regardless of system setting
            if (Window.Current.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = ElementTheme.Dark;
            }
        }
    }

    // Helper to detect system theme changes
    internal class SystemThemeChangedHelper
    {
        private static SystemThemeChangedHelper _instance;
        public static SystemThemeChangedHelper Instance => _instance ??= new SystemThemeChangedHelper();

        public event EventHandler<SystemThemeChangedEventArgs> ThemeChanged;

        private SystemThemeChangedHelper()
        {
            // Listen for theme changes (simplified implementation)
            // In a real implementation, you'd use UISettings and watch for changes
            Application.Current.Resources.ThemeDictionaries.CollectionChanged += (s, e) =>
            {
                ThemeChanged?.Invoke(this, new SystemThemeChangedEventArgs());
            };
        }
    }

    internal class SystemThemeChangedEventArgs : EventArgs { }
}