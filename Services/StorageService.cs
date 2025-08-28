// Services/StorageService.cs
using FocusMate.Models;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace FocusMate.Services
{
    public class StorageService
    {
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private StorageFolder _dataFolder = ApplicationData.Current.LocalFolder;
        private readonly DispatcherQueue _dispatcherQueue;

        public StorageService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            InitializeDataFolderAsync().FireAndForget();
        }

        private async Task InitializeDataFolderAsync()
        {
            try
            {
                _dataFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(
                    "FocusMateData", CreationCollisionOption.OpenIfExists);
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error creating data folder: {ex.Message}");
            }
        }

        private async Task<StorageFile> GetOrCreateFileAsync(string fileName)
        {
            try
            {
                return await _dataFolder.CreateFileAsync(
                    fileName, CreationCollisionOption.OpenIfExists);
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error creating file {fileName}: {ex.Message}");
                throw;
            }
        }

        private async Task<T> ReadFileAsync<T>(string fileName)
        {
            try
            {
                var file = await _dataFolder.GetFileAsync(fileName);
                var json = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions)
                    ?? throw new InvalidDataException("Deserialization returned null");
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException($"File {fileName} not found");
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error reading {fileName}: {ex.Message}");
                throw;
            }
        }

        private async Task WriteFileAsync<T>(string fileName, T data)
        {
            try
            {
                var tempFile = await _dataFolder.CreateFileAsync(
                    $"{fileName}.tmp", CreationCollisionOption.ReplaceExisting);

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                await FileIO.WriteTextAsync(tempFile, json);

                var finalFile = await _dataFolder.CreateFileAsync(
                    fileName, CreationCollisionOption.ReplaceExisting);

                await tempFile.MoveAndReplaceAsync(finalFile);
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error writing {fileName}: {ex.Message}");
                throw;
            }
        }

        public async Task SaveSessionsAsync(Session[] sessions)
        {
            try
            {
                await WriteFileAsync("sessions.json", sessions);
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error saving sessions: {ex.Message}");
            }
        }

        public async Task<Session[]> LoadSessionsAsync()
        {
            try
            {
                return await ReadFileAsync<Session[]>("sessions.json");
            }
            catch (FileNotFoundException)
            {
                return Array.Empty<Session>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading sessions: {ex.Message}");
                return Array.Empty<Session>();
            }
        }

        public async Task SaveSettingsAsync(SettingsModel settings)
        {
            try
            {
                await WriteFileAsync("settings.json", settings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public async Task<SettingsModel> LoadSettingsAsync()
        {
            try
            {
                return await ReadFileAsync<SettingsModel>("settings.json");
            }
            catch (FileNotFoundException)
            {
                return new SettingsModel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                return new SettingsModel();
            }
        }

        public async Task ExportSessionsToCsvAsync(StorageFile file)
        {
            try
            {
                var sessions = await LoadSessionsAsync();
                var csv = BuildCsvFromSessions(sessions);

                await FileIO.WriteTextAsync(file, csv);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting sessions: {ex.Message}");
                throw;
            }
        }

        private string BuildCsvFromSessions(Session[] sessions)
        {
            var csv = new StringBuilder();
            csv.AppendLine("StartTime,EndTime,DurationMinutes,Label,Category,Mode,WasInterrupted");

            foreach (var session in sessions)
            {
                csv.AppendLine(
                    $"{session.StartUtc:yyyy-MM-dd HH:mm}," +
                    $"{session.EndUtc:yyyy-MM-dd HH:mm}," +
                    $"{session.DurationMinutes}," +
                    $"{EscapeCsvField(session.Label)}," +
                    $"{EscapeCsvField(session.Category)}," +
                    $"{session.Mode}," +
                    $"{session.WasInterrupted}");
            }

            return csv.ToString();
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return string.Empty;
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
    }

    // Helper extension for fire-and-forget tasks with error logging
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