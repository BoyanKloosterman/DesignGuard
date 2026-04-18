using DesignGuard.Models;
using DesignGuard.Rules;
using DesignGuard.Services;
using Xunit;

namespace DesignGuard.Tests.Services;

public sealed class TraceabilityServiceTests
{
    private readonly TraceabilityService _sut = new();

    [Fact]
    public void BuildTraceabilitySummary_bevat_dreiging_en_uitlegregels()
    {
        var p = new ProjectModel { Name = "P" };
        var t = new ThreatModel
        {
            Title = "Datalek",
            StrideCategory = StrideCategory.InformationDisclosure,
            Severity = SeverityEstimate.High,
            GenerationReason = "Test",
            TriggerKeys = new List<string> { RuleTriggerKeys.InternetExposed }
        };
        p.Threats.Add(t);

        var text = _sut.BuildTraceabilitySummary(p);
        Assert.Contains("Traceability-overzicht", text);
        Assert.Contains("Datalek", text);
        Assert.Contains("Het systeem is blootgesteld aan internet.", text);
        Assert.Contains("Test", text);
    }

    [Fact]
    public void ExplainThreat_gebruikt_label_voor_bekende_trigger()
    {
        var p = new ProjectModel();
        var t = new ThreatModel
        {
            Title = "X",
            TriggerKeys = new List<string> { RuleTriggerKeys.PersonalData },
            GenerationReason = "gr"
        };
        var ex = _sut.ExplainThreat(p, t);
        Assert.Contains(ex.Lines, line => line.Contains("persoonsgegevens", StringComparison.OrdinalIgnoreCase));
    }
}
