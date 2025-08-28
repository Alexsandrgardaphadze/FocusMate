// Services/NotificationService.cs
using FocusMate.Models;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace FocusMate.Services
{
    public class NotificationService : INotificationService, IDisposable
    {
        private readonly SettingsModel _settings;
        private readonly DispatcherQueue _dispatcherQueue;
        private bool _isDisposed;
        private bool _isLogoValid;
        private Uri _logoUri;

        public NotificationService(SettingsModel settings, DispatcherQueue dispatcherQueue)
        {
            _settings = settings;
            _dispatcherQueue = dispatcherQueue;
            Initialize();
            ValidateAssetsAsync().FireAndForget();
        }

        private void Initialize()
        {
            try
            {
                AppNotificationManager.Default.SetDisplayName("FocusMate");
                AppNotificationManager.Default.Register();
                AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Notification init failed: {ex.Message}");
            }
        }

        private async Task ValidateAssetsAsync()
        {
            try
            {
                _logoUri = new Uri("ms-appx:///Assets/Logo.png");
                await StorageFile.GetFileFromApplicationUriAsync(_logoUri);
                _isLogoValid = true;
            }
            catch
            {
                _logoUri = new Uri("ms-appx:///Assets/Square44x44Logo.png");
                _isLogoValid = false;
            }
        }

        public void ShowSessionCompleteNotification(TimerMode mode)
        {
            string title = mode switch
            {
                TimerMode.Focus => "Focus session completed!",
                TimerMode.ShortBreak => "Short break completed!",
                TimerMode.LongBreak => "Long break completed!",
                _ => "Session completed!"
            };

            string message = mode switch
            {
                TimerMode.Focus => "Time for a break. You've earned it!",
                TimerMode.ShortBreak => "Ready to focus again?",
                TimerMode.LongBreak => "Refreshed and ready to focus?",
                _ => "Your session has ended."
            };

            var notification = new AppNotificationBuilder()
                .AddText(title)
                .AddText(message)
                .AddButton("Start Next Focus", "action=startFocus")
                .SetAppLogoOverride(_isLogoValid ? _logoUri : null)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
            PlayNotificationSound();
        }

        public void ShowFocusLockNotification(string appName)
        {
            var notification = new AppNotificationBuilder()
                .AddText("Focus Lock Activated")
                .AddText($"Blocked access to {appName} during focus session")
                .SetAppLogoOverride(_isLogoValid ? _logoUri : null)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }

        private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                // CORRECTED: Parse arguments properly
                var arguments = ParseArguments(args.Argument);

                // Handle action buttons
                if (arguments.TryGetValue("action", out var action) && action == "startFocus")
                {
                    App.TimerService?.SetMode(TimerMode.Focus, TimeSpan.FromMinutes(50));
                    App.TimerService?.Start();
                }

                // Always activate main window
                App.MainWindow?.Activate();
            });
        }

        // CORRECTED: Proper argument parsing
        private Dictionary<string, string> ParseArguments(string argumentString)
        {
            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(argumentString))
                return result;

            // Handle both "key=value" and "key1=value1&key2=value2" formats
            foreach (var part in argumentString.Split('&'))
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    result[keyValue[0]] = WebUtility.UrlDecode(keyValue[1]);
                }
            }

            return result;
        }

        private void PlayNotificationSound()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                var player = new MediaPlayer();
                player.MediaEnded += (s, e) => player.Dispose();

                try
                {
                    if (_settings.UseWindowsNotificationSound)
                    {
                        player.Source = MediaSource.CreateFromUri(
                            new Uri("ms-winsoundevent:Notification.Default"));
                    }
                    else if (!string.IsNullOrEmpty(_settings.CustomSoundPath))
                    {
                        // CORRECTED: Validate file existence first
                        if (File.Exists(_settings.CustomSoundPath))
                        {
                            player.Source = MediaSource.CreateFromUri(
                                new Uri(_settings.CustomSoundPath));
                        }
                        else
                        {
                            // File doesn't exist - fallback to system sound
                            throw new FileNotFoundException("Custom sound file not found",
                                _settings.CustomSoundPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Sound playback error: {ex.Message}");
                    player.Source = MediaSource.CreateFromUri(
                        new Uri("ms-winsoundevent:Notification.Default"));
                }

                player.Play();
            });
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            // CORRECTED: Unsubscribe before unregistering
            AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
            AppNotificationManager.Default.Unregister();
        }
    }

    // For testability and future-proofing
    public interface INotificationService
    {
        void ShowSessionCompleteNotification(TimerMode mode);
        void ShowFocusLockNotification(string appName);
    }

    // Helper for fire-and-forget async operations
    internal static class TaskExtensions
    {
        public static void FireAndForget(this Task task)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    System.Diagnostics.Debug.WriteLine($"Task failed: {t.Exception?.Message}");
                }
            }, TaskScheduler.Default);
        }
    }
}