namespace DesignGuard.Models;

public sealed class AssetModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Enum-naam of eigen label (DB string).</summary>
    public string Classification { get; set; } = nameof(AssetClassification.Unspecified);

    /// <summary>Enum-naam of eigen label (DB string).</summary>
    public string Sensitivity { get; set; } = nameof(DataSensitivity.None);
    public string Notes { get; set; } = "";

    /// <summary>Eerste gekoppelde component (legacy + weergave); gelijk aan eerste id in <see cref="RelatedComponentIds"/>.</summary>
    public int RelatedComponentId { get; set; }

    /// <summary>Alle gekoppelde component-id's (volgorde behouden).</summary>
    public List<int> RelatedComponentIds { get; set; } = new();

    /// <summary>Vul RelatedComponentIds vanuit legacy veld indien nodig; zet RelatedComponentId op eerste id.</summary>
    public void NormalizeRelatedComponents()
    {
        var ids = new List<int>();
        foreach (var x in RelatedComponentIds)
        {
            if (x <= 0 || ids.Contains(x)) continue;
            ids.Add(x);
        }

        if (RelatedComponentId > 0 && !ids.Contains(RelatedComponentId))
            ids.Insert(0, RelatedComponentId);

        RelatedComponentIds = ids;
        RelatedComponentId = ids.Count > 0 ? ids[0] : 0;
    }
}
