// Models/Session.cs
using System;
using System.Text.Json.Serialization;

namespace FocusMate.Models
{
    public sealed class Session
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonPropertyName("startUtc")]
        public DateTimeOffset StartUtc { get; set; } = DateTimeOffset.UtcNow;

        [JsonPropertyName("endUtc")]
        public DateTimeOffset EndUtc { get; set; }

        [JsonPropertyName("durationMinutes")]
        public int DurationMinutes { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("wasInterrupted")]
        public bool WasInterrupted { get; set; }

        [JsonPropertyName("mode")]
        public TimerMode Mode { get; set; } = TimerMode.Focus;

        [JsonIgnore]
        public string FormattedDuration =>
            DurationMinutes >= 60
                ? $"{DurationMinutes / 60}h {DurationMinutes % 60}m"
                : $"{DurationMinutes}m";

        // (Optional) Convenience property for quick check
        [JsonIgnore]
        public bool IsCompleted => EndUtc > StartUtc && DurationMinutes > 0;
    }

    public enum TimerMode
    {
        Focus = 0,
        ShortBreak = 1,
        LongBreak = 2,
        Custom = 3
    }
}
