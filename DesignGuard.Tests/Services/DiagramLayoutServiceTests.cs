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
}
