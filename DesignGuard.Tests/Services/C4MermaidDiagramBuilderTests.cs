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

    [Fact]
    public void Build_Container_gebruikt_altijd_Rel_voor_mermaid11_compat()
    {
        var p = new ProjectModel { Name = "X" };
        p.C4Elements.Add(new C4ElementModel { Id = 1, Level = C4Level.Context, Name = "User", Description = "gebruiker" });
        p.C4Elements.Add(new C4ElementModel { Id = 5, Level = C4Level.Container, Name = "API", Description = "", Technology = "" });
        p.C4Relations.Add(new C4RelationModel
        {
            Id = 1,
            FromElementId = 1,
            ToElementId = 5,
            Label = "call",
            LineKind = C4MermaidRelLineKind.Down
        });
        var code = _sut.Build(C4MermaidBand.Container, p);
        Assert.Contains("Rel(E1, E5", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Rel_D", code, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_Component_skipt_rel_tussen_container_en_direct_child()
    {
        var p = new ProjectModel { Name = "X" };
        p.C4Elements.Add(new C4ElementModel { Id = 8, Level = C4Level.Container, Name = "Svc", Description = "", Technology = "" });
        p.C4Elements.Add(new C4ElementModel { Id = 13, Level = C4Level.Component, Name = "Api", Description = "", Technology = "", ParentId = 8 });
        p.C4Elements.Add(new C4ElementModel { Id = 14, Level = C4Level.Component, Name = "Api2", Description = "", Technology = "", ParentId = 8 });
        p.C4Relations.Add(new C4RelationModel { Id = 1, FromElementId = 13, ToElementId = 8, Label = "deel van" });
        p.C4Relations.Add(new C4RelationModel { Id = 2, FromElementId = 13, ToElementId = 14, Label = "intern" });
        var code = _sut.Build(C4MermaidBand.Component, p);
        Assert.Contains("Rel(E13, E14", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Rel(E13, E8", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Rel(E8, E13", code, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_Code_skipt_rel_tussen_component_en_direct_child_code()
    {
        var p = new ProjectModel { Name = "X" };
        p.C4Elements.Add(new C4ElementModel { Id = 13, Level = C4Level.Component, Name = "Api", Description = "", Technology = "" });
        p.C4Elements.Add(new C4ElementModel { Id = 16, Level = C4Level.Code, Name = "C1", Description = "", Technology = "", ParentId = 13 });
        p.C4Elements.Add(new C4ElementModel { Id = 17, Level = C4Level.Code, Name = "C2", Description = "", Technology = "", ParentId = 13 });
        p.C4Relations.Add(new C4RelationModel { Id = 1, FromElementId = 16, ToElementId = 13, Label = "in" });
        p.C4Relations.Add(new C4RelationModel { Id = 2, FromElementId = 16, ToElementId = 17, Label = "roept" });
        var code = _sut.Build(C4MermaidBand.Code, p);
        Assert.Contains("Rel(E16, E17", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Rel(E16, E13", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Rel(E13, E16", code, StringComparison.Ordinal);
    }
}
