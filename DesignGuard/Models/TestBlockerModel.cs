namespace DesignGuard.Models;

/// <summary>Iets dat tijdens de test niet kon, los van kick-off-beperkingen.</summary>
public sealed class TestBlockerModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Reason { get; set; } = "";
    public string CoverageThemeId { get; set; } = "";
}
