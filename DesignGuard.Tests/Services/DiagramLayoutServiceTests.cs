using DesignGuard.Models;
using DesignGuard.Services;
using Xunit;

namespace DesignGuard.Tests.Services;

public sealed class DiagramLayoutServiceTests
{
    private readonly DiagramLayoutService _sut = new();

    [Fact]
    public void Layout_leeg_project_geeft_minimale_afmeting()
    {
        var p = new ProjectModel();
        var r = _sut.Layout(p);
        Assert.Empty(r.Nodes);
        Assert.Equal(400, r.ContentWidth);
        Assert.Equal(300, r.ContentHeight);
    }

    [Fact]
    public void Layout_enkele_component_geeft_node()
    {
        var p = new ProjectModel();
        p.Components.Add(new ComponentModel { Id = 1, Name = "API", Tag = "api" });
        var r = _sut.Layout(p);
        Assert.Single(r.Nodes);
        Assert.Equal(1, r.Nodes[0].ComponentId);
        Assert.Equal("API", r.Nodes[0].Name);
    }

    [Fact]
    public void Layout_cyclus_in_stromen_geeft_geen_extreme_breedte()
    {
        var p = new ProjectModel();
        p.Components.Add(new ComponentModel { Id = 1, Name = "A", Tag = "a" });
        p.Components.Add(new ComponentModel { Id = 2, Name = "B", Tag = "b" });
        p.Components.Add(new ComponentModel { Id = 3, Name = "C", Tag = "c" });
        p.DataFlows.Add(new DataFlowModel { Id = 1, FromComponentId = 1, ToComponentId = 2 });
        p.DataFlows.Add(new DataFlowModel { Id = 2, FromComponentId = 2, ToComponentId = 3 });
        p.DataFlows.Add(new DataFlowModel { Id = 3, FromComponentId = 3, ToComponentId = 1 });

        var r = _sut.Layout(p);
        Assert.Equal(3, r.Nodes.Count);
        // Zelfde SCC → allemaal laag 0 → geen horizontale spreiding door cyclus
        Assert.True(r.ContentWidth < 600, $"Verwacht compacte breedte, kreeg {r.ContentWidth}");
    }

    [Fact]
    public void Layout_dag_keten_geeft_gespreide_lagen()
    {
        var p = new ProjectModel();
        for (var i = 1; i <= 4; i++)
            p.Components.Add(new ComponentModel { Id = i, Name = $"N{i}", Tag = "x" });
        p.DataFlows.Add(new DataFlowModel { Id = 1, FromComponentId = 1, ToComponentId = 2 });
        p.DataFlows.Add(new DataFlowModel { Id = 2, FromComponentId = 2, ToComponentId = 3 });
        p.DataFlows.Add(new DataFlowModel { Id = 3, FromComponentId = 3, ToComponentId = 4 });

        var r = _sut.Layout(p);
        var xs = r.Nodes.OrderBy(n => n.ComponentId).Select(n => n.X).ToList();
        Assert.True(xs[1] > xs[0] && xs[2] > xs[1] && xs[3] > xs[2]);
    }
}
