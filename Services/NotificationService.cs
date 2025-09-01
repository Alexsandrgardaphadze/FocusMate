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
        // IMPROVED: Add continuation for error logging on the asset validation task
        private Task _validateAssetsTask; // Async asset validation

        public NotificationService(SettingsModel settings, DispatcherQueue dispatcherQueue)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
            Initialize();
            // IMPROVED: Add continuation for unobserved exceptions
            _validateAssetsTask = ValidateAssetsAsync()
                .ContinueWith(
                    t => System.Diagnostics.Debug.WriteLine($"Notification asset validation failed: {t.Exception?.InnerException?.Message}"),
                    TaskContinuationOptions.OnlyOnFaulted
                );
        }

        /// <summary>
        /// Initializes the notification manager and subscribes to activation events.
        /// This should be called once during application startup.
        /// </summary>
        private void Initialize()
        {
            try
            {
                // Register the app to receive notifications.
                // This should ideally be done once, typically during app startup.
                // Unregistering should only happen when the app shuts down.
                AppNotificationManager.Default.Register();

                // Subscribe to the event that fires when a notification is clicked/activated.
                AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Notification service initialization failed: {ex.Message}");
                // Depending on requirements, you might want to handle this more gracefully,
                // perhaps by disabling notifications or showing a user-friendly message.
            }
        }

        /// <summary>
        /// Asynchronously validates and loads the logo assets for notifications.
        /// </summary>
        private async Task ValidateAssetsAsync()
        {
            try
            {
                _logoUri = new Uri("ms-appx:///Assets/Logo.png");
                await StorageFile.GetFileFromApplicationUriAsync(_logoUri);
            }
            catch
            {
                // Try fallback logo if the primary one fails
                try
                {
                    _logoUri = new Uri("ms-appx:///Assets/Square44x44Logo.png");
                    await StorageFile.GetFileFromApplicationUriAsync(_logoUri);
                }
                catch (Exception fallbackEx)
                {
                    // Both logos failed - set to null to indicate no valid logo.
                    // The AppNotificationBuilder will handle a null Uri gracefully.
                    _logoUri = null;
                    System.Diagnostics.Debug.WriteLine($"Notification logo validation failed: {fallbackEx.Message}");
                }
            }
        }

        /// <summary>
        /// Sends a session completion notification with an action button.
        /// </summary>
        /// <param name="mode">The timer mode that just completed.</param>
        /// <returns>The ID of the sent notification, or 0 if sending failed.</returns>
        public async Task<uint> ShowSessionCompleteNotification(TimerMode mode)
        {
            try
            {
                // Ensure asset validation is complete before building the notification
                await _validateAssetsTask;

                string title = mode switch
                {
                    TimerMode.Focus => "Focus Session Completed!",
                    TimerMode.ShortBreak => "Short Break Completed!",
                    TimerMode.LongBreak => "Long Break Completed!",
                    _ => "Session Completed!"
                };

                string message = mode switch
                {
                    TimerMode.Focus => "Time for a break. You've earned it!",
                    TimerMode.ShortBreak => "Ready to focus again?",
                    TimerMode.LongBreak => "Refreshed and ready to focus?",
                    _ => "Your session has ended."
                };

                // Build the notification using AppNotificationBuilder
                var builder = new AppNotificationBuilder()
                    .AddText(title)
                    .AddText(message)
                    // Add an action button to start the next focus session
                    .AddButton(new AppNotificationButton("Start Next Focus")
                        .AddArgument("action", "startFocus"))
                    // Add a text input box for quick notes
                    .AddTextBox("quickNote", "Quick Note", "Add a note...")
                    // Set the notification duration
                    .SetDuration(AppNotificationDuration.Default);

                // Conditionally set the logo if it was successfully loaded
                if (_logoUri != null)
                {
                    builder.SetAppLogoOverride(_logoUri);
                }

                var notification = builder.BuildNotification();

                // Show the notification using the default notification manager
                AppNotificationManager.Default.Show(notification);

                // Play the configured notification sound
                PlayNotificationSound();

                // IMPROVED: Return the notification ID for potential future tracking
                return notification.Id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show session complete notification: {ex.Message}");
                return 0; // Return 0 to indicate failure
            }
        }

        /// <summary>
        /// Sends a focus lock notification when a distracting app is blocked.
        /// </summary>
        /// <param name="appName">The name of the blocked application.</param>
        /// <returns>The ID of the sent notification, or 0 if sending failed.</returns>
        public async Task<uint> ShowFocusLockNotification(string appName)
        {
            try
            {
                // Ensure asset validation is complete
                await _validateAssetsTask;

                var builder = new AppNotificationBuilder()
                    .AddText("Focus Lock Activated")
                    .AddText($"Blocked access to {appName} during focus session.")
                    .SetDuration(AppNotificationDuration.Default); // Shorter duration for lock alerts

                // Conditionally set the logo
                if (_logoUri != null)
                {
                    builder.SetAppLogoOverride(_logoUri);
                }

                var notification = builder.BuildNotification();
                AppNotificationManager.Default.Show(notification);

                // Return the notification ID for potential future tracking
                return notification.Id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show focus lock notification: {ex.Message}");
                return 0; // Return 0 to indicate failure
            }
        }

        /// <summary>
        /// Handles the event when a notification is clicked or activated by the user.
        /// </summary>
        /// <param name="sender">The AppNotificationManager instance.</param>
        /// <param name="args">Event arguments containing activation details.</param>
        private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
        {
            // Ensure UI-related actions are performed on the UI thread
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    // Parse the arguments passed by the notification action
                    var parsedArgs = ParseArguments(args.Argument);

                    // Handle specific actions based on the 'action' argument
                    if (parsedArgs.TryGetValue("action", out string action))
                    {
                        switch (action)
                        {
                            case "startFocus":
                                // Trigger the start of a new focus session
                                StartNextFocusSession();
                                break;
                            case "dismiss":
                                // Optionally handle dismiss action if added
                                // For now, just activate the window
                                break;
                            default:
                                // Handle any other potential actions
                                System.Diagnostics.Debug.WriteLine($"Unknown notification action: {action}");
                                break;
                        }
                    }

                    // Handle text input from the notification (e.g., quick note)
                    if (args.UserInput.TryGetValue("quickNote", out string note) && !string.IsNullOrWhiteSpace(note))
                    {
                        SaveQuickNote(note);
                    }

                    // Always bring the main application window to the foreground
                    ActivateMainWindow();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error handling notification activation: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Starts the next focus session based on settings.
        /// </summary>
        private void StartNextFocusSession()
        {
            try
            {
                // Resolve TimerService from the global service provider
                var timerService = App.Services?.GetService(typeof(TimerService)) as TimerService;
                if (timerService != null)
                {
                    // Get settings to determine the correct focus duration
                    var settings = App.Services?.GetService(typeof(SettingsModel)) as SettingsModel;
                    int focusMinutes = settings?.DefaultFocusMinutes ?? 50; // Fallback to 50 mins

                    // Configure and start the timer for a new focus session
                    timerService.SetMode(TimerMode.Focus, TimeSpan.FromMinutes(focusMinutes));
                    timerService.Start();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting next focus session from notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves a quick note entered via the notification input.
        /// </summary>
        /// <param name="note">The text of the note.</param>
        private void SaveQuickNote(string note)
        {
            try
            {
                // Example: Append the note to a text file
                var notesFile = Path.Combine(ApplicationData.Current.LocalFolder.Path, "QuickNotes.txt");
                var noteEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {note}{Environment.NewLine}";
                File.AppendAllText(notesFile, noteEntry);

                System.Diagnostics.Debug.WriteLine($"Quick note saved from notification: {note}");
                // Future enhancement: Save to current session or a dedicated notes service
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save quick note from notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Activates the main application window.
        /// </summary>
        private void ActivateMainWindow()
        {
            try
            {
                // Safely get the main window instance and activate it
                var app = App.Current as App;
                app?.MainWindow?.Activate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error activating main window from notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses the query-string-like arguments from a notification activation.
        /// </summary>
        /// <param name="argumentString">The raw argument string (e.g., "action=startFocus&param=value").</param>
        /// <returns>A dictionary of key-value pairs.</returns>
        private Dictionary<string, string> ParseArguments(string argumentString)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(argumentString))
                return result;

            foreach (var part in argumentString.Split('&'))
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    // URL decode the value in case it contains special characters
                    result[keyValue[0]] = WebUtility.UrlDecode(keyValue[1]);
                }
            }
            return result;
        }

        /// <summary>
        /// Plays the configured notification sound.
        /// </summary>
        private void PlayNotificationSound()
        {
            // Ensure MediaPlayer operations happen on the UI thread dispatcher
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    // Create a new MediaPlayer instance for this sound play
                    _mediaPlayer = new MediaPlayer();

                    // Set up disposal when playback ends to free resources
                    _mediaPlayer.MediaEnded += (s, e) =>
                    {
                        _mediaPlayer?.Dispose();
                        _mediaPlayer = null;
                    };

                    // Determine the sound source based on settings
                    if (_settings.UseWindowsNotificationSound)
                    {
                        _mediaPlayer.Source = MediaSource.CreateFromUri(
                            new Uri("ms-winsoundevent:Notification.Default"));
                    }
                    else if (!string.IsNullOrEmpty(_settings.CustomSoundPath))
                    {
                        // IMPROVED: Add validation for URI format
                        try
                        {
                            // Validate file existence first
                            if (File.Exists(_settings.CustomSoundPath))
                            {
                                // Test URI creation to catch format issues
                                var testUri = new Uri(_settings.CustomSoundPath);
                                _mediaPlayer.Source = MediaSource.CreateFromUri(testUri);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"Custom sound file not found: {_settings.CustomSoundPath}");
                                // Fallback to system default if custom path is invalid
                                _mediaPlayer.Source = MediaSource.CreateFromUri(
                                    new Uri("ms-winsoundevent:Notification.Default"));
                            }
                        }
                        catch (UriFormatException uriEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Invalid URI format for custom sound: {uriEx.Message}");
                            // Fallback to system default if URI is malformed
                            _mediaPlayer.Source = MediaSource.CreateFromUri(
                                new Uri("ms-winsoundevent:Notification.Default"));
                        }
                        catch (Exception fileEx) // Catch other potential file access issues
                        {
                            System.Diagnostics.Debug.WriteLine($"Error accessing custom sound file: {fileEx.Message}");
                            // Fallback to system default if file access fails
                            _mediaPlayer.Source = MediaSource.CreateFromUri(
                                new Uri("ms-winsoundevent:Notification.Default"));
                        }
                    }
                    else
                    {
                        // Fallback to system default if custom path is empty/null
                        _mediaPlayer.Source = MediaSource.CreateFromUri(
                            new Uri("ms-winsoundevent:Notification.Default"));
                    }

                    // Start playing the sound
                    _mediaPlayer.Play();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error playing notification sound: {ex.Message}");
                    // Ensure cleanup even if playback fails
                    _mediaPlayer?.Dispose();
                    _mediaPlayer = null;
                }
            });
        }

        /// <summary>
        /// Unsubscribes from events and cleans up resources.
        /// IMPORTANT: For AppNotificationManager, you typically should NOT call Unregister
        /// unless the app is truly shutting down, as it can disable all notifications for the app.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                // Unsubscribe from the notification activation event to prevent leaks
                AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;

                // DO NOT call AppNotificationManager.Default.Unregister() here.
                // Unregistering stops the app from receiving *any* notifications,
                // even from external sources like scheduled tasks.
                // It should only be called when the app process exits.
                // AppNotificationManager.Default.Unregister();

                // Dispose the MediaPlayer if it's still active
                _mediaPlayer?.Dispose();
                _mediaPlayer = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during NotificationService disposal: {ex.Message}");
            }
        }
    }

    // Interface for testability and abstraction
    public interface INotificationService
    {
        // IMPROVED: Return notification ID
        Task<uint> ShowSessionCompleteNotification(TimerMode mode);
        Task<uint> ShowFocusLockNotification(string appName);
    }
}