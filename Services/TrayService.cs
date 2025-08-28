// Services/TrayService.cs
using FocusMate.Models;
using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace FocusMate.Services
{
    public class TrayService : IDisposable
    {
        private TaskbarIcon _taskbarIcon;
        private Window _mainWindow;
        private TimerService _timerService;
        private bool _isWindowVisible;
        private bool _isDisposed;

        public TrayService(TimerService timerService)
        {
            _timerService = timerService;
        }

        public void Initialize(Window mainWindow)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(TrayService));

            _mainWindow = mainWindow;
            _isWindowVisible = true;

            _taskbarIcon = new TaskbarIcon
            {
                IconSource = new Uri("ms-appx:///Assets/Icons/tray.ico"),
                ToolTipText = "FocusMate - Ready",
                ContextMenu = new ContextMenu()
            };

            SetupContextMenu();

            _taskbarIcon.TrayLeftMouseDown += OnTrayLeftMouseDown;
            _timerService.Tick += OnTimerTick;
            _timerService.ModeChanged += OnTimerModeChanged;

            // Initial state
            UpdateOverlayText();
        }

        private void SetupContextMenu()
        {
            var menu = _taskbarIcon.ContextMenu;

            // Start/Pause button
            var toggleButton = new MenuItem
            {
                Header = "Start",
                Tag = "StartPause"
            };
            toggleButton.Click += OnToggleTimer;
            menu.Items.Add(toggleButton);

            // Mode selection
            var modeMenu = new MenuItem { Header = "Switch Mode" };
            modeMenu.Items.Add(CreateModeMenuItem("Focus", TimerMode.Focus));
            modeMenu.Items.Add(CreateModeMenuItem("Short Break", TimerMode.ShortBreak));
            modeMenu.Items.Add(CreateModeMenuItem("Long Break", TimerMode.LongBreak));
            menu.Items.Add(modeMenu);

            menu.Items.Add(new Separator());

            // Show/Hide app
            var showHideItem = new MenuItem
            {
                Header = "Hide FocusMate",
                Tag = "ShowHide"
            };
            showHideItem.Click += OnShowHideApp;
            menu.Items.Add(showHideItem);

            menu.Items.Add(new Separator());

            // Exit
            var exitItem = new MenuItem { Header = "Exit" };
            exitItem.Click += OnExitApp;
            menu.Items.Add(exitItem);

            UpdateContextMenu();
        }

        private void UpdateContextMenu()
        {
            foreach (var item in _taskbarIcon.ContextMenu.Items)
            {
                if (item is MenuItem menuItem)
                {
                    if (menuItem.Tag as string == "StartPause")
                    {
                        menuItem.Header = _timerService.IsRunning ? "Pause" : "Start";
                    }
                    else if (menuItem.Tag as string == "ShowHide")
                    {
                        menuItem.Header = _isWindowVisible ? "Hide FocusMate" : "Show FocusMate";
                    }
                }
            }
        }

        private MenuItem CreateModeMenuItem(string label, TimerMode mode)
        {
            var item = new MenuItem
            {
                Header = label,
                Tag = mode
            };
            item.Click += OnSwitchMode;
            return item;
        }

        private void OnTrayLeftMouseDown(object sender, EventArgs e)
        {
            ToggleWindowVisibility();
        }

        private void ToggleWindowVisibility()
        {
            if (_isWindowVisible)
            {
                _mainWindow.Hide();
                _isWindowVisible = false;
            }
            else
            {
                _mainWindow.Show();
                _mainWindow.Activate();
                _isWindowVisible = true;
            }
            UpdateContextMenu();
        }

        private void OnTimerTick(object sender, TimerTickEventArgs e)
        {
            UpdateOverlayText();
        }

        private void OnTimerModeChanged(object sender, TimerModeChangedEventArgs e)
        {
            UpdateOverlayText();
        }

        private void UpdateOverlayText()
        {
            if (_timerService.IsRunning)
            {
                var minutes = (int)_timerService.Elapsed.TotalMinutes;
                var modeChar = _timerService.CurrentMode == TimerMode.Focus ? "F" : "B";
                _taskbarIcon.UpdateOverlayText($"{modeChar}{minutes}");
            }
            else
            {
                _taskbarIcon.UpdateOverlayText(string.Empty);
            }
        }

        private void OnToggleTimer(object sender, RoutedEventArgs e)
        {
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

        private void OnSwitchMode(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is TimerMode mode)
            {
                var duration = mode switch
                {
                    TimerMode.Focus => TimeSpan.FromMinutes(50),
                    TimerMode.ShortBreak => TimeSpan.FromMinutes(10),
                    TimerMode.LongBreak => TimeSpan.FromMinutes(25),
                    _ => TimeSpan.FromMinutes(25)
                };

                _timerService.SetMode(mode, duration);
                UpdateContextMenu();
            }
        }

        private void OnShowHideApp(object sender, RoutedEventArgs e)
        {
            ToggleWindowVisibility();
        }

        private void OnExitApp(object sender, RoutedEventArgs e)
        {
            Dispose();
            Application.Current.Exit();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _timerService.Tick -= OnTimerTick;
            _timerService.ModeChanged -= OnTimerModeChanged;

            _taskbarIcon?.Dispose();
            _taskbarIcon = null;
        }
    }
}