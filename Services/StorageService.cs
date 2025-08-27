// Services/StorageService.cs
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using FocusMate.Models;

namespace FocusMate.Services
{
    public class StorageService
    {
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private StorageFolder _localFolder;

        public StorageService()
        {
            _localFolder = ApplicationData.Current.LocalFolder;
        }

        public async Task InitializeAsync()
        {
            // Create app directory if it doesn't exist
            try
            {
                await _localFolder.CreateFolderAsync("FocusMate", CreationCollisionOption.OpenIfExists);
            }
            catch (Exception ex)
            {
                // Handle exception
                Console.WriteLine($"Error creating directory: {ex.Message}");
            }
        }

        public async Task SaveSessionsAsync(Session[] sessions)
        {
            try
            {
                var file = await _localFolder.CreateFileAsync("sessions.json", CreationCollisionOption.ReplaceExisting);
                var json = JsonSerializer.Serialize(sessions, _jsonOptions);
                await FileIO.WriteTextAsync(file, json);
            }
            catch (Exception ex)
            {
                // Handle exception
                Console.WriteLine($"Error saving sessions: {ex.Message}");
            }
        }

        public async Task<Session[]> LoadSessionsAsync()
        {
            try
            {
                var file = await _localFolder.GetFileAsync("sessions.json");
                var json = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize<Session[]>(json) ?? Array.Empty<Session>();
            }
            catch (FileNotFoundException)
            {
                return Array.Empty<Session>();
            }
            catch (Exception ex)
            {
                // Handle exception
                Console.WriteLine($"Error loading sessions: {ex.Message}");
                return Array.Empty<Session>();
            }
        }

        public async Task SaveSettingsAsync(SettingsModel settings)
        {
            try
            {
                var file = await _localFolder.CreateFileAsync("settings.json", CreationCollisionOption.ReplaceExisting);
                var json = JsonSerializer.Serialize(settings, _jsonOptions);
                await FileIO.WriteTextAsync(file, json);
            }
            catch (Exception ex)
            {
                // Handle exception
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public async Task<SettingsModel> LoadSettingsAsync()
        {
            try
            {
                var file = await _localFolder.GetFileAsync("settings.json");
                var json = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize<SettingsModel>(json) ?? new SettingsModel();
            }
            catch (FileNotFoundException)
            {
                return new SettingsModel();
            }
            catch (Exception ex)
            {
                // Handle exception
                Console.WriteLine($"Error loading settings: {ex.Message}");
                return new SettingsModel();
            }
        }

        public async Task SaveBlockRulesAsync(BlockRule rules)
        {
            try
            {
                var file = await _localFolder.CreateFileAsync("blockrules.json", CreationCollisionOption.ReplaceExisting);
                var json = JsonSerializer.Serialize(rules, _jsonOptions);
                await FileIO.WriteTextAsync(file, json);
            }
            catch (Exception ex)
            {
                // Handle exception
                Console.WriteLine($"Error saving block rules: {ex.Message}");
            }
        }

        public async Task<BlockRule> LoadBlockRulesAsync()
        {
            try
            {
                var file = await _localFolder.GetFileAsync("blockrules.json");
                var json = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize<BlockRule>(json) ?? new BlockRule();
            }
            catch (FileNotFoundException)
            {
                return new BlockRule();
            }
            catch (Exception ex)
            {
                // Handle exception
                Console.WriteLine($"Error loading block rules: {ex.Message}");
                return new BlockRule();
            }
        }
    }
}