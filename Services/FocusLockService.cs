// Services/FocusLockService.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusMate.Helpers;
using FocusMate.Models;
using FocusMate.Services;

namespace FocusMate.Services
{
    public class FocusLockService : IDisposable
    {
        private readonly TimerService _timerService;
        private readonly NotificationService _notificationService;
        private readonly SettingsService _settingsService;
        private readonly SessionService _sessionService;

        private Timer _monitoringTimer;
        private bool _isMonitoring;
        private bool _isDisposed;

        public FocusLockService(
            TimerService timerService,
            NotificationService notificationService,
            SettingsService settingsService,
            SessionService sessionService)
        {
            _timerService = timerService;
            _notificationService = notificationService;
            _settingsService = settingsService;
            _sessionService = sessionService;

            _timerService.TimerStarted += OnTimerStarted;
            _timerService.TimerPaused += OnTimerPaused;
            _timerService.TimerCompleted += OnTimerCompleted;

            // Initial state check
            if (_timerService.IsRunning && _timerService.CurrentMode == TimerMode.Focus)
            {
                StartMonitoring();
            }
        }

        private void OnTimerStarted(object sender, EventArgs e)
        {
            if (_timerService.CurrentMode == TimerMode.Focus &&
                _settingsService.GetSettings().BlockRule.IsEnabled)
            {
                StartMonitoring();
            }
        }

        private void OnTimerPaused(object sender, EventArgs e)
        {
            StopMonitoring();
        }

        private void OnTimerCompleted(object sender, EventArgs e)
        {
            StopMonitoring();
        }

        public void StartMonitoring()
        {
            if (_isMonitoring || _isDisposed) return;

            _isMonitoring = true;
            _monitoringTimer = new Timer(MonitorProcesses, null, 0, 2000);
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring || _isDisposed) return;

            _isMonitoring = false;
            _monitoringTimer?.Dispose();
            _monitoringTimer = null;
        }

        private async void MonitorProcesses(object state)
        {
            if (!_isMonitoring || _isDisposed) return;

            try
            {
                var settings = _settingsService.GetSettings();
                var blockRule = settings.BlockRule;

                if (!blockRule.IsEnabled) return;

                // Only monitor during focus sessions if configured
                if (blockRule.FocusSessionsOnly && _timerService.CurrentMode != TimerMode.Focus)
                    return;

                // Check for blocked applications
                foreach (var appRule in blockRule.Apps.Where(r => r.IsActive))
                {
                    if (ProcessHelper.IsProcessRunning(appRule.ProcessName))
                    {
                        await HandleBlockedApp(appRule);
                    }
                }

                // Note: Website blocking is not implemented in MVP
                // as it requires admin privileges and doesn't work in MSIX
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error monitoring processes: {ex.Message}");
            }
        }

        private async Task HandleBlockedApp(AppBlockRule rule)
        {
            switch (rule.Action)
            {
                case BlockAction.Warn:
                    _notificationService.ShowFocusLockNotification(rule.FriendlyName);
                    break;

                case BlockAction.KillProcess:
                    await ProcessHelper.KillProcessAsync(rule.ProcessName);
                    _notificationService.ShowFocusLockNotification(rule.FriendlyName);
                    break;

                case BlockAction.CloseWindow:
                    // Not implemented in MVP
                    break;

                case BlockAction.BlockNetwork:
                    // Not implemented in MVP
                    break;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            StopMonitoring();

            _timerService.TimerStarted -= OnTimerStarted;
            _timerService.TimerPaused -= OnTimerPaused;
            _timerService.TimerCompleted -= OnTimerCompleted;
        }
    }
}