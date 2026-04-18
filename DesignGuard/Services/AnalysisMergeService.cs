using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>Versmelt gegenereerde analyse met handmatige aanpassingen.</summary>
public sealed class AnalysisMergeService
{
    public void MergeThreats(ProjectModel project, IReadOnlyList<ThreatModel> generated)
    {
        var userFp = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in project.Threats)
        {
            if (t is { Origin: ThreatOrigin.Generated, UserModified: true } &&
                !string.IsNullOrEmpty(t.RuleFingerprint))
                userFp.Add(t.RuleFingerprint);
        }

        var preserved = project.Threats
            .Where(t => t.Origin == ThreatOrigin.Custom || t.UserModified)
            .ToList();

        var merged = new List<ThreatModel>();
        foreach (var g in generated)
        {
            if (g.RuleFingerprint != null && userFp.Contains(g.RuleFingerprint))
                continue;

            var old = project.Threats.FirstOrDefault(x =>
                x.RuleFingerprint == g.RuleFingerprint &&
                x is { Origin: ThreatOrigin.Generated, UserModified: false });
            if (old != null)
            {
                g.Status = old.Status;
                g.Notes = old.Notes;
                g.StatusChangedAtUtc = old.StatusChangedAtUtc;
                g.StatusChangedBy = old.StatusChangedBy;
                g.StatusChangeNote = old.StatusChangeNote;
                if (string.IsNullOrWhiteSpace(g.SourceAttribution.KnowledgePackId) &&
                    !string.IsNullOrWhiteSpace(old.SourceAttribution.KnowledgePackId))
                    g.SourceAttribution = old.SourceAttribution;
            }

            merged.Add(g);
        }

        project.Threats.Clear();
        foreach (var t in preserved)
            project.Threats.Add(t);
        foreach (var t in merged)
            project.Threats.Add(t);
    }

    public void MergeRequirements(ProjectModel project, IReadOnlyList<RequirementModel> generated)
    {
        var userFp = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in project.Requirements)
        {
            if (r is { Origin: RequirementOrigin.Generated, UserModified: true } &&
                !string.IsNullOrEmpty(r.RuleFingerprint))
                userFp.Add(r.RuleFingerprint);
        }

        var preserved = project.Requirements
            .Where(r => r.Origin == RequirementOrigin.Custom || r.UserModified)
            .ToList();

        var merged = new List<RequirementModel>();
        foreach (var g in generated)
        {
            if (g.RuleFingerprint != null && userFp.Contains(g.RuleFingerprint))
                continue;

            var old = project.Requirements.FirstOrDefault(x =>
                x.RuleFingerprint == g.RuleFingerprint &&
                x is { Origin: RequirementOrigin.Generated, UserModified: false });
            if (old != null)
            {
                g.Status = old.Status;
                g.Notes = old.Notes;
                if (string.IsNullOrWhiteSpace(g.SourceAttribution.KnowledgePackId) &&
                    !string.IsNullOrWhiteSpace(old.SourceAttribution.KnowledgePackId))
                    g.SourceAttribution = old.SourceAttribution;
            }

            merged.Add(g);
        }

        project.Requirements.Clear();
        foreach (var r in preserved)
            project.Requirements.Add(r);
        foreach (var r in merged)
            project.Requirements.Add(r);
    }
}
