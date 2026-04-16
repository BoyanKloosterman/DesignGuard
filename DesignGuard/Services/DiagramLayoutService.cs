using DesignGuard.Models;

namespace DesignGuard.Services;

public sealed record DiagramNodeLayout(
    int ComponentId,
    string Name,
    double X,
    double Y,
    string Tag,
    int? TrustBoundaryId,
    bool IsEntryPoint,
    DataSensitivity DataSensitivity);

public sealed record DiagramEdgeLayout(
    int FromId,
    int ToId,
    string Label,
    string PathData,
    double LabelX,
    double LabelY);

public sealed record TrustBoundaryOverlay(
    int Id,
    string Name,
    double X,
    double Y,
    double Width,
    double Height,
    string ColorHint);

public sealed record DiagramLayoutResult(
    IReadOnlyList<DiagramNodeLayout> Nodes,
    IReadOnlyList<DiagramEdgeLayout> Edges,
    IReadOnlyList<TrustBoundaryOverlay> TrustOverlays,
    double ContentWidth,
    double ContentHeight);

/// <summary>Laag-indeling; gebruikt opgeslagen X/Y indien aanwezig.</summary>
public sealed class DiagramLayoutService
{
    private const double LayerDx = 260;
    private const double NodeDy = 108;
    private const double Margin = 48;
    private const double BoundaryPad = 22;

    public DiagramLayoutResult Layout(ProjectModel project)
    {
        var comps = project.Components.ToList();
        if (comps.Count == 0)
            return new DiagramLayoutResult(Array.Empty<DiagramNodeLayout>(), Array.Empty<DiagramEdgeLayout>(),
                Array.Empty<TrustBoundaryOverlay>(), 400, 300);

        var idSet = comps.Select(c => c.Id).ToHashSet();
        var layers = AssignLayers(comps, project.DataFlows, idSet);
        var byLayer = comps.GroupBy(c => layers.GetValueOrDefault(c.Id, 0)).OrderBy(g => g.Key).ToList();
        var positions = new Dictionary<int, (double X, double Y)>();

        foreach (var group in byLayer)
        {
            var layerIndex = group.Key;
            var nodesInLayer = group.OrderBy(c => c.Name).ToList();
            for (var i = 0; i < nodesInLayer.Count; i++)
            {
                var c = nodesInLayer[i];
                double x, y;
                if (c.VisualX is { } vx && c.VisualY is { } vy)
                {
                    x = vx;
                    y = vy;
                }
                else
                {
                    x = Margin + layerIndex * LayerDx;
                    y = Margin + i * NodeDy;
                }

                positions[c.Id] = (x, y);
            }
        }

        var nodeLayouts = comps.Select(c =>
        {
            var p = positions[c.Id];
            return new DiagramNodeLayout(c.Id, c.Name, p.X, p.Y, c.Tag, c.TrustBoundaryId, c.IsEntryPoint,
                c.StoresOrProcesses);
        }).ToList();

        var edges = new List<DiagramEdgeLayout>();
        foreach (var f in project.DataFlows)
        {
            if (!positions.TryGetValue(f.FromComponentId, out var from) ||
                !positions.TryGetValue(f.ToComponentId, out var to))
                continue;
            var (path, lx, ly) = DiagramEdgeGeometry.Build(from.X, from.Y, to.X, to.Y, f.Label);
            edges.Add(new DiagramEdgeLayout(
                f.FromComponentId,
                f.ToComponentId,
                f.Label,
                path,
                lx,
                ly));
        }

        var overlays = BuildTrustOverlays(project, nodeLayouts);
        var (cw, ch) = ComputeBounds(nodeLayouts, overlays);
        return new DiagramLayoutResult(nodeLayouts, edges, overlays, cw, ch);
    }

    private static (double W, double H) ComputeBounds(
        IReadOnlyList<DiagramNodeLayout> nodes,
        IReadOnlyList<TrustBoundaryOverlay> overlays)
    {
        var maxX = Margin + DiagramEdgeGeometry.NodeW;
        var maxY = Margin + DiagramEdgeGeometry.NodeH;
        foreach (var n in nodes)
        {
            maxX = Math.Max(maxX, n.X + DiagramEdgeGeometry.NodeW + Margin);
            maxY = Math.Max(maxY, n.Y + DiagramEdgeGeometry.NodeH + Margin);
        }

        foreach (var o in overlays)
        {
            maxX = Math.Max(maxX, o.X + o.Width + Margin);
            maxY = Math.Max(maxY, o.Y + o.Height + Margin);
        }

        return (maxX, maxY);
    }

    private static List<TrustBoundaryOverlay> BuildTrustOverlays(
        ProjectModel project,
        IReadOnlyList<DiagramNodeLayout> nodes)
    {
        var list = new List<TrustBoundaryOverlay>();
        var byTb = nodes.Where(n => n.TrustBoundaryId is not null)
            .GroupBy(n => n.TrustBoundaryId!.Value).ToList();
        var tbModels = project.TrustBoundaries.ToDictionary(t => t.Id);
        foreach (var g in byTb)
        {
            if (!tbModels.TryGetValue(g.Key, out var tb))
                continue;
            var xs = g.Select(n => n.X).ToList();
            var ys = g.Select(n => n.Y).ToList();
            var minX = xs.Min() - BoundaryPad;
            var minY = ys.Min() - BoundaryPad;
            var maxX = xs.Max() + DiagramEdgeGeometry.NodeW + BoundaryPad;
            var maxY = ys.Max() + DiagramEdgeGeometry.NodeH + BoundaryPad;
            list.Add(new TrustBoundaryOverlay(g.Key, tb.Name, minX, minY, maxX - minX, maxY - minY,
                string.IsNullOrWhiteSpace(tb.ColorHint) ? "#3B5B8C" : tb.ColorHint));
        }

        return list;
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
