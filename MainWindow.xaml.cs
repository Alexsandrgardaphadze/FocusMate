// MainWindow.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
// Add this using for Navigation (if needed, though not directly used here)
// using Microsoft.UI.Xaml.Navigation;
using FocusMate.Views; // Ensure correct namespace for Pages

namespace FocusMate
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            // Enable extending content into the title bar for a modern look
            ExtendsContentIntoTitleBar = true;
            // Specify the UI element to act as the draggable title bar area.
            // Ensure an element named 'AppTitleBar' exists in MainWindow.xaml.
            // --- Adopted: Assumes <Grid x:Name="AppTitleBar" .../> exists in XAML ---
            SetTitleBar(AppTitleBar);

            // Navigate to the default page (TimerPage) when the window loads
            ContentFrame.Navigate(typeof(TimerPage)); // Ensure correct namespace
            // Optionally select the default item in NavView
            // NavView.SelectedItem = NavView.MenuItems[0]; // If needed
        }

        /// <summary>
        /// Handles navigation when a NavigationView item is selected.
        /// </summary>
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            // --- Adopted: Better handling of built-in Settings item ---
            if (args.IsSettingsSelected)
            {
                ContentFrame?.Navigate(typeof(SettingsPage));
                return; // Exit early if Settings was selected
            }

            // Check if a regular menu item was actually selected
            if (args.SelectedItem is NavigationViewItem item)
            {
                // --- Adopted: Safer Tag access with ?. ---
                switch (item.Tag?.ToString()) // Use ?. to prevent NullReference if Tag is null
                {
                    case "TimerPage":
                        ContentFrame.Navigate(typeof(TimerPage));
                        break;
                    case "AnalyticsPage":
                        ContentFrame.Navigate(typeof(AnalyticsPage));
                        break;
                    case "TasksPage":
                        ContentFrame.Navigate(typeof(TasksPage));
                        break;
                    case "SettingsPage": // Handle if you have a separate SettingsPage item
                        ContentFrame.Navigate(typeof(SettingsPage));
                        break;
                    default:
                        // Optionally handle unknown tags or navigate to a default page
                        ContentFrame.Navigate(typeof(TimerPage));
                        break;
                }
            }
            // --- Implicitly handles if args.SelectedItem is null (e.g., deselection) ---
        }
    }
}