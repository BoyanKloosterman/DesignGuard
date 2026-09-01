using DesignGuard.Data.Mongo;
using DesignGuard.Models;
using Xunit;

namespace DesignGuard.Tests.Data.Mongo;

/// <summary>Bevestigt Mongo-pad: status-audit (wie/wanneer/toelichting) overleeft build→map→model.</summary>
public sealed class ThreatStatusAuditPersistenceTests
{
    [Fact]
    public void Threat_status_audit_rondrit_via_ProjectDocument()
    {
        var auditUtc = new DateTime(2026, 4, 18, 14, 30, 0, DateTimeKind.Utc);
        var m = new ProjectModel { Name = "P", SystemName = "S" };
        m.Threats.Add(new ThreatModel
        {
            Id = "th-1",
            Title = "XSS",
            Status = ThreatStatus.Accepted,
            StatusChangedAtUtc = auditUtc,
            StatusChangedBy = "Alice",
            StatusChangeNote = "Risico geaccepteerd ticket-42",
            UserModified = true,
            Likelihood = 4,
            Impact = 5
        });

        var doc = ProjectDocumentBuilder.Build(m, 99, auditUtc.AddDays(-1));
        var back = ProjectDocumentMapper.ToModel(doc);

        var t = Assert.Single(back.Threats);
        Assert.Equal(ThreatStatus.Accepted, t.Status);
        Assert.Equal(4, t.Likelihood);
        Assert.Equal(5, t.Impact);
        Assert.Equal(20, t.RiskScore);
        Assert.Equal(auditUtc, t.StatusChangedAtUtc);
        Assert.Equal("Alice", t.StatusChangedBy);
        Assert.Equal("Risico geaccepteerd ticket-42", t.StatusChangeNote);
    }

    [Fact]
    public void Finding_rondrit_via_FindingsJson()
    {
        var m = new ProjectModel { Name = "P", SystemName = "S" };
        m.Findings.Add(new PentestFindingModel
        {
            Id = "find-1",
            Title = "IDOR",
            Description = "Cross-tenant read",
            AffectedTarget = "https://api.test/orders/1",
            EvidenceNotes = "HTTP 200",
            Recommendation = "Object-level authz",
            WstgCategory = "Autorisatie",
            Likelihood = 4,
            Impact = 5,
            Status = FindingStatus.Confirmed,
            LinkedThreatId = "th-1",
            Notes = "testomgeving"
        });
        m.AssessmentContact = "Sec";
        m.AssessmentWindow = "week 16";
        m.AssessmentEnvironment = "test";
        m.AssessmentAccounts = "shop-user";
        m.AssessmentLimitations = "geen DoS";

        var doc = ProjectDocumentBuilder.Build(m, 7, DateTime.UtcNow);
        Assert.Contains("IDOR", doc.FindingsJson, StringComparison.Ordinal);
        Assert.Equal("Sec", doc.AssessmentContact);

        var back = ProjectDocumentMapper.ToModel(doc);
        var f = Assert.Single(back.Findings);
        Assert.Equal("find-1", f.Id);
        Assert.Equal("IDOR", f.Title);
        Assert.Equal(4, f.Likelihood);
        Assert.Equal(5, f.Impact);
        Assert.Equal(20, f.RiskScore);
        Assert.Equal(FindingStatus.Confirmed, f.Status);
        Assert.Equal("th-1", f.LinkedThreatId);
        Assert.Equal("test", back.AssessmentEnvironment);
        Assert.Equal("geen DoS", back.AssessmentLimitations);
    }

    [Fact]
    public void Requirement_status_audit_rondrit_via_ProjectDocument()
    {
        var auditUtc = new DateTime(2026, 4, 18, 9, 0, 0, DateTimeKind.Utc);
        var m = new ProjectModel { Name = "P", SystemName = "S" };
        m.Requirements.Add(new RequirementModel
        {
            Id = "rq-1",
            Title = "Auth",
            Category = "Sec",
            Status = RequirementStatus.Implemented,
            StatusChangedAtUtc = auditUtc,
            StatusChangedBy = "Bob",
            StatusChangeNote = "Release 2"
        });

        var doc = ProjectDocumentBuilder.Build(m, 3, auditUtc.AddDays(-2));
        var back = ProjectDocumentMapper.ToModel(doc);

        var r = Assert.Single(back.Requirements);
        Assert.Equal(RequirementStatus.Implemented, r.Status);
        Assert.Equal(auditUtc, r.StatusChangedAtUtc);
        Assert.Equal("Bob", r.StatusChangedBy);
        Assert.Equal("Release 2", r.StatusChangeNote);
    }
}
