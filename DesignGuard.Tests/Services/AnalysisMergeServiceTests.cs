using DesignGuard.Models;
using DesignGuard.Services;
using Xunit;

namespace DesignGuard.Tests.Services;

public sealed class AnalysisMergeServiceTests
{
    private readonly AnalysisMergeService _sut = new();

    [Fact]
    public void MergeThreats_behoudt_custom_en_UserModified_generated()
    {
        var project = new ProjectModel();
        var custom = new ThreatModel
        {
            Origin = ThreatOrigin.Custom,
            Title = "Handmatig",
            RuleFingerprint = "c1"
        };
        var userLocked = new ThreatModel
        {
            Origin = ThreatOrigin.Generated,
            UserModified = true,
            RuleFingerprint = "rule-x",
            Title = "Oud maar vastgezet"
        };
        project.Threats.Add(custom);
        project.Threats.Add(userLocked);

        var generated = new List<ThreatModel>
        {
            new()
            {
                Origin = ThreatOrigin.Generated,
                RuleFingerprint = "rule-x",
                Title = "Nieuwe versie"
            },
            new()
            {
                Origin = ThreatOrigin.Generated,
                RuleFingerprint = "rule-y",
                Title = "Vers"
            }
        };

        _sut.MergeThreats(project, generated);

        Assert.Equal(3, project.Threats.Count);
        Assert.Contains(project.Threats, t => t.Title == "Handmatig");
        Assert.Contains(project.Threats, t => t.RuleFingerprint == "rule-x" && t.Title == "Oud maar vastgezet");
        Assert.Contains(project.Threats, t => t.RuleFingerprint == "rule-y");
        Assert.DoesNotContain(project.Threats, t => t.Title == "Nieuwe versie");
    }

    [Fact]
    public void MergeRequirements_overneemt_Status_van_oude_generated_rij()
    {
        var project = new ProjectModel();
        var old = new RequirementModel
        {
            Origin = RequirementOrigin.Generated,
            UserModified = false,
            RuleFingerprint = "r1",
            Status = RequirementStatus.Implemented,
            Title = "Eis"
        };
        project.Requirements.Add(old);

        var generated = new List<RequirementModel>
        {
            new()
            {
                Origin = RequirementOrigin.Generated,
                RuleFingerprint = "r1",
                Status = RequirementStatus.Proposed,
                Title = "Eis"
            }
        };

        _sut.MergeRequirements(project, generated);

        Assert.Single(project.Requirements);
        Assert.Equal(RequirementStatus.Implemented, project.Requirements[0].Status);
    }

    [Fact]
    public void MergeRequirements_behoudt_status_audit_van_oude_generated_rij()
    {
        var at = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var project = new ProjectModel();
        var old = new RequirementModel
        {
            Origin = RequirementOrigin.Generated,
            UserModified = false,
            RuleFingerprint = "r1",
            Status = RequirementStatus.Implemented,
            Title = "Eis",
            StatusChangedAtUtc = at,
            StatusChangedBy = "Charlie",
            StatusChangeNote = "OK"
        };
        project.Requirements.Add(old);

        var generated = new List<RequirementModel>
        {
            new()
            {
                Origin = RequirementOrigin.Generated,
                RuleFingerprint = "r1",
                Status = RequirementStatus.Proposed,
                Title = "Eis"
            }
        };

        _sut.MergeRequirements(project, generated);

        var r = Assert.Single(project.Requirements);
        Assert.Equal(at, r.StatusChangedAtUtc);
        Assert.Equal("Charlie", r.StatusChangedBy);
        Assert.Equal("OK", r.StatusChangeNote);
    }

    [Fact]
    public void MergeThreats_behoudt_kans_en_impact_van_oude_generated_rij()
    {
        var project = new ProjectModel();
        project.Threats.Add(new ThreatModel
        {
            Origin = ThreatOrigin.Generated,
            UserModified = false,
            RuleFingerprint = "t1",
            Title = "Oud",
            Likelihood = 5,
            Impact = 4
        });

        _sut.MergeThreats(project, new List<ThreatModel>
        {
            new()
            {
                Origin = ThreatOrigin.Generated,
                RuleFingerprint = "t1",
                Title = "Nieuw",
                Likelihood = 2,
                Impact = 2
            }
        });

        var t = Assert.Single(project.Threats);
        Assert.Equal("Nieuw", t.Title);
        Assert.Equal(5, t.Likelihood);
        Assert.Equal(4, t.Impact);
        Assert.Equal(SeverityEstimate.High, t.Severity);
    }
}
