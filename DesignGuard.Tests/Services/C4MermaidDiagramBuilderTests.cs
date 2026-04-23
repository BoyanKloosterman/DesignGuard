using DesignGuard.Models;
using DesignGuard.Services;
using Xunit;

namespace DesignGuard.Tests.Services;

public sealed class C4MermaidDiagramBuilderTests
{
    private readonly C4MermaidDiagramBuilder _sut = new();

    [Fact]
    public void Build_Context_includes_Rel_when_both_endpoints_in_diagram()
    {
        var p = new ProjectModel { Name = "X", SystemName = "Sys", Description = "d" };
        p.C4Elements.Add(new C4ElementModel { Id = 1, Level = C4Level.Context, Name = "User", Description = "gebruiker" });
        p.C4Relations.Add(new C4RelationModel { Id = 1, FromElementId = 1, ToElementId = 0, Label = "uses" });

        var code = _sut.Build(C4MermaidBand.Context, p);
        Assert.Contains("Rel(E1, SysInScope", code, StringComparison.Ordinal);
        Assert.Contains("uses", code, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_Container_skips_rel_when_endpoint_is_only_SysInScope()
    {
        var p = new ProjectModel { Name = "X" };
        p.C4Elements.Add(new C4ElementModel { Id = 1, Level = C4Level.Context, Name = "Ext", Description = "" });
        p.C4Relations.Add(new C4RelationModel { Id = 1, FromElementId = 0, ToElementId = 1, Label = "x" });
        var code = _sut.Build(C4MermaidBand.Container, p);
        Assert.DoesNotContain("Rel(", code, StringComparison.Ordinal);
    }
}
