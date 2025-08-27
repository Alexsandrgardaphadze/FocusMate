// ViewModels/TimerViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusMate.Models;
using FocusMate.Services;
using System;
using System.Threading.Tasks;

namespace FocusMate.ViewModels
{
    public partial class TimerViewModel : ObservableObject
    {
        private readonly TimerService _timerService;
        private readonly SessionService _sessionService;
        private readonly SettingsModel _settings;

        [ObservableProperty]
        private string _remainingTime = "25:00";

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private string _sessionLabel = string.Empty;

        [ObservableProperty]
        private string _selectedCategory = string.Empty;

        public TimerViewModel(TimerService timerService, SessionService sessionService, SettingsModel settings)
        {
            _timerService = timerService;
            _sessionService = sessionService;
            _settings = settings;

            _timerService.Tick += OnTimerTick;
            _timerService.TimerStarted += OnTimerStarted;
            _timerService.TimerPaused += OnTimerPaused;
            _timerService.TimerCompleted += OnTimerCompleted;

            _timerService.Initialize(_settings);
        }

        private void OnTimerTick(object? sender, TimerTickEventArgs e)
        {
            RemainingTime = _timerService.RemainingTimeFormatted;
        }

        private void OnTimerStarted(object? sender, EventArgs e)
        {
            IsRunning = true;
        }

        private void OnTimerPaused(object? sender, EventArgs e)
        {
            IsRunning = false;
        }

        private async void OnTimerCompleted(object? sender, EventArgs e)
        {
            IsRunning = false;

            // Save the completed session
            var session = new Session
            {
                EndUtc = DateTimeOffset.UtcNow,
                DurationMinutes = (int)_settings.DefaultFocusMinutes,
                Label = SessionLabel,
                Category = SelectedCategory,
                WasInterrupted = false,
                Mode = _timerService.CurrentMode
            };

            await _sessionService.SaveSessionAsync(session);

            // Auto-start next session if enabled
            if (_settings.AutoStartNext)
            {
                // Switch to next mode and start
                SwitchToNextMode();
                StartTimer();
            }
        }

        [RelayCommand]
        private void StartTimer()
        {
            _timerService.Start();
        }

        [RelayCommand]
        private void PauseTimer()
        {
            _timerService.Pause();
        }

        [RelayCommand]
        private void ResetTimer()
        {
            _timerService.Reset();
            RemainingTime = _timerService.RemainingTimeFormatted;
        }

        [RelayCommand]
        private void SwitchToFocusMode()
        {
            _timerService.SetMode(TimerMode.Focus, TimeSpan.FromMinutes(_settings.DefaultFocusMinutes));
            RemainingTime = _timerService.RemainingTimeFormatted;
        }

        [RelayCommand]
        private void SwitchToShortBreak()
        {
            _timerService.SetMode(TimerMode.ShortBreak, TimeSpan.FromMinutes(_settings.ShortBreakMinutes));
            RemainingTime = _timerService.RemainingTimeFormatted;
        }

        [RelayCommand]
        private void SwitchToLongBreak()
        {
            _timerService.SetMode(TimerMode.LongBreak, TimeSpan.FromMinutes(_settings.LongBreakMinutes));
            RemainingTime = _timerService.RemainingTimeFormatted;
        }

        private void SwitchToNextMode()
        {
            // Logic to determine next mode based on current mode and settings
            switch (_timerService.CurrentMode)
            {
                case TimerMode.Focus:
                    SwitchToShortBreak();
                    break;
                case TimerMode.ShortBreak:
                case TimerMode.LongBreak:
                    SwitchToFocusMode();
                    break;
            }
        }
    }
}