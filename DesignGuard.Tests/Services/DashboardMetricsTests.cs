using DesignGuard.Models;
using DesignGuard.Services;
using Xunit;

namespace DesignGuard.Tests.Services;

public sealed class DashboardMetricsTests
{
    [Fact]
    public void Compute_telt_Open_en_Mitigated_en_eisen()
    {
        var threats = new[]
        {
            new ThreatModel { Status = ThreatStatus.Open },
            new ThreatModel { Status = ThreatStatus.Open },
            new ThreatModel { Status = ThreatStatus.Mitigated },
            new ThreatModel { Status = ThreatStatus.Accepted }
        };
        var reqs = new[]
        {
            new RequirementModel { Status = RequirementStatus.Proposed },
            new RequirementModel { Status = RequirementStatus.Accepted },
            new RequirementModel { Status = RequirementStatus.Implemented }
        };

        var (o, m, orc, ir) = DashboardMetrics.Compute(threats, reqs);
        Assert.Equal(2, o);
        Assert.Equal(2, m);
        Assert.Equal(2, orc);
        Assert.Equal(1, ir);
    }
}
