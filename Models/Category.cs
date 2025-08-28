// Models/Category.cs
using System;
using System.Text.Json.Serialization;

namespace FocusMate.Models
{
    public sealed class Category : IEquatable<Category>
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public string Color { get; set; } = "#0078D4";

        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

        [JsonPropertyName("lastUsed")]
        public DateTimeOffset? LastUsed { get; set; }

        [JsonIgnore]
        public string DisplayName => IsDefault ? $"{Name} (Default)" : Name;

        public bool Equals(Category? other) => other is not null && Id.Equals(other.Id);
        public override bool Equals(object? obj) => Equals(obj as Category);
        public override int GetHashCode() => Id.GetHashCode();
    }
}