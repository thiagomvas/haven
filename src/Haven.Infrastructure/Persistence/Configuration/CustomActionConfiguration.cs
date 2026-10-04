using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class CustomActionConfiguration : IEntityTypeConfiguration<CustomAction>
{
    public void Configure(EntityTypeBuilder<CustomAction> builder)
    {
        builder.ToTable("custom_actions");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();
        
        builder.Property(c => c.ServiceId)
            .HasColumnName("service_id")
            .IsRequired();
        
        builder.Property(c => c.ActionName)
            .HasColumnName("action_name")
            .IsRequired();
        
        builder.Property(c => c.Alias)
            .HasColumnName("alias")
            .IsRequired();
        
        builder.Property(c => c.ActionDescription)
            .HasColumnName("action_description")
            .IsRequired();
        
        builder.Property(c => c.Icon)
            .HasColumnName("icon")
            .IsRequired();
        
        builder.Property(a => a.Config)
            .HasColumnType("jsonb")
            .HasConversion(
                config => JsonSerializer.Serialize(config, ActionJson.Options),
                json => ActionJson.DeserializeConfig(json),
                ActionJson.ConfigComparer)
            .IsRequired();
        
        builder.Property(c => c.RequiredPermissions)
            .HasColumnName("required_permissions")
            .HasConversion(
                permissions => JsonSerializer.Serialize(permissions, ActionJson.Options),
                json => JsonSerializer.Deserialize<string[]>(json, ActionJson.Options)!)
            .IsRequired();
        
        builder.Property(c => c.Risk)
            .HasColumnName("risk")
            .IsRequired();
        
        builder.Property(c => c.Timeout)
            .HasColumnName("timeout")
            .IsRequired();
        
        builder.Property(c => c.Token)
            .HasColumnName("token")
            .IsRequired();
        
        builder.HasOne(c => c.Service)
            .WithMany(s => s.CustomActions)
            .HasForeignKey(c => c.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal static class ActionJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static readonly ValueComparer<ActionConfig> ConfigComparer = new(
        (a, b) => Serialize(a) == Serialize(b),
        c => Serialize(c).GetHashCode(),
        c => DeserializeConfig(Serialize(c)));

    /// <summary>
    /// Reads an <see cref="ActionConfig"/>, tolerating payloads whose type discriminator is missing,
    /// not first, or named "type" instead of "$type" by normalizing it before polymorphic deserialization.
    /// </summary>
    public static ActionConfig DeserializeConfig(string json)
    {
        if (JsonNode.Parse(json) is not JsonObject obj)
            throw new JsonException("Action config must be a JSON object.");

        string? discriminator = null;
        var rest = new List<KeyValuePair<string, JsonNode?>>();
        foreach (var (key, value) in obj)
        {
            if (key is "$type" or "type" or "Type")
                discriminator ??= value?.GetValue<string>();
            else
                rest.Add(new(key, value?.DeepClone()));
        }

        discriminator ??= rest.Any(p => p.Key.Equals("command", StringComparison.OrdinalIgnoreCase)) ? "exec"
            : rest.Any(p => p.Key.Equals("url", StringComparison.OrdinalIgnoreCase)) ? "http"
            : throw new JsonException("Action config has no recognizable type.");

        var normalized = new JsonObject { ["$type"] = discriminator.ToLowerInvariant() };
        foreach (var (key, value) in rest)
            normalized[key] = value;

        return normalized.Deserialize<ActionConfig>(Options)!;
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}