namespace DesignGuard.Models;

public sealed class RequirementModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public List<string> SourceTags { get; set; } = new();
    public string PlainExplanation { get; set; } = "";
    public string WhyApplies { get; set; } = "";
    public string ImplementationDirection { get; set; } = "";
    public ExplanationModel Explanation { get; set; } = new();
}
