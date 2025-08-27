// Helpers/SerializationHelper.cs
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace FocusMate.Helpers
{
    public static class SerializationHelper
    {
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static async Task<T> DeserializeFromFileAsync<T>(string filePath, JsonSerializerOptions options = null)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(filePath);
                var content = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize<T>(content, options ?? DefaultOptions);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is IOException)
            {
                // Return default instance for non-existent files
                return Activator.CreateInstance<T>();
            }
            catch (JsonException)
            {
                // Handle corrupt JSON files
                return Activator.CreateInstance<T>();
            }
        }

        public static async Task<bool> SerializeToFileAsync<T>(string filePath, T data, JsonSerializerOptions options = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, options ?? DefaultOptions);
                var folder = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                await File.WriteAllTextAsync(filePath, json);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Log error
                return false;
            }
        }

        public static T DeserializeFromString<T>(string json, JsonSerializerOptions options = null)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
            }
            catch (JsonException)
            {
                return default;
            }
        }

        public static string SerializeToString<T>(T data, JsonSerializerOptions options = null)
        {
            return JsonSerializer.Serialize(data, options ?? DefaultOptions);
        }

        public static async Task<T> DeserializeFromAppDataAsync<T>(string fileName)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var filePath = Path.Combine(localFolder.Path, fileName);

            return await DeserializeFromFileAsync<T>(filePath);
        }

        public static async Task<bool> SerializeToAppDataAsync<T>(string fileName, T data)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var filePath = Path.Combine(localFolder.Path, fileName);

            return await SerializeToFileAsync(filePath, data);
        }
    }
}