using System.Text.Json.Serialization;

namespace DesignGuard.Services;

internal sealed class ControlLibraryFileDto
{
    [JsonPropertyName("items")] public List<ControlLibraryItemDto> Items { get; set; } = new();
}

internal sealed class ControlLibraryItemDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("sourceTags")] public List<string> SourceTags { get; set; } = new();
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("implementationGuidance")] public string ImplementationGuidance { get; set; } = "";
    [JsonPropertyName("when")] public ControlLibraryWhenDto? When { get; set; }
}

internal sealed class ControlLibraryWhenDto
{
    [JsonPropertyName("anyProjectFlag")] public List<string> AnyProjectFlag { get; set; } = new();
    [JsonPropertyName("anyThreatTriggerContains")] public List<string> AnyThreatTriggerContains { get; set; } = new();
    [JsonPropertyName("anyRequirementTriggerContains")] public List<string> AnyRequirementTriggerContains { get; set; } = new();
    [JsonPropertyName("anyComponentTagContains")] public List<string> AnyComponentTagContains { get; set; } = new();
}
