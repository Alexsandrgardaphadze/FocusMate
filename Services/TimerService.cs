// Services/TimerService.cs
using System;
using System.Diagnostics;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using FocusMate.Models;

namespace FocusMate.Services
{
    public partial class TimerService : ObservableObject, IDisposable
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly Timer _timer = new Timer(1000);
        private TimeSpan _remainingTime;
        private TimeSpan _sessionDuration;

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

        public TimerService()
        {
            _timer.Elapsed += OnTimerElapsed;
        }

        public void Initialize(SettingsModel settings)
        {
            SetMode(TimerMode.Focus, TimeSpan.FromMinutes(settings.DefaultFocusMinutes));
        }

        public void SetMode(TimerMode mode, TimeSpan duration)
        {
            CurrentMode = mode;
            _sessionDuration = duration;
            _remainingTime = duration;
            OnPropertyChanged(nameof(RemainingTimeFormatted));
        }

        public void Start()
        {
            if (!IsRunning)
            {
                _stopwatch.Start();
                _timer.Start();
                IsRunning = true;
                TimerStarted?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Pause()
        {
            if (IsRunning)
            {
                _stopwatch.Stop();
                _timer.Stop();
                IsRunning = false;
                TimerPaused?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Reset()
        {
            _stopwatch.Reset();
            _timer.Stop();
            _remainingTime = _sessionDuration;
            IsRunning = false;
            OnPropertyChanged(nameof(RemainingTimeFormatted));
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            _remainingTime = _sessionDuration - _stopwatch.Elapsed;

            if (_remainingTime.TotalSeconds <= 0)
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
            _timer.Stop();
            _stopwatch.Reset();
            IsRunning = false;
            _remainingTime = TimeSpan.Zero;
            OnPropertyChanged(nameof(RemainingTimeFormatted));
            TimerCompleted?.Invoke(this, EventArgs.Empty);
        }

        public string RemainingTimeFormatted =>
            $"{_remainingTime.Minutes:00}:{_remainingTime.Seconds:00}";

        public void Dispose()
        {
            _timer?.Dispose();
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
}