using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>Kans × impact (1–5) naar score, risicoklasse en afgeleide ernst.</summary>
public static class RiskScoring
{
    public static readonly IReadOnlyList<int> Scale = [1, 2, 3, 4, 5];

    public static int Score(int likelihood, int impact)
    {
        if (likelihood is < 1 or > 5 || impact is < 1 or > 5) return 0;
        return likelihood * impact;
    }

    public static RiskLevel Level(int score) => score switch
    {
        <= 0 => RiskLevel.Unspecified,
        <= 4 => RiskLevel.Low,
        <= 9 => RiskLevel.Medium,
        <= 16 => RiskLevel.High,
        _ => RiskLevel.Critical
    };

    public static RiskLevel LevelOf(ThreatModel t) => Level(Score(t.Likelihood, t.Impact));

    public static string LevelLabel(RiskLevel level) => level switch
    {
        RiskLevel.Low => "Laag",
        RiskLevel.Medium => "Midden",
        RiskLevel.High => "Hoog",
        RiskLevel.Critical => "Kritiek",
        _ => "n.n.b."
    };

    public static string LikelihoodLabel(int value) => value switch
    {
        1 => "1 Zeldzaam",
        2 => "2 Onwaarschijnlijk",
        3 => "3 Mogelijk",
        4 => "4 Waarschijnlijk",
        5 => "5 Zeer waarschijnlijk",
        _ => "n.n.b."
    };

    public static string ImpactLabel(int value) => value switch
    {
        1 => "1 Verwaarloosbaar",
        2 => "2 Beperkt",
        3 => "3 Merkbaar",
        4 => "4 Ernstig",
        5 => "5 Kritiek",
        _ => "n.n.b."
    };

    public static SeverityEstimate ToSeverity(RiskLevel level) => level switch
    {
        RiskLevel.Low => SeverityEstimate.Low,
        RiskLevel.High or RiskLevel.Critical => SeverityEstimate.High,
        _ => SeverityEstimate.Medium
    };

    /// <summary>Oude ernst zonder K×I: waarden die terugkaatsen naar dezelfde risicoklasse.</summary>
    public static (int Likelihood, int Impact) FromSeverity(SeverityEstimate severity) => severity switch
    {
        SeverityEstimate.Low => (2, 2),
        SeverityEstimate.High => (4, 4),
        _ => (3, 3)
    };

    public static bool HasScores(ThreatModel t) =>
        t.Likelihood is >= 1 and <= 5 && t.Impact is >= 1 and <= 5;

    public static void EnsureScores(ThreatModel t)
    {
        if (!HasScores(t))
        {
            var (l, i) = FromSeverity(t.Severity);
            t.Likelihood = l;
            t.Impact = i;
        }

        SyncSeverity(t);
    }

    public static void SyncSeverity(ThreatModel t)
    {
        if (!HasScores(t)) return;
        t.Severity = ToSeverity(LevelOf(t));
    }

    public static string FormatSummary(ThreatModel t)
    {
        if (!HasScores(t)) return t.Severity.ToString();
        var score = Score(t.Likelihood, t.Impact);
        return $"K{t.Likelihood} × I{t.Impact} = {score} ({LevelLabel(Level(score))})";
    }

    /// <summary>Voorstel voor kans/impact op basis van ernst en ontwerpcontext.</summary>
    public static void ApplyHeuristics(SystemDesignContextLike ctx, ThreatModel t)
    {
        if (!HasScores(t))
        {
            var (l, i) = FromSeverity(t.Severity);
            t.Likelihood = l;
            t.Impact = i;
        }

        if (ctx.InternetExposed)
            t.Likelihood = Math.Min(5, t.Likelihood + 1);

        if (ctx.HasAdmin || ctx.PersonalData || ctx.CriticalBusiness)
            t.Impact = Math.Min(5, t.Impact + 1);

        SyncSeverity(t);
    }
}

/// <summary>Minimale ontwerpcontext voor K×I-heuristiek (los van rules-context).</summary>
public readonly record struct SystemDesignContextLike(
    bool InternetExposed,
    bool HasAdmin,
    bool PersonalData,
    bool CriticalBusiness);
