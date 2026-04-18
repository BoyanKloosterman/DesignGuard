using DesignGuard.Models;
using DesignGuard.Services;
using Xunit;

namespace DesignGuard.Tests.Services;

public sealed class DesignValidationServiceTests
{
    private readonly DesignValidationService _sut = new();

    [Fact]
    public void Ongeldige_datastroom_geeft_fout()
    {
        var p = new ProjectModel
        {
            Name = "P",
            Components = { new ComponentModel { Id = 1, Name = "A" } },
            DataFlows =
            {
                new DataFlowModel { FromComponentId = 99, ToComponentId = 1, Label = "x" }
            }
        };

        var f = _sut.Validate(p);
        Assert.Contains(f, x => x.Severity == DesignValidationSeverity.Error && x.Code == "FLOW-FROM");
    }

    [Fact]
    public void Leeg_project_geeft_info_ok()
    {
        var f = _sut.Validate(new ProjectModel());
        Assert.Single(f);
        Assert.Equal("OK", f[0].Code);
    }
}
