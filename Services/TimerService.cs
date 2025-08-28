// Services/TimerService.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusMate.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;

namespace FocusMate.Services
{
    public partial class TimerService : ObservableObject, IDisposable
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly DispatcherTimer _uiTimer;
        private TimeSpan _sessionDuration;
        private TimeSpan _remainingTime;
        private bool _isDisposing;

        [ObservableProperty]
        private TimerMode _currentMode = TimerMode.Focus;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private string _currentLabel = string.Empty;

        [ObservableProperty]
        private string _currentCategory = string.Empty;

        public event EventHandler<TimerTickEventArgs>? Tick;
        public event EventHandler? TimerStarted;
        public event EventHandler? TimerPaused;
        public event EventHandler? TimerCompleted;
        public event EventHandler<TimerModeChangedEventArgs>? ModeChanged;

        public TimerService()
        {
            // Use DispatcherTimer for UI-safe updates
            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _uiTimer.Tick += OnTimerTick;
        }

        public void Initialize(SettingsModel settings)
        {
            SetMode(TimerMode.Focus, TimeSpan.FromMinutes(settings.DefaultFocusMinutes));
        }

        public void SetMode(TimerMode mode, TimeSpan duration)
        {
            var previousMode = _currentMode;
            _currentMode = mode;
            _sessionDuration = duration;
            _remainingTime = duration;

            ModeChanged?.Invoke(this, new TimerModeChangedEventArgs(previousMode, mode));
            OnPropertyChanged(nameof(RemainingTimeFormatted));
        }

        public void Start()
        {
            if (!IsRunning)
            {
                _stopwatch.Start();
                _uiTimer.Start();
                IsRunning = true;
                TimerStarted?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Pause()
        {
            if (IsRunning)
            {
                _stopwatch.Stop();
                _uiTimer.Stop();
                IsRunning = false;
                TimerPaused?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Reset()
        {
            _stopwatch.Reset();
            _uiTimer.Stop();
            _remainingTime = _sessionDuration;
            IsRunning = false;
            OnPropertyChanged(nameof(RemainingTimeFormatted));
        }

        private void OnTimerTick(object? sender, object e)
        {
            var elapsed = _stopwatch.Elapsed;
            _remainingTime = _sessionDuration - elapsed;

            if (_remainingTime <= TimeSpan.Zero)
            {
                CompleteTimer();
            }
            else
            {
                Tick?.Invoke(this, new TimerTickEventArgs(_remainingTime));
                OnPropertyChanged(nameof(RemainingTimeFormatted));
            }
        }

        private void CompleteTimer()
        {
            _uiTimer.Stop();
            _stopwatch.Reset();
            IsRunning = false;
            _remainingTime = TimeSpan.Zero;
            OnPropertyChanged(nameof(RemainingTimeFormatted));
            TimerCompleted?.Invoke(this, EventArgs.Empty);
        }

        public string RemainingTimeFormatted =>
            _remainingTime.Hours > 0
                ? $"{_remainingTime.Hours:00}:{_remainingTime.Minutes:00}:{_remainingTime.Seconds:00}"
                : $"{_remainingTime.Minutes:00}:{_remainingTime.Seconds:00}";

        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public void Dispose()
        {
            if (_isDisposing) return;
            _isDisposing = true;

            _uiTimer?.Stop();
            _uiTimer?.Tick -= OnTimerTick;
            _stopwatch?.Stop();
        }
    }

    public class TimerTickEventArgs : EventArgs
    {
        public TimeSpan RemainingTime { get; }

        public TimerTickEventArgs(TimeSpan remainingTime)
        {
            RemainingTime = remainingTime;
        }
    }

    public class TimerModeChangedEventArgs : EventArgs
    {
        public TimerMode PreviousMode { get; }
        public TimerMode NewMode { get; }

        public TimerModeChangedEventArgs(TimerMode previousMode, TimerMode newMode)
        {
            PreviousMode = previousMode;
            NewMode = newMode;
        }
    }
}