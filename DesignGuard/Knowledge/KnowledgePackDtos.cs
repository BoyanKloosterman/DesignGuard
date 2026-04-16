using System.Text.Json.Serialization;

namespace DesignGuard.Knowledge;

public sealed class KnowledgePackIndexDto
{
    public List<string> PackFiles { get; set; } = new();
}

public sealed class KnowledgePackFileDto
{
    public string PackId { get; set; } = "";

    public string DisplayLabel { get; set; } = "";

    public string VersionLabel { get; set; } = "";

    public string SourceName { get; set; } = "";

    public string PublicationOrReviewDate { get; set; } = "";

    public DateTime? LastReviewedUtc { get; set; }

    public string Disclaimer { get; set; } = "";

    public bool IsActive { get; set; } = true;

    public bool IsArchived { get; set; }

    public List<KnowledgeGuidanceItemDto> Items { get; set; } = new();

    public List<KnowledgeMappingRuleDto> MappingRules { get; set; } = new();
}

public sealed class KnowledgeGuidanceItemDto
{
    public string Id { get; set; } = "";

    public string Title { get; set; } = "";

    public string Category { get; set; } = "";

    public string PlainLanguageExplanation { get; set; } = "";

    public string WhyItAppliesNote { get; set; } = "";

    public List<string> SourceTags { get; set; } = new();

    public string SourceReference { get; set; } = "";

    public string ReviewNote { get; set; } = "";

    [JsonPropertyName("guidanceNature")]
    public string GuidanceNature { get; set; } = "IndustryGuidanceInspired";
}

public sealed class KnowledgeMappingRuleDto
{
    public string? MatchRequirementRuleNameContains { get; set; }

    public string? MatchThreatRuleNameContains { get; set; }

    public List<string> GuidanceItemIds { get; set; } = new();
}
