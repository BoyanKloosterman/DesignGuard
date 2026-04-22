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
    string StoresOrProcessesLabel);

/// <summary>Stroom in het diagram; labelposities na stagger tegen overlap.</summary>
public sealed record DiagramEdgeLayout(
    int FromId,
    int ToId,
    string Label,
    string CurvePath,
    string ArrowPath,
    double LabelCenterX,
    double LabelCenterY,
    double LabelDrawLeft,
    double LabelDrawTop,
    // Fan-offsets + label-t meegegeven zodat live-herberekening tijdens drag consistent blijft
    double LateralStart,
    double LateralEnd,
    double LabelT);

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
    /// <summary>Horizontale afstand tussen lagen; ruim genoeg zodat labels tussen twee kolommen passen.</summary>
    private const double LayerDx = 340;
    /// <summary>Verticale tussenruimte — genoeg lucht zodat labels naast/tussen rijen kunnen staan.</summary>
    private const double NodeDy = 176;
    private const double Margin = 56;
    private const double BoundaryPad = 24;
    /// <summary>Geschatte maximale label-hoogte (twee regels) voor collision-detectie.</summary>
    private const double LabelLineH = 14;

    public DiagramLayoutResult Layout(ProjectModel project)
    {
        var comps = project.Components.ToList();
        if (comps.Count == 0)
            return new DiagramLayoutResult(Array.Empty<DiagramNodeLayout>(), Array.Empty<DiagramEdgeLayout>(),
                Array.Empty<TrustBoundaryOverlay>(), 400, 300);

        var idSet = comps.Select(c => c.Id).ToHashSet();
        var rawLayers = AssignLayers(comps, project.DataFlows, idSet);
        // Opeenvolgende kolommen 0..n — voorkomt lege "spooklagen" en enorme horizontale gaten.
        var distinctRanks = rawLayers.Values.Distinct().OrderBy(v => v).ToList();
        var rankToCol = distinctRanks.Select((v, i) => (v, i)).ToDictionary(t => t.v, t => t.i);
        var layers = comps.ToDictionary(c => c.Id, c => rankToCol[rawLayers[c.Id]]);
        var byLayer = comps.GroupBy(c => layers[c.Id]).OrderBy(g => g.Key).ToList();
        var positions = new Dictionary<int, (double X, double Y)>();

        // Gerichte buurlijsten per component (voor barycenter-ordering).
        var adjFwd = comps.ToDictionary(c => c.Id, _ => new List<int>());
        var adjBack = comps.ToDictionary(c => c.Id, _ => new List<int>());
        foreach (var f in project.DataFlows)
        {
            if (!idSet.Contains(f.FromComponentId) || !idSet.Contains(f.ToComponentId)) continue;
            adjFwd[f.FromComponentId].Add(f.ToComponentId);
            adjBack[f.ToComponentId].Add(f.FromComponentId);
        }

        // Eerste plaatsing per laag: alfabetisch als stabiele startvolgorde.
        // Opgeslagen VisualY wordt bewust genegeerd — auto-layout moet deterministisch alles op
        // het raster neerzetten; drag-ankers zouden barycenter blokkeren en kruisingen behouden.
        foreach (var group in byLayer)
        {
            var col = group.Key;
            var colX = Margin + col * LayerDx;
            var list = group.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
            for (var i = 0; i < list.Count; i++)
                positions[list[i].Id] = (colX, Margin + i * NodeDy);
        }

        // Barycenter crossings-reductie (Sugiyama phase 2).
        // Afwisselend L→R (kijk naar predecessors) en R→L (kijk naar successors).
        // Meerdere rondes tot convergentie; bij gelijke barycenter-score houd je de huidige volgorde.
        for (var pass = 0; pass < 12; pass++)
        {
            var forward = pass % 2 == 0;
            var layersOrdered = forward
                ? byLayer
                : byLayer.AsEnumerable().Reverse().ToList();
            foreach (var group in layersOrdered)
            {
                var col = group.Key;
                var colX = Margin + col * LayerDx;
                var nodesInLayer = group.ToList();
                if (nodesInLayer.Count <= 1) continue;

                double Bary(int id)
                {
                    var others = forward ? adjBack[id] : adjFwd[id];
                    var ys = new List<double>();
                    foreach (var nId in others)
                    {
                        if (positions.TryGetValue(nId, out var p))
                            ys.Add(p.Y);
                    }
                    // Geen buren in de gekozen richting → huidige Y (stabiel, geen random shuffle)
                    return ys.Count > 0 ? ys.Average() : positions[id].Y;
                }

                var sorted = nodesInLayer
                    .Select(c => (c, b: Bary(c.Id), cy: positions[c.Id].Y))
                    .OrderBy(t => t.b)
                    .ThenBy(t => t.cy)
                    .ThenBy(t => t.c.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(t => t.c)
                    .ToList();

                for (var i = 0; i < sorted.Count; i++)
                    positions[sorted[i].Id] = (colX, Margin + i * NodeDy);
            }
        }

        // Na barycenter: respecteer opgeslagen VisualX/VisualY van handmatig gesleepte componenten.
        // Overige nodes blijven op hun raster-positie staan.
        foreach (var c in comps)
        {
            if (c.VisualX is double vx && c.VisualY is double vy)
                positions[c.Id] = (vx, vy);
        }

        var nodeLayouts = comps.Select(c =>
        {
            var p = positions[c.Id];
            return new DiagramNodeLayout(c.Id, c.Name, p.X, p.Y, c.Tag, c.TrustBoundaryId, c.IsEntryPoint,
                c.StoresOrProcesses);
        }).ToList();

        var flowsOk = project.DataFlows
            .Where(flow => positions.ContainsKey(flow.FromComponentId) &&
                           positions.ContainsKey(flow.ToComponentId))
            .ToList();
        var byPair = flowsOk
            .GroupBy(f => (f.FromComponentId, f.ToComponentId))
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).ToList());
        var idxInPair = new Dictionary<int, int>();
        foreach (var list in byPair.Values)
        {
            for (var i = 0; i < list.Count; i++)
                idxInPair[list[i].Id] = i;
        }

        var outgoingByFrom = flowsOk
            .GroupBy(f => f.FromComponentId)
            .ToDictionary(g => g.Key, g => g.OrderBy(f => positions[f.ToComponentId].Y).ThenBy(f => f.Id).ToList());
        var outIdx = new Dictionary<int, int>();
        foreach (var list in outgoingByFrom.Values)
        {
            for (var i = 0; i < list.Count; i++)
                outIdx[list[i].Id] = i;
        }

        var outN = outgoingByFrom.ToDictionary(g => g.Key, g => g.Value.Count);

        var incomingByTo = flowsOk
            .GroupBy(f => f.ToComponentId)
            .ToDictionary(g => g.Key, g => g.OrderBy(f => positions[f.FromComponentId].Y).ThenBy(f => f.Id).ToList());
        var inIdx = new Dictionary<int, int>();
        foreach (var list in incomingByTo.Values)
        {
            for (var i = 0; i < list.Count; i++)
                inIdx[list[i].Id] = i;
        }

        var inN = incomingByTo.ToDictionary(g => g.Key, g => g.Value.Count);

        var edges = new List<DiagramEdgeLayout>();
        foreach (var f in project.DataFlows)
        {
            if (!positions.TryGetValue(f.FromComponentId, out var from) ||
                !positions.TryGetValue(f.ToComponentId, out var to))
                continue;
            var pair = byPair[(f.FromComponentId, f.ToComponentId)];
            double lateralStart;
            double lateralEnd;
            // Label-positie langs de boog (0 = bij bron, 1 = bij doel)
            double labelT = 0.5;
            if (pair.Count > 1)
            {
                var idx = idxInPair[f.Id];
                var d = (idx - (pair.Count - 1) / 2.0) * 18;
                lateralStart = lateralEnd = d;
                // Meerdere parallelle edges bron→doel: label iets naar bron-kant, licht gespreid
                labelT = 0.3 + (idx - (pair.Count - 1) / 2.0) * 0.04;
            }
            else
            {
                var nc = outN[f.FromComponentId];
                var ic = outIdx[f.Id];
                lateralStart = nc <= 1 ? 0 : (ic - (nc - 1) / 2.0) * 18;
                var nt = inN[f.ToComponentId];
                var it = inIdx[f.Id];
                lateralEnd = nt <= 1 ? 0 : (it - (nt - 1) / 2.0) * 18;

                // Label nabij de "unieke" kant; hub-zijde is te druk om labels netjes naast elkaar te krijgen.
                // Doel is hub (nt >= nc en nt >= 2) → label bij de BRON (elke bron is uniek, dus gespreid)
                // Bron is hub (nc > nt en nc >= 2) → label bij het DOEL
                if (nt >= 2 && nt >= nc)
                    labelT = 0.27;
                else if (nc >= 2)
                    labelT = 0.73;
                else
                    labelT = 0.5;
            }

            var (curve, arrow, lcx, lcy) = DiagramEdgeGeometry.Build(
                from.X, from.Y, to.X, to.Y, f.Label, lateralStart, lateralEnd, labelT);
            var (dl, dt) = LabelDrawCoords(f.Label, lcx, lcy);
            edges.Add(new DiagramEdgeLayout(
                f.FromComponentId,
                f.ToComponentId,
                f.Label,
                curve,
                arrow,
                lcx,
                lcy,
                dl,
                dt,
                lateralStart,
                lateralEnd,
                labelT));
        }

        edges = ApplyStaggeredLabels(edges);
        edges = NudgeLabelsClearOfNodes(edges, nodeLayouts);
        edges = NudgeLabelsClearOfNodes(edges, nodeLayouts);
        var overlays = BuildTrustOverlays(project, nodeLayouts);
        var (cw, ch) = ComputeBounds(nodeLayouts, overlays, edges);
        return new DiagramLayoutResult(nodeLayouts, edges, overlays, cw, ch);
    }

    /// <summary>Geschatte halve breedte van een label-kader (incl. padding).</summary>
    private static double LabelHalfWidth(string? label)
    {
        var len = label?.Length ?? 0;
        return Math.Clamp(len * 3.4 + 14, 36, 110);
    }

    /// <summary>Geschatte hoogte van een label-kader (1 of 2 regels).</summary>
    private static double LabelHeight(string? label)
    {
        var len = label?.Length ?? 0;
        return len > 34 ? LabelLineH * 2 + 10 : LabelLineH + 8;
    }

    private static (double Left, double Top) LabelDrawCoords(string? label, double centerX, double centerY)
    {
        var halfW = LabelHalfWidth(label);
        var h = LabelHeight(label);
        return (centerX - halfW, centerY - h / 2);
    }

    /// <summary>Labels die op dezelfde plek zouden vallen verticaal uit elkaar duwen, afwisselend omhoog/omlaag.</summary>
    private static List<DiagramEdgeLayout> ApplyStaggeredLabels(IReadOnlyList<DiagramEdgeLayout> edges)
    {
        var sorted = edges
            .Select((e, i) => (e, i))
            .OrderBy(t => string.IsNullOrWhiteSpace(t.e.Label) ? 1 : 0)
            .ThenBy(t => t.e.LabelCenterX)
            .ThenBy(t => t.e.LabelCenterY)
            .ThenBy(t => t.i)
            .ToList();

        // (X, Y, HalfW, H) van reeds geplaatste labels
        var placed = new List<(double X, double Y, double HalfW, double H)>();
        var outList = new DiagramEdgeLayout[edges.Count];

        foreach (var (e, origIdx) in sorted)
        {
            if (string.IsNullOrWhiteSpace(e.Label))
            {
                var (dl, dt) = LabelDrawCoords(e.Label, e.LabelCenterX, e.LabelCenterY);
                outList[origIdx] = e with { LabelDrawLeft = dl, LabelDrawTop = dt };
                continue;
            }

            var halfW = LabelHalfWidth(e.Label);
            var myH = LabelHeight(e.Label);
            var cx = e.LabelCenterX;
            var cy = e.LabelCenterY;

            bool Overlaps(double x, double y)
            {
                foreach (var p in placed)
                {
                    var dx = Math.Abs(p.X - x);
                    var minDx = p.HalfW + halfW - 6;
                    var dy = Math.Abs(p.Y - y);
                    var minDy = (p.H + myH) / 2 - 2;
                    if (dx < minDx && dy < minDy)
                        return true;
                }
                return false;
            }

            // 1) Verticaal afwisselend omhoog/omlaag, begrensd zodat labels niet de canvas uit vliegen
            var finalX = cx;
            var finalY = cy;
            if (Overlaps(cx, cy))
            {
                var stepY = Math.Max(16, myH / 2 + 4);
                var found = false;
                for (var k = 1; k <= 8 && !found; k++)
                {
                    foreach (var sign in new[] { -1, 1 })
                    {
                        var cand = cy + sign * k * stepY;
                        if (cand < myH / 2 + 4) continue;
                        if (!Overlaps(cx, cand))
                        {
                            finalY = cand;
                            found = true;
                            break;
                        }
                    }
                }

                // 2) Als verticaal niet lukt: horizontaal opzij schuiven
                if (!found)
                {
                    var stepX = halfW + 8;
                    for (var k = 1; k <= 8 && !found; k++)
                    {
                        foreach (var sign in new[] { -1, 1 })
                        {
                            var cand = cx + sign * k * stepX;
                            if (!Overlaps(cand, cy))
                            {
                                finalX = cand;
                                found = true;
                                break;
                            }
                        }
                    }
                }
            }

            placed.Add((finalX, finalY, halfW, myH));
            var (drawL, drawT) = LabelDrawCoords(e.Label, finalX, finalY);
            outList[origIdx] = e with
            {
                LabelCenterX = finalX,
                LabelCenterY = finalY,
                LabelDrawLeft = drawL,
                LabelDrawTop = drawT
            };
        }

        return outList.ToList();
    }

    /// <summary>Labels uit node-vakken duwen (leesbare titels, minder "alles op elkaar").</summary>
    private static List<DiagramEdgeLayout> NudgeLabelsClearOfNodes(
        IReadOnlyList<DiagramEdgeLayout> edges,
        IReadOnlyList<DiagramNodeLayout> nodes)
    {
        const double pad = 10;
        var nw = DiagramEdgeGeometry.NodeW;
        var nh = DiagramEdgeGeometry.NodeH;
        var list = edges.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (string.IsNullOrWhiteSpace(e.Label))
                continue;
            var w = LabelHalfWidth(e.Label) * 2;
            var h = LabelHeight(e.Label);
            var left = e.LabelDrawLeft;
            var top = e.LabelDrawTop;

            bool Hits(double l, double t) =>
                nodes.Any(n =>
                    l < n.X + nw + pad && l + w > n.X - pad &&
                    t < n.Y + nh + pad && t + h > n.Y - pad);

            if (!Hits(left, top))
                continue;

            // Zoek dichtstbijzijnde vrije positie boven of onder, in kleine stappen,
            // begrensd tot ~150 px drift zodat labels in de buurt van hun boog blijven.
            var stepY = 10.0;
            var found = false;
            for (var step = 1; step <= 15 && !found; step++)
            {
                foreach (var sign in new[] { -1, 1 })
                {
                    var newTop = top + sign * step * stepY;
                    if (newTop < 4) continue;
                    if (!Hits(left, newTop))
                    {
                        list[i] = e with { LabelDrawTop = newTop, LabelCenterY = newTop + h / 2 };
                        found = true;
                        break;
                    }
                }
            }
        }

        return list;
    }

    private static (double W, double H) ComputeBounds(
        IReadOnlyList<DiagramNodeLayout> nodes,
        IReadOnlyList<TrustBoundaryOverlay> overlays,
        IReadOnlyList<DiagramEdgeLayout> edges)
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

        foreach (var e in edges)
        {
            if (string.IsNullOrWhiteSpace(e.Label))
                continue;
            var estW = LabelHalfWidth(e.Label) * 2;
            var estH = LabelHeight(e.Label);
            maxX = Math.Max(maxX, e.LabelDrawLeft + estW + Margin);
            maxY = Math.Max(maxY, e.LabelDrawTop + estH + Margin);
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

    /// <summary>
    /// Lagen voor links-naar-rechts layout. Oude aanpak: herhaaldelijk To := max(To, From+1).
    /// Bij een cyclus in datastromen stijgen die waarden elke ronde → extreem brede diagrammen.
    /// Oplossing: strongly connected components samenvouwen; op de acyclische condensatiegraaf
    /// één keer longest-path (topologische volgorde).
    /// </summary>
    private static Dictionary<int, int> AssignLayers(
        List<ComponentModel> comps,
        List<DataFlowModel> flows,
        HashSet<int> idSet)
    {
        var ids = comps.Select(c => c.Id).ToList();
        var adj = ids.ToDictionary(id => id, _ => new List<int>());
        foreach (var f in flows)
        {
            if (!idSet.Contains(f.FromComponentId) || !idSet.Contains(f.ToComponentId))
                continue;
            adj[f.FromComponentId].Add(f.ToComponentId);
        }

        var indexCounter = 0;
        var stack = new Stack<int>();
        var onStack = new HashSet<int>();
        var indices = ids.ToDictionary(id => id, _ => -1);
        var lowLink = ids.ToDictionary(id => id, _ => -1);
        var compToScc = new Dictionary<int, int>();
        var sccCount = 0;

        void StrongConnect(int v)
        {
            indices[v] = indexCounter;
            lowLink[v] = indexCounter;
            indexCounter++;
            stack.Push(v);
            onStack.Add(v);

            foreach (var w in adj[v])
            {
                if (indices[w] < 0)
                {
                    StrongConnect(w);
                    lowLink[v] = Math.Min(lowLink[v], lowLink[w]);
                }
                else if (onStack.Contains(w))
                {
                    lowLink[v] = Math.Min(lowLink[v], indices[w]);
                }
            }

            if (lowLink[v] != indices[v])
                return;

            while (true)
            {
                var w = stack.Pop();
                onStack.Remove(w);
                compToScc[w] = sccCount;
                if (w == v)
                    break;
            }

            sccCount++;
        }

        foreach (var id in ids)
        {
            if (indices[id] < 0)
                StrongConnect(id);
        }

        var condAdj = Enumerable.Range(0, sccCount).Select(_ => new List<int>()).ToArray();
        var condInDegree = new int[sccCount];
        var condEdgeSeen = new HashSet<(int A, int B)>();
        foreach (var f in flows)
        {
            if (!idSet.Contains(f.FromComponentId) || !idSet.Contains(f.ToComponentId))
                continue;
            var a = compToScc[f.FromComponentId];
            var b = compToScc[f.ToComponentId];
            if (a == b || !condEdgeSeen.Add((a, b)))
                continue;
            condAdj[a].Add(b);
            condInDegree[b]++;
        }

        var topo = new List<int>(sccCount);
        var indegWork = (int[])condInDegree.Clone();
        var q = new Queue<int>();
        for (var i = 0; i < sccCount; i++)
        {
            if (indegWork[i] == 0)
                q.Enqueue(i);
        }

        while (q.Count > 0)
        {
            var v = q.Dequeue();
            topo.Add(v);
            foreach (var w in condAdj[v])
            {
                indegWork[w]--;
                if (indegWork[w] == 0)
                    q.Enqueue(w);
            }
        }

        // Zou niet moeten: condensatie is altijd een DAG. Fallback: volgorde 0..n-1.
        if (topo.Count != sccCount)
        {
            topo.Clear();
            for (var i = 0; i < sccCount; i++)
                topo.Add(i);
        }

        var sccLayer = new int[sccCount];
        foreach (var v in topo)
        {
            foreach (var w in condAdj[v])
                sccLayer[w] = Math.Max(sccLayer[w], sccLayer[v] + 1);
        }

        return comps.ToDictionary(c => c.Id, c => sccLayer[compToScc[c.Id]]);
    }
}
