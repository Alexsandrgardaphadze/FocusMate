// Services/NotificationService.cs
using FocusMate.Models;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.IO;
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
        private Uri _logoUri;
        private MediaPlayer _mediaPlayer; // Keep reference to prevent GC
        private Task _validateAssetsTask; // Async asset validation

        public NotificationService(SettingsModel settings, DispatcherQueue dispatcherQueue)
        {
            _settings = settings;
            _dispatcherQueue = dispatcherQueue;
            Initialize();
            _validateAssetsTask = ValidateAssetsAsync(); // Start validation
        }

        private void Initialize()
        {
            try
            {
                // Register at app level - don't dispose unless app shuts down
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
            }
            catch
            {
                // Try fallback logo
                try
                {
                    _logoUri = new Uri("ms-appx:///Assets/Square44x44Logo.png");
                    await StorageFile.GetFileFromApplicationUriAsync(_logoUri);
                }
                catch (Exception fallbackEx)
                {
                    // Both logos failed - set to null to avoid invalid URIs
                    _logoUri = null;
                    System.Diagnostics.Debug.WriteLine($"Both logo files failed to load: {fallbackEx.Message}");
                }
            }
        }

        // FIXED: Changed from async void to async Task
        public async Task ShowSessionCompleteNotification(TimerMode mode)
        {
            try
            {
                // Wait for asset validation if needed
                await _validateAssetsTask;

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

                var builder = new AppNotificationBuilder()
                    .AddText(title)
                    .AddText(message)
                    .AddButton(new AppNotificationButton("Start Next Focus")
                        .AddArgument("action", "startFocus"))
                    .SetDuration(AppNotificationDuration.Default)
                    .AddTextBox("quickNote", "Quick Note", "Add quick note...");

                // Only set logo if it exists
                if (_logoUri != null)
                {
                    builder.SetAppLogoOverride(_logoUri);
                }

                var notification = builder.BuildNotification();
                AppNotificationManager.Default.Show(notification);
                PlayNotificationSound();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show notification: {ex.Message}");
            }
        }

        // FIXED: Changed from async void to async Task
        public async Task ShowFocusLockNotification(string appName)
        {
            try
            {
                // Wait for asset validation if needed
                await _validateAssetsTask;

                var builder = new AppNotificationBuilder()
                    .AddText("Focus Lock Activated")
                    .AddText($"Blocked access to {appName} during focus session")
                    .SetDuration(AppNotificationDuration.Default);

                // Only set logo if it exists
                if (_logoUri != null)
                {
                    builder.SetAppLogoOverride(_logoUri);
                }

                var notification = builder.BuildNotification();
                AppNotificationManager.Default.Show(notification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show focus lock notification: {ex.Message}");
            }
        }

        private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    // Parse arguments with better structure
                    var action = ParseArguments(args.Argument);

                    // Handle action buttons
                    if (action.Name == "startFocus")
                    {
                        // FIXED: Access TimerService through static Services property
                        if (App.Services != null)
                        {
                            var timerService = App.Services.GetService(typeof(TimerService)) as TimerService;
                            if (timerService != null)
                            {
                                timerService.SetMode(TimerMode.Focus, TimeSpan.FromMinutes(50));
                                timerService.Start();
                            }
                        }
                    }

                    // Handle quick note input
                    if (args.UserInput.TryGetValue("quickNote", out var note) && !string.IsNullOrWhiteSpace(note))
                    {
                        // Save note to session or storage
                        SaveQuickNote(note);
                    }

                    // FIXED: Safer window activation using proper cast
                    var app = App.Current as App;
                    app?.MainWindow?.Activate();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error handling notification: {ex.Message}");
                }
            });
        }

        private void SaveQuickNote(string note)
        {
            try
            {
                // IMPROVED: Persist quick notes to file
                var notesFile = Path.Combine(
                    ApplicationData.Current.LocalFolder.Path,
                    "QuickNotes.txt");

                var noteEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {note}\n";
                File.AppendAllText(notesFile, noteEntry);

                System.Diagnostics.Debug.WriteLine($"Quick note saved: {note}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save quick note: {ex.Message}");
            }
        }

        // Better strong-typed argument parsing
        private NotificationAction ParseArguments(string argumentString)
        {
            var dict = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(argumentString))
            {
                foreach (var part in argumentString.Split('&'))
                {
                    var keyValue = part.Split('=');
                    if (keyValue.Length == 2)
                    {
                        dict[keyValue[0]] = WebUtility.UrlDecode(keyValue[1]);
                    }
                }
            }

            var actionName = dict.TryGetValue("action", out var action) ? action : "";
            return new NotificationAction(actionName, dict);
        }

        private void PlayNotificationSound()
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    // Keep MediaPlayer reference to prevent GC
                    _mediaPlayer = new MediaPlayer();
                    _mediaPlayer.MediaEnded += (s, e) =>
                    {
                        _mediaPlayer?.Dispose();
                        _mediaPlayer = null;
                    };

                    if (_settings.UseWindowsNotificationSound)
                    {
                        _mediaPlayer.Source = MediaSource.CreateFromUri(
                            new Uri("ms-winsoundevent:Notification.Default"));
                    }
                    else if (!string.IsNullOrEmpty(_settings.CustomSoundPath))
                    {
                        // Validate file existence first
                        if (File.Exists(_settings.CustomSoundPath))
                        {
                            _mediaPlayer.Source = MediaSource.CreateFromUri(
                                new Uri(_settings.CustomSoundPath));
                        }
                        else
                        {
                            // File doesn't exist - fallback to system sound
                            _mediaPlayer.Source = MediaSource.CreateFromUri(
                                new Uri("ms-winsoundevent:Notification.Default"));
                        }
                    }

                    _mediaPlayer.Play();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Sound playback error: {ex.Message}");
                    _mediaPlayer?.Dispose();
                    _mediaPlayer = null;
                }
            });
        }

        // DON'T dispose notification registration - keep it at app level
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            // Only unsubscribe from events, don't unregister
            AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;

            // Dispose media player if still active
            _mediaPlayer?.Dispose();
            _mediaPlayer = null;

            // Cancel validation task if still running
            if (_validateAssetsTask?.IsCompleted == false)
            {
                // Note: We can't cancel the task, but we can ignore its result
            }
        }
    }

    // For testability and future-proofing
    public interface INotificationService
    {
        Task ShowSessionCompleteNotification(TimerMode mode);
        Task ShowFocusLockNotification(string appName);
    }

    // Better strong-typed arguments
    public record NotificationAction(string Name, Dictionary<string, string> Parameters);
}