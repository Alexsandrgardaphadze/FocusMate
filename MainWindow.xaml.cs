// MainWindow.xaml.cs
using FocusMate.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace FocusMate
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // Navigate to timer page by default
            ContentFrame.Navigate(typeof(TimerPage));
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                switch (item.Tag.ToString())
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
                    case "SettingsPage":
                        ContentFrame.Navigate(typeof(SettingsPage));
                        break;
                }
            }
        }
    }
}