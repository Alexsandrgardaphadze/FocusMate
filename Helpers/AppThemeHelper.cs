// Helpers/AppThemeHelper.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System; // Add this for EventArgs, though not strictly needed here anymore

namespace FocusMate.Helpers
{
    public static class AppThemeHelper
    {
        /// <summary>
        /// Enforces the dark theme on the specified window.
        /// </summary>
        /// <param name="window">The window to apply the dark theme to.</param>
        public static void EnforceDarkTheme(Window window)
        {
            // Set the requested theme for the window's content
            if (window?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = ElementTheme.Dark;
            }

            // Note: Detecting and reacting to *system* theme changes in WinUI 3 Desktop
            // is non-trivial and often involves Windows.UI.ViewManagement.UISettings.
            // The previous attempt to listen to ThemeDictionaries.CollectionChanged was incorrect
            // as ThemeDictionaries is an IDictionary, not an ObservableCollection.
            // For a simple "app always dark" requirement, setting it once is usually sufficient.
            // If dynamic system theme following is needed, a more complex implementation
            // using UISettings.ColorValuesChanged would be required.
        }

        /// <summary>
        /// Applies the system accent color to specific resources within the provided dictionary.
        /// </summary>
        /// <param name="resources">The ResourceDictionary to update.</param>
        public static void ApplySystemAccentColor(ResourceDictionary resources)
        {
            // Use system accent color for key elements
            // Ensure the correct type is used (Windows.UI.Color)
            if (resources != null && resources.TryGetValue("SystemAccentColor", out object accentColor) && accentColor is Windows.UI.Color uiColor)
            {
                // Apply accent color to various resources
                resources["PrimaryBrush"] = new SolidColorBrush(uiColor);
                resources["ButtonBackgroundPressed"] = new SolidColorBrush(uiColor);
                // Add other resources as needed...
            }
            else
            {
                // Optional: Fallback color if system accent cannot be retrieved
                // resources["PrimaryBrush"] = new SolidColorBrush(Windows.UI.Colors.Blue); 
            }
        }

        // The SystemThemeChangedHelper and SystemThemeChangedEventArgs classes that caused the error
        // have been removed as they were not working correctly for the stated goal.
        // If you need dynamic theme switching based on system settings in the future,
        // a new implementation using UISettings.ColorValuesChanged would be necessary.
    }

    // Removed SystemThemeChangedHelper and SystemThemeChangedEventArgs as they were faulty.
}