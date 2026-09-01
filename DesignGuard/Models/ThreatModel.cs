using DesignGuard.Services;

namespace DesignGuard.Models;

public sealed class ThreatModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>Stabiele sleutel voor dedup/sync met regels.</summary>
    public string? RuleFingerprint { get; set; }
    public ThreatOrigin Origin { get; set; } = ThreatOrigin.Generated;
    public bool UserModified { get; set; }

    public string Title { get; set; } = "";
    public StrideCategory StrideCategory { get; set; }
    public SeverityEstimate Severity { get; set; } = SeverityEstimate.Medium;
    /// <summary>Kans 1–5; 0 = nog niet gezet (backfill uit ernst).</summary>
    public int Likelihood { get; set; }
    /// <summary>Impact 1–5; 0 = nog niet gezet (backfill uit ernst).</summary>
    public int Impact { get; set; }
    public int RiskScore => RiskScoring.Score(Likelihood, Impact);
    public RiskLevel RiskLevel => RiskScoring.LevelOf(this);
    public string RiskSummary => RiskScoring.FormatSummary(this);
    public ThreatStatus Status { get; set; } = ThreatStatus.Open;

    /// <summary>UTC-tijdstip van de laatste statuswijziging (review/mitigatie).</summary>
    public DateTime? StatusChangedAtUtc { get; set; }

    /// <summary>Wie de status vastlegde (vrij veld, zie ook instellingen ‘Weergavenaam reviewer’).</summary>
    public string StatusChangedBy { get; set; } = "";

    /// <summary>Korte toelichting bij de laatste statuswijziging (bijv. geaccepteerd risico, verwijzing ticket).</summary>
    public string StatusChangeNote { get; set; } = "";

    public string Notes { get; set; } = "";

    public string Description { get; set; } = "";
    public List<string> AffectedComponents { get; set; } = new();
    public List<string> AffectedAssets { get; set; } = new();
    public string GenerationReason { get; set; } = "";
    public List<string> SuggestedMitigations { get; set; } = new();
    public ExplanationModel Explanation { get; set; } = new();

    /// <summary>Kenmerken uit ontwerp dat deze dreiging activeerde (traceability).</summary>
    public List<string> TriggerKeys { get; set; } = new();
    public List<int> RelatedDesignNoteIds { get; set; } = new();

    public SourceAttributionModel SourceAttribution { get; set; } = new();
}
