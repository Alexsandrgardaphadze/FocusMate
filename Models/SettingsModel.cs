// Models/SettingsModel.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FocusMate.Models
{
    public sealed class SettingsModel
    {
        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        [JsonPropertyName("defaultFocusMinutes")]
        public int DefaultFocusMinutes { get; set; } = 50;

        [JsonPropertyName("shortBreakMinutes")]
        public int ShortBreakMinutes { get; set; } = 10;

        [JsonPropertyName("longBreakMinutes")]
        public int LongBreakMinutes { get; set; } = 25;

        [JsonPropertyName("autoStartNext")]
        public bool AutoStartNext { get; set; } = true;

        [JsonPropertyName("useWindowsNotificationSound")]
        public bool UseWindowsNotificationSound { get; set; } = true;

        [JsonPropertyName("customSoundPath")]
        public string? CustomSoundPath { get; set; }

        [JsonPropertyName("blockRule")]
        public BlockRule BlockRule { get; set; } = new BlockRule();

        [JsonPropertyName("runMinimized")]
        public bool RunMinimized { get; set; }

        [JsonPropertyName("startWithWindows")]
        public bool StartWithWindows { get; set; }

        [JsonPropertyName("defaultCategoryId")]
        public Guid? DefaultCategoryId { get; set; }

        public bool Validate(out string errorMessage)
        {
            if (DefaultFocusMinutes <= 0)
            {
                errorMessage = "Focus duration must be greater than 0.";
                return false;
            }

            if (ShortBreakMinutes <= 0 || LongBreakMinutes <= 0)
            {
                errorMessage = "Break durations must be greater than 0.";
                return false;
            }

            if (ShortBreakMinutes >= DefaultFocusMinutes)
            {
                errorMessage = "Short break should be shorter than focus session.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}