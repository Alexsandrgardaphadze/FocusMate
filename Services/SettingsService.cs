// Services/SettingsService.cs
using FocusMate.Models;
using System;
using System.Threading.Tasks;

namespace FocusMate.Services
{
    /// <summary>
    /// Manages application settings, providing access and updates.
    /// Acts as a central point for settings-related logic.
    /// </summary>
    public class SettingsService
    {
        private readonly StorageService _storageService;
        private SettingsModel _settings;

        /// <summary>
        /// Event raised when settings are updated.
        /// </summary>
        public event Action<SettingsModel> SettingsChanged;

        /// <summary>
        /// Initializes a new instance of the SettingsService.
        /// </summary>
        /// <param name="storageService">The storage service used to load/save settings.</param>
        public SettingsService(StorageService storageService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            // Initialize with default settings until loaded
            _settings = new SettingsModel();
        }

        /// <summary>
        /// Asynchronously loads settings from storage.
        /// This should be called during application startup.
        /// </summary>
        public async Task InitializeAsync()
        {
            var loadedSettings = await _storageService.LoadSettingsAsync();
            if (loadedSettings != null)
            {
                _settings = loadedSettings;
            }
            // If load fails or returns null, _settings remains the default instance created in the constructor.
        }

        /// <summary>
        /// Gets the current application settings.
        /// </summary>
        /// <returns>The current SettingsModel instance.</returns>
        public SettingsModel GetSettings() => _settings;

        /// <summary>
        /// Asynchronously updates the application settings.
        /// This saves the settings to storage and notifies listeners of the change.
        /// </summary>
        /// <param name="updateAction">An action that modifies the provided SettingsModel instance.</param>
        public async Task UpdateSettingsAsync(Action<SettingsModel> updateAction)
        {
            if (updateAction == null) throw new ArgumentNullException(nameof(updateAction));

            updateAction(_settings);
            await _storageService.SaveSettingsAsync(_settings);
            SettingsChanged?.Invoke(_settings);
        }
    }
}