// Models/BlockRule.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FocusMate.Models
{
    public sealed class BlockRule
    {
        [JsonPropertyName("apps")]
        public List<AppBlockRule> Apps { get; set; } = new();

        [JsonPropertyName("sites")]
        public List<SiteBlockRule> Sites { get; set; } = new();

        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; } = true;

        [JsonPropertyName("focusSessionsOnly")]
        public bool FocusSessionsOnly { get; set; } = true;
    }

    public sealed class AppBlockRule
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("processName")]
        public string ProcessName { get; set; } = string.Empty;

        [JsonPropertyName("friendlyName")]
        public string FriendlyName { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public BlockAction Action { get; set; } = BlockAction.KillProcess;

        [JsonPropertyName("gracePeriodSeconds")]
        public int GracePeriodSeconds { get; set; } = 5;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;
    }

    public sealed class SiteBlockRule
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("domain")]
        public string Domain { get; set; } = string.Empty;

        [JsonPropertyName("friendlyName")]
        public string FriendlyName { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public BlockAction Action { get; set; } = BlockAction.Warn;

        [JsonPropertyName("includeSubdomains")]
        public bool IncludeSubdomains { get; set; } = true;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;
    }

    public enum BlockAction
    {
        Warn = 0,
        KillProcess = 1,
        CloseWindow = 2,
        BlockNetwork = 3
    }
}