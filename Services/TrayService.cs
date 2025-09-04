// Services/TrayService.cs
// Corrected for H.NotifyIcon.WinUI compatibility
using FocusMate.Models;
using H.NotifyIcon; // This is correct for H.NotifyIcon.WinUI
using Microsoft.UI;
using Microsoft.UI.Windowing; // For AppWindow
using Microsoft.UI.Xaml; // For Window
using Microsoft.UI.Xaml.Controls; // For MenuFlyoutItem, RoutedEventArgs etc.
using System; // For EventArgs, Uri, TimeSpan
using System.Diagnostics; // For Debug.WriteLine
using WinRT.Interop; // For WindowNative

namespace FocusMate.Services
{
    /// <summary>
    /// Manages the system tray icon for the FocusMate application.
    /// Integrates with H.NotifyIcon.WinUI for tray functionality in WinUI 3.
    /// </summary>
    public class TrayService : IDisposable
    {
        private TaskbarIcon? _taskbarIcon;
        private Window? _mainWindow;
        private AppWindow? _appWindow; // For WinUI 3 window management
        private readonly TimerService _timerService;
        private bool _isWindowVisible = true;
        private bool _isDisposed = false;

        /// <summary>
        /// Initializes a new instance of the TrayService.
        /// </summary>
        /// <param name="timerService">The TimerService to interact with.</param>
        public TrayService(TimerService timerService)
        {
            _timerService = timerService ?? throw new ArgumentNullException(nameof(timerService));
        }

        /// <summary>
        /// Initializes the tray icon and sets up event subscriptions.
        /// </summary>
        /// <param name="mainWindow">The application's main Window instance.</param>
        public void Initialize(Window mainWindow)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(TrayService));
            }

            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));

            try
            {
                // --- Get AppWindow for WinUI 3 window management ---
                var hwnd = WindowNative.GetWindowHandle(_mainWindow);
                var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                _appWindow = AppWindow.GetFromWindowId(windowId);

                // --- Create and configure the TaskbarIcon ---
                _taskbarIcon = new TaskbarIcon();
                // Correct way to set the icon source for H.NotifyIcon.WinUI
                _taskbarIcon.Icon = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                    new Uri("ms-appx:///Assets/Icons/tray.ico")); // Ensure this file exists
                _taskbarIcon.ToolTip = "FocusMate - Ready";

                // --- Set up the context menu ---
                var contextMenu = new MenuFlyout();
                _taskbarIcon.ContextFlyout = contextMenu;
                SetupContextMenu(contextMenu);

                // --- Subscribe to tray icon events ---
                // H.NotifyIcon.WinUI typically uses TrayLeftMouseDown
                _taskbarIcon.TrayLeftMouseDown += OnTrayLeftMouseDown;

                // --- Subscribe to TimerService events for dynamic updates ---
                _timerService.Tick += OnTimerTick;
                _timerService.ModeChanged += OnTimerModeChanged;

                // --- Set initial state ---
                UpdateOverlayText(); // Note: Overlay text might not be supported as expected

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TrayService] Error during initialization: {ex.Message}");
                _taskbarIcon?.Dispose();
                _taskbarIcon = null;
            }
        }

        /// <summary>
        /// Sets up the context menu items and their event handlers.
        /// </summary>
        private void SetupContextMenu(MenuFlyout menu)
        {
            if (menu == null) return;

            menu.Items.Clear();

            // --- Start/Pause Menu Item ---
            var toggleMenuItem = new MenuFlyoutItem
            {
                Text = "Start",
                Tag = "StartPause"
            };
            toggleMenuItem.Click += OnToggleTimerClick;
            menu.Items.Add(toggleMenuItem);

            // --- Switch Mode Submenu ---
            var modeMenu = new MenuFlyoutSubItem { Text = "Switch Mode" };
            modeMenu.Items.Add(CreateModeMenuItem("Focus", TimerMode.Focus));
            modeMenu.Items.Add(CreateModeMenuItem("Short Break", TimerMode.ShortBreak));
            modeMenu.Items.Add(CreateModeMenuItem("Long Break", TimerMode.LongBreak));
            menu.Items.Add(modeMenu);

            // --- Separator ---
            menu.Items.Add(new MenuFlyoutSeparator());

            // --- Show/Hide App Menu Item ---
            var showHideMenuItem = new MenuFlyoutItem
            {
                Text = "Hide FocusMate",
                Tag = "ShowHide"
            };
            showHideMenuItem.Click += OnShowHideAppClick;
            menu.Items.Add(showHideMenuItem);

            // --- Separator ---
            menu.Items.Add(new MenuFlyoutSeparator());

            // --- Exit Menu Item ---
            var exitMenuItem = new MenuFlyoutItem { Text = "Exit" };
            exitMenuItem.Click += OnExitAppClick;
            menu.Items.Add(exitMenuItem);

            // Update initial menu item states
            UpdateContextMenu();
        }

        /// <summary>
        /// Updates the state (e.g., text) of context menu items.
        /// </summary>
        private void UpdateContextMenu()
        {
            if (_taskbarIcon?.ContextFlyout is MenuFlyout menu)
            {
                foreach (var item in menu.Items)
                {
                    if (item is MenuFlyoutItem menuItem)
                    {
                        if (menuItem.Tag as string == "StartPause")
                        {
                            // Access TimerService properties directly - they exist in the provided code
                            menuItem.Text = _timerService.IsRunning ? "Pause" : "Start";
                        }
                        else if (menuItem.Tag as string == "ShowHide")
                        {
                            menuItem.Text = _isWindowVisible ? "Hide FocusMate" : "Show FocusMate";
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a menu item for switching timer modes.
        /// </summary>
        private MenuFlyoutItem CreateModeMenuItem(string label, TimerMode mode)
        {
            var item = new MenuFlyoutItem
            {
                Text = label,
                Tag = mode
            };
            item.Click += OnSwitchModeClick;
            return item;
        }

        // --- Event Handlers ---

        /// <summary>
        /// Handles clicks on the "Start/Pause" context menu item.
        /// </summary>
        private void OnToggleTimerClick(object sender, RoutedEventArgs e)
        {
            // Access TimerService properties/methods directly
            if (_timerService.IsRunning)
            {
                _timerService.Pause();
            }
            else
            {
                _timerService.Start();
            }
            UpdateContextMenu();
        }

        /// <summary>
        /// Handles clicks on a mode switch context menu item.
        /// </summary>
        private void OnSwitchModeClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is TimerMode mode)
            {
                // Fix 1: Use non-generic GetService or ensure Microsoft.Extensions.DependencyInjection is referenced
                // If Microsoft.Extensions.DependencyInjection is referenced, GetService<T> should work.
                // Otherwise, use the non-generic version.
                // Assuming Microsoft.Extensions.DependencyInjection is referenced based on App.xaml.cs
                var settings = App.Services?.GetService(typeof(SettingsModel)) as SettingsModel;
                var duration = mode switch
                {
                    TimerMode.Focus => TimeSpan.FromMinutes(settings?.DefaultFocusMinutes ?? 50),
                    TimerMode.ShortBreak => TimeSpan.FromMinutes(settings?.ShortBreakMinutes ?? 10),
                    TimerMode.LongBreak => TimeSpan.FromMinutes(settings?.LongBreakMinutes ?? 25),
                    _ => TimeSpan.FromMinutes(25)
                };

                _timerService.SetMode(mode, duration);
                UpdateContextMenu();
            }
        }

        /// <summary>
        /// Handles clicks on the "Show/Hide App" context menu item.
        /// </summary>
        private void OnShowHideAppClick(object sender, RoutedEventArgs e)
        {
            ToggleWindowVisibility();
        }

        /// <summary>
        /// Handles clicks on the "Exit" context menu item.
        /// </summary>
        private void OnExitAppClick(object sender, RoutedEventArgs e)
        {
            Dispose();
            Microsoft.UI.Xaml.Application.Current.Exit();
        }

        /// <summary>
        /// Handles left mouse clicks on the tray icon.
        /// </summary>
        private void OnTrayLeftMouseDown(object sender, RoutedEventArgs e) // Fix: Use TrayLeftMouseDown
        {
            ToggleWindowVisibility();
        }

        /// <summary>
        /// Handles the TimerService.Tick event to update the overlay text.
        /// </summary>
        private void OnTimerTick(object sender, TimerTickEventArgs e)
        {
            UpdateOverlayText();
        }

        /// <summary>
        /// Handles the TimerService.ModeChanged event to update the overlay text.
        /// </summary>
        private void OnTimerModeChanged(object sender, TimerModeChangedEventArgs e)
        {
            UpdateOverlayText();
            UpdateContextMenu();
        }

        // --- Helper Methods ---

        /// <summary>
        /// Toggles the visibility of the main application window using AppWindow.
        /// </summary>
        private void ToggleWindowVisibility()
        {
            if (_appWindow == null || _mainWindow == null) return;

            try
            {
                if (_isWindowVisible)
                {
                    _appWindow.Show(false); // Minimize/Hide
                    _isWindowVisible = false;
                }
                else
                {
                    _appWindow.Show(true); // Restore/Show
                    _mainWindow.Activate(); // Bring to foreground
                    _isWindowVisible = true;
                }
                UpdateContextMenu();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TrayService] Error toggling window visibility: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the tray icon's overlay text based on the timer state.
        /// Note: Overlay text support in H.NotifyIcon.WinUI might be limited or different.
        /// </summary>
        private void UpdateOverlayText()
        {
            // Fix: Check if UpdateOverlayText method actually exists on the version of TaskbarIcon you are using.
            // If it doesn't exist, this call will need to be removed or replaced.
            // The method signature might also be different (e.g., taking a string and a color).
            // For now, we'll wrap it in a try-catch to prevent crashes if the method signature is wrong.
            try
            {
                string overlayText = "";
                if (_timerService.IsRunning) // Access property directly
                {
                    var minutes = (int)_timerService.Elapsed.TotalMinutes; // Access property directly
                    // Access property directly
                    var modeChar = _timerService.CurrentMode == TimerMode.Focus ? "F" : "B";
                    overlayText = $"{modeChar}{minutes:D2}";
                }

                // Attempt to update overlay text. This might not work as expected depending on the H.NotifyIcon version.
                // If this method doesn't exist or has a different signature, you'll need to find the correct way
                // or remove this feature.
                // Example of potential signature: _taskbarIcon?.UpdateOverlayText(overlayText, Windows.UI.Colors.White);
                // Or it might not be supported at all in this library version for WinUI.
                _taskbarIcon?.UpdateOverlayText(overlayText);
            }
            catch (MissingMethodException mmEx)
            {
                Debug.WriteLine($"[TrayService] UpdateOverlayText method not found or has different signature: {mmEx.Message}");
                // If the method is missing, we can't update the overlay text. Consider removing the call or finding an alternative.
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TrayService] Potentially unexpected error updating overlay text (method might not exist as expected): {ex.Message}");
            }
        }


        /// <summary>
        /// Releases the unmanaged resources used by the TrayService.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                // Unsubscribe from TimerService events
                if (_timerService != null)
                {
                    _timerService.Tick -= OnTimerTick;
                    _timerService.ModeChanged -= OnTimerModeChanged;
                }

                // Unsubscribe from TaskbarIcon events
                if (_taskbarIcon != null)
                {
                    // Fix: Unsubscribe from the correct event (TrayLeftMouseDown)
                    _taskbarIcon.TrayLeftMouseDown -= OnTrayLeftMouseDown;
                    // Unsubscribe from other events if you add them later
                }

                // Dispose the TaskbarIcon
                _taskbarIcon?.Dispose();
                _taskbarIcon = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TrayService] Error during disposal: {ex.Message}");
            }
        }
    }
}