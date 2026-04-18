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
            UserModified = true
        });

        var doc = ProjectDocumentBuilder.Build(m, 99, auditUtc.AddDays(-1));
        var back = ProjectDocumentMapper.ToModel(doc);

        var t = Assert.Single(back.Threats);
        Assert.Equal(ThreatStatus.Accepted, t.Status);
        Assert.Equal(auditUtc, t.StatusChangedAtUtc);
        Assert.Equal("Alice", t.StatusChangedBy);
        Assert.Equal("Risico geaccepteerd ticket-42", t.StatusChangeNote);
    }
}
