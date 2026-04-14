using DesignGuard.Models;

namespace DesignGuard.Services;

public sealed record DiagramNodeLayout(int ComponentId, string Name, double X, double Y, string Tag);

public sealed record DiagramEdgeLayout(
    int FromId,
    int ToId,
    string Label,
    double FromX,
    double FromY,
    double ToX,
    double ToY);

public sealed record DiagramLayoutResult(
    IReadOnlyList<DiagramNodeLayout> Nodes,
    IReadOnlyList<DiagramEdgeLayout> Edges);

/// <summary>
/// Eenvoudige laag-indeling op basis van datastromen (geen zware graph-layout lib).
/// </summary>
public sealed class DiagramLayoutService
{
    private const double LayerDx = 220;
    private const double NodeDy = 100;
    private const double Margin = 40;

    public DiagramLayoutResult Layout(ProjectModel project)
    {
        var comps = project.Components.ToList();
        if (comps.Count == 0)
            return new DiagramLayoutResult(Array.Empty<DiagramNodeLayout>(), Array.Empty<DiagramEdgeLayout>());

        var idSet = comps.Select(c => c.Id).ToHashSet();
        var layers = AssignLayers(comps, project.DataFlows, idSet);
        var maxLayer = layers.Values.DefaultIfEmpty(0).Max();

        var byLayer = comps.GroupBy(c => layers.GetValueOrDefault(c.Id, 0)).OrderBy(g => g.Key).ToList();
        var positions = new Dictionary<int, (double X, double Y)>();

        foreach (var group in byLayer)
        {
            var layerIndex = group.Key;
            var nodesInLayer = group.OrderBy(c => c.Name).ToList();
            for (var i = 0; i < nodesInLayer.Count; i++)
            {
                var c = nodesInLayer[i];
                var x = Margin + layerIndex * LayerDx;
                var y = Margin + i * NodeDy;
                positions[c.Id] = (x, y);
            }
        }

        var nodeLayouts = comps.Select(c =>
        {
            var p = positions[c.Id];
            return new DiagramNodeLayout(c.Id, c.Name, p.X, p.Y, c.Tag);
        }).ToList();

        var edges = new List<DiagramEdgeLayout>();
        foreach (var f in project.DataFlows)
        {
            if (!positions.TryGetValue(f.FromComponentId, out var from) ||
                !positions.TryGetValue(f.ToComponentId, out var to))
                continue;
            edges.Add(new DiagramEdgeLayout(
                f.FromComponentId,
                f.ToComponentId,
                f.Label,
                from.X + 80,
                from.Y + 24,
                to.X,
                to.Y + 24));
        }

        return new DiagramLayoutResult(nodeLayouts, edges);
    }

    private static Dictionary<int, int> AssignLayers(
        List<ComponentModel> comps,
        List<DataFlowModel> flows,
        HashSet<int> idSet)
    {
        var layers = comps.ToDictionary(c => c.Id, _ => 0);
        var edges = flows
            .Where(f => idSet.Contains(f.FromComponentId) && idSet.Contains(f.ToComponentId))
            .ToList();

        // Meerdere relaxatie-rondes: layer[to] = max(layer[from]) + 1
        for (var pass = 0; pass < comps.Count + 2; pass++)
        {
            foreach (var f in edges)
            {
                var lf = layers[f.FromComponentId];
                var next = lf + 1;
                if (layers[f.ToComponentId] < next)
                    layers[f.ToComponentId] = next;
            }
        }

        return layers;
    }
}
