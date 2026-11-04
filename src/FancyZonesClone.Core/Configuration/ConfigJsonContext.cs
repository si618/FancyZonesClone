using System.Text.Json;
using System.Text.Json.Serialization;
using FancyZonesClone.Core.Layouts;

namespace FancyZonesClone.Core.Configuration;

/// <summary>Source-generated JSON metadata, so serialisation works under Native AOT and trimming.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    WriteIndented = true,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(FancyZonesCloneConfig))]
[JsonSerializable(typeof(GridLayoutInfo))]
internal sealed partial class ConfigJsonContext : JsonSerializerContext;
