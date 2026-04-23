using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>C4-weergave in Mermaid (C4Context / C4Container / C4Component) uit project.C4Elements.</summary>
public enum C4MermaidBand
{
    Context,
    Container,
    Component,
    Code
}

public sealed class C4MermaidDiagramBuilder
{
    private static readonly Regex PersonHints = new(
        @"patiënt|patient|gebruiker|arts|beheerder|admin|persoon|eindgebruiker|manager|klant|customer|operator|saas|verpleeg|zorgverlener|ziekenhuis",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public string Build(C4MermaidBand band, ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return band switch
        {
            C4MermaidBand.Context => BuildContext(project),
            C4MermaidBand.Container => BuildContainer(project),
            C4MermaidBand.Component => BuildComponent(project),
            C4MermaidBand.Code => BuildCode(project),
            _ => BuildContext(project)
        };
    }

    private static string BuildContext(ProjectModel p)
    {
        var ctx = p.C4Elements.Where(e => e.Level == C4Level.Context).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("C4Context");
        sb.AppendLine("title C1 - Context - " + EscapeTitle(p.Name));
        var scopeName = string.IsNullOrWhiteSpace(p.SystemName) ? p.Name : p.SystemName.Trim();
        if (string.IsNullOrWhiteSpace(scopeName)) scopeName = "Systeem";
        sb.AppendLine($"System(SysInScope, {Q(scopeName)}, {Q(TrimDesc(p.Description))})");
        foreach (var c in ContextElementsOrderedForLayout(ctx))
        {
            if (string.IsNullOrWhiteSpace(c.Name)) continue;
            if (LooksLikePerson(c))
                sb.AppendLine($"Person({Alias(c)}, {Q(c.Name.Trim())}, {Q(TrimDesc(c.Description))})");
            else
                sb.AppendLine($"System_Ext({Alias(c)}, {Q(c.Name.Trim())}, {Q(TrimDesc(c.Description))})");
        }

        AppendRelationStatements(sb, C4MermaidBand.Context, p);
        return sb.ToString();
    }

    private static string BuildContainer(ProjectModel p)
    {
        var ctx = p.C4Elements.Where(e => e.Level == C4Level.Context).ToList();
        var ctr = p.C4Elements.Where(e => e.Level == C4Level.Container).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("C4Container");
        sb.AppendLine("title C2 - Containers - " + EscapeTitle(p.Name));
        foreach (var c in ContextElementsOrderedForLayout(ctx))
        {
            if (string.IsNullOrWhiteSpace(c.Name)) continue;
            if (LooksLikePerson(c))
                sb.AppendLine($"Person({Alias(c)}, {Q(c.Name.Trim())}, {Q(TrimDesc(c.Description))})");
            else
                sb.AppendLine($"System_Ext({Alias(c)}, {Q(c.Name.Trim())}, {Q(TrimDesc(c.Description))})");
        }

        var boundaryLabel = string.IsNullOrWhiteSpace(p.SystemName) ? p.Name.Trim() : p.SystemName.Trim();
        if (string.IsNullOrWhiteSpace(boundaryLabel)) boundaryLabel = "Systeem";
        boundaryLabel += " - containers";
        sb.AppendLine($"System_Boundary(BndMain, {Q(boundaryLabel)}) {{");
        var emittedCtr = false;
        foreach (var c in OrderedContainersForLayout(ctr))
        {
            if (string.IsNullOrWhiteSpace(c.Name)) continue;
            AppendContainerStatement(sb, c, "  ");
            emittedCtr = true;
        }

        // Lege boundary breekt de Mermaid C4-parser (verwacht elementen, geen directe })
        if (!emittedCtr)
            sb.AppendLine($"  Container(PhCtr, {Q("(geen C2-rijen)")}, \"\", {Q("Voeg container-niveau toe in de tabel")})");

        sb.AppendLine("}");
        AppendRelationStatements(sb, C4MermaidBand.Container, p);
        return sb.ToString();
    }

    private static string BuildComponent(ProjectModel p)
    {
        var containers = p.C4Elements.Where(e => e.Level == C4Level.Container).ToList();
        var components = p.C4Elements.Where(e => e.Level == C4Level.Component).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("C4Component");
        sb.AppendLine("title C3 - Components - " + EscapeTitle(p.Name));
        var any = false;
        foreach (var cont in containers.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(cont.Name)) continue;
            var children = components.Where(x => x.ParentId == cont.Id).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
            if (children.Count == 0) continue;
            any = true;
            sb.AppendLine($"Container_Boundary({Alias(cont)}, {Q(cont.Name.Trim())}) {{");
            foreach (var ch in children)
            {
                if (string.IsNullOrWhiteSpace(ch.Name)) continue;
                AppendComponentStatement(sb, ch, "  ");
            }

            sb.AppendLine("}");
        }

        // Na '{' verplicht newline vóór eerste statement (Mermaid C4-parser)
        if (!any)
        {
            sb.AppendLine($"Container_Boundary(BPh, {Q("(geen C3-koppelingen)")}) {{");
            sb.AppendLine($"  Component(PhC3, {Q("Koppel componenten aan een C2-container via Parent id")}, \"\", \"\")");
            sb.AppendLine("}");
        }

        AppendRelationStatements(sb, C4MermaidBand.Component, p);
        return sb.ToString();
    }

    private static string BuildCode(ProjectModel p)
    {
        var comps = p.C4Elements.Where(e => e.Level == C4Level.Component).ToList();
        var codes = p.C4Elements.Where(e => e.Level == C4Level.Code).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("C4Component");
        sb.AppendLine("title C4 - Code - " + EscapeTitle(p.Name));
        var any = false;
        foreach (var comp in comps.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(comp.Name)) continue;
            var children = codes.Where(x => x.ParentId == comp.Id).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
            if (children.Count == 0) continue;
            any = true;
            sb.AppendLine($"Container_Boundary({Alias(comp)}, {Q(comp.Name.Trim())}) {{");
            foreach (var ch in children)
            {
                if (string.IsNullOrWhiteSpace(ch.Name)) continue;
                AppendComponentStatement(sb, ch, "  ");
            }

            sb.AppendLine("}");
        }

        if (!any)
        {
            sb.AppendLine($"Container_Boundary(BPh4, {Q("(geen C4-rijen)")}) {{");
            sb.AppendLine($"  Component(PhC4, {Q("Voeg code-niveau toe met Parent id = C3-component")}, \"\", \"\")");
            sb.AppendLine("}");
        }

        AppendRelationStatements(sb, C4MermaidBand.Code, p);
        return sb.ToString();
    }

    /// <summary>Rel(...) alleen als beide eindpunten in dit diagram voorkomen (zelfde regels als elementen).</summary>
    private static void AppendRelationStatements(StringBuilder sb, C4MermaidBand band, ProjectModel p)
    {
        var rels = p.C4Relations;
        if (rels == null || rels.Count == 0) return;

        var valid = CollectAliasesForBand(band, p);
        if (valid.Count == 0) return;

        var resolved = new List<(C4RelationModel rel, string a, string b)>();
        foreach (var rel in rels)
        {
            if (ShouldSkipRelationForMermaid11(rel, band, p)) continue;
            var a = ResolveRelationEndpointAlias(rel.FromElementId, band, p, valid);
            var b = ResolveRelationEndpointAlias(rel.ToElementId, band, p, valid);
            if (a == null || b == null || a == b) continue;
            resolved.Add((rel, a, b));
        }

        if (resolved.Count == 0) return;

        IEnumerable<(C4RelationModel rel, string a, string b)> ordered = band switch
        {
            C4MermaidBand.Container => resolved
                .OrderBy(t => DiagramRelSortKey(t.rel.FromElementId, t.rel.ToElementId, band, p))
                .ThenBy(t => t.rel.Id),
            _ => resolved
                .OrderBy(t => t.rel.Id)
                .ThenBy(t => t.rel.Label, StringComparer.OrdinalIgnoreCase)
        };

        foreach (var (rel, a, b) in ordered)
        {
            var lbl = string.IsNullOrWhiteSpace(rel.Label) ? " " : rel.Label.Trim();
            sb.AppendLine($"{RelKeyword(rel.LineKind)}({a}, {b}, {Q(lbl)})");
        }
    }

    /// <summary>Alleen Rel: Rel_U/D/L/R triggert in Mermaid 11.14+ soms generieke parse-fouten bij C4.</summary>
    private static string RelKeyword(C4MermaidRelLineKind _) => "Rel";

    /// <summary>Geen Rel tussen parent-container en child-component/code: Mermaid 11 C4-parser faalt daar vaak op.</summary>
    private static bool ShouldSkipRelationForMermaid11(C4RelationModel rel, C4MermaidBand band, ProjectModel p)
    {
        if (band is not (C4MermaidBand.Component or C4MermaidBand.Code)) return false;
        if (rel.FromElementId == 0 || rel.ToElementId == 0) return false;
        var a = p.C4Elements.FirstOrDefault(e => e.Id == rel.FromElementId);
        var b = p.C4Elements.FirstOrDefault(e => e.Id == rel.ToElementId);
        if (a == null || b == null) return false;

        if (band == C4MermaidBand.Component)
        {
            if (a.Level == C4Level.Container && b.Level == C4Level.Component && b.ParentId == a.Id) return true;
            if (b.Level == C4Level.Container && a.Level == C4Level.Component && a.ParentId == b.Id) return true;
        }

        if (band == C4MermaidBand.Code)
        {
            if (a.Level == C4Level.Component && b.Level == C4Level.Code && b.ParentId == a.Id) return true;
            if (b.Level == C4Level.Component && a.Level == C4Level.Code && a.ParentId == b.Id) return true;
        }

        return false;
    }

    /// <summary>Personen eerst, daarna externe systemen — stabielere rasterplaatsing.</summary>
    private static IEnumerable<C4ElementModel> ContextElementsOrderedForLayout(IEnumerable<C4ElementModel> ctx) =>
        ctx.Where(e => !string.IsNullOrWhiteSpace(e.Name))
            .OrderBy(e => LooksLikePerson(e) ? 0 : 1)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>C2: volgorde rijen — SPA, gateway, cache, services, data — i.p.v. alfabetisch (minder spaghetti).</summary>
    private static IEnumerable<C4ElementModel> OrderedContainersForLayout(List<C4ElementModel> ctr)
    {
        int Tier(C4ElementModel c)
        {
            var nm = c.Name;
            if (nm.Contains("SPA", StringComparison.OrdinalIgnoreCase)) return 0;
            if (nm.Contains("gateway", StringComparison.OrdinalIgnoreCase)) return 1;
            if (nm.Contains("Redis", StringComparison.OrdinalIgnoreCase)) return 2;
            if (nm.Contains("Shop-service", StringComparison.OrdinalIgnoreCase)) return 3;
            if (nm.Contains("Admin-service", StringComparison.OrdinalIgnoreCase)) return 4;
            var k = GuessContainerKind(c);
            if (k == ContainerKind.Db) return 5;
            if (k == ContainerKind.Queue) return 5;
            return 6;
        }

        int SpaOrder(C4ElementModel c)
        {
            if (c.Name.Contains("Shop SPA", StringComparison.OrdinalIgnoreCase)) return 0;
            if (c.Name.Contains("Admin SPA", StringComparison.OrdinalIgnoreCase)) return 1;
            return 2;
        }

        return ctr
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .OrderBy(Tier)
            .ThenBy(SpaOrder)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Relaties in laag-volgorde: actoren → frontends → gateway → services → data → extern.</summary>
    private static int DiagramRelSortKey(int fromId, int toId, C4MermaidBand band, ProjectModel p)
    {
        if (band != C4MermaidBand.Container)
            return 0;

        var rf = EndpointRankForContainerDiagram(fromId, p);
        var rt = EndpointRankForContainerDiagram(toId, p);
        return rf * 32 + rt;
    }

    private static int EndpointRankForContainerDiagram(int elementId, ProjectModel p)
    {
        if (elementId == 0) return 20;
        var el = p.C4Elements.FirstOrDefault(e => e.Id == elementId);
        if (el == null) return 99;
        if (el.Level == C4Level.Context)
            return LooksLikePerson(el) ? 0 : 18;
        if (el.Level != C4Level.Container) return 12;
        if (el.Name.Contains("SPA", StringComparison.OrdinalIgnoreCase)) return 2;
        if (el.Name.Contains("gateway", StringComparison.OrdinalIgnoreCase)) return 4;
        if (el.Name.Contains("Redis", StringComparison.OrdinalIgnoreCase)) return 5;
        if (el.Name.Contains("service", StringComparison.OrdinalIgnoreCase)) return 6;
        var k = GuessContainerKind(el);
        if (k is ContainerKind.Db or ContainerKind.Queue) return 10;
        return 8;
    }

    private static HashSet<string> CollectAliasesForBand(C4MermaidBand band, ProjectModel p)
    {
        var set = new HashSet<string>();

        switch (band)
        {
            case C4MermaidBand.Context:
                set.Add("SysInScope");
                foreach (var c in p.C4Elements.Where(e => e.Level == C4Level.Context && !string.IsNullOrWhiteSpace(e.Name)))
                    set.Add(Alias(c));
                break;
            case C4MermaidBand.Container:
                foreach (var c in p.C4Elements.Where(e =>
                             e.Level == C4Level.Context && !string.IsNullOrWhiteSpace(e.Name)))
                    set.Add(Alias(c));
                foreach (var c in p.C4Elements.Where(e =>
                             e.Level == C4Level.Container && !string.IsNullOrWhiteSpace(e.Name)))
                    set.Add(Alias(c));
                break;
            case C4MermaidBand.Component:
                var containers = p.C4Elements.Where(e => e.Level == C4Level.Container).ToList();
                var components = p.C4Elements.Where(e => e.Level == C4Level.Component).ToList();
                foreach (var cont in containers)
                {
                    if (string.IsNullOrWhiteSpace(cont.Name)) continue;
                    var children = components.Where(x => x.ParentId == cont.Id && !string.IsNullOrWhiteSpace(x.Name))
                        .ToList();
                    if (children.Count == 0) continue;
                    set.Add(Alias(cont));
                    foreach (var ch in children)
                        set.Add(Alias(ch));
                }

                break;
            case C4MermaidBand.Code:
                var comps = p.C4Elements.Where(e => e.Level == C4Level.Component).ToList();
                var codes = p.C4Elements.Where(e => e.Level == C4Level.Code).ToList();
                foreach (var comp in comps)
                {
                    if (string.IsNullOrWhiteSpace(comp.Name)) continue;
                    var children = codes.Where(x => x.ParentId == comp.Id && !string.IsNullOrWhiteSpace(x.Name)).ToList();
                    if (children.Count == 0) continue;
                    set.Add(Alias(comp));
                    foreach (var ch in children)
                        set.Add(Alias(ch));
                }

                break;
        }

        return set;
    }

    private static string? ResolveRelationEndpointAlias(int elementId, C4MermaidBand band, ProjectModel p,
        HashSet<string> valid)
    {
        if (elementId == 0)
        {
            if (band != C4MermaidBand.Context) return null;
            return valid.Contains("SysInScope") ? "SysInScope" : null;
        }

        var el = p.C4Elements.FirstOrDefault(e => e.Id == elementId);
        if (el == null || string.IsNullOrWhiteSpace(el.Name)) return null;

        var a = Alias(el);
        return valid.Contains(a) ? a : null;
    }

    private static void AppendContainerStatement(StringBuilder sb, C4ElementModel c, string indent)
    {
        var line = GuessContainerKind(c) switch
        {
            ContainerKind.Db => $"ContainerDb({Alias(c)}, {Q(c.Name.Trim())}, {Q(c.Technology)}, {Q(TrimDesc(c.Description))})",
            ContainerKind.Queue => $"ContainerQueue({Alias(c)}, {Q(c.Name.Trim())}, {Q(c.Technology)}, {Q(TrimDesc(c.Description))})",
            _ => $"Container({Alias(c)}, {Q(c.Name.Trim())}, {Q(c.Technology)}, {Q(TrimDesc(c.Description))})"
        };
        sb.Append(indent).AppendLine(line);
    }

    private static void AppendComponentStatement(StringBuilder sb, C4ElementModel c, string indent)
    {
        var line = GuessContainerKind(c) switch
        {
            ContainerKind.Db => $"ComponentDb({Alias(c)}, {Q(c.Name.Trim())}, {Q(c.Technology)}, {Q(TrimDesc(c.Description))})",
            ContainerKind.Queue => $"ComponentQueue({Alias(c)}, {Q(c.Name.Trim())}, {Q(c.Technology)}, {Q(TrimDesc(c.Description))})",
            _ => $"Component({Alias(c)}, {Q(c.Name.Trim())}, {Q(c.Technology)}, {Q(TrimDesc(c.Description))})"
        };
        sb.Append(indent).AppendLine(line);
    }

    private enum ContainerKind { App, Db, Queue }

    private static ContainerKind GuessContainerKind(C4ElementModel c)
    {
        var s = $"{c.Name} {c.Technology} {c.Description}";
        if (string.IsNullOrWhiteSpace(s)) return ContainerKind.App;
        if (QueueHints.IsMatch(s)) return ContainerKind.Queue;
        if (DbHints.IsMatch(s)) return ContainerKind.Db;
        return ContainerKind.App;
    }

    private static readonly Regex DbHints = new(
        @"postgres|sql|database|db\b|mongo|redis|datastore|jdbc|pgcrypto|sqlite|mysql|mariadb",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex QueueHints = new(
        @"rabbit|kafka|amqp|queue|message broker|service ?bus|mq\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static bool LooksLikePerson(C4ElementModel el)
    {
        var s = $"{el.Name} {el.Description} {el.Technology}";
        return !string.IsNullOrWhiteSpace(s) && PersonHints.IsMatch(s);
    }

    private static string Alias(C4ElementModel c) =>
        "E" + c.Id.ToString(CultureInfo.InvariantCulture);

    private static string Q(string? s)
    {
        var t = string.IsNullOrEmpty(s) ? " " : s.Trim();
        t = t.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
        return "\"" + t + "\"";
    }

    private static string TrimDesc(string? s) =>
        string.IsNullOrWhiteSpace(s) ? " " : s.Trim();

    /// <summary>Mermaid title-regel: lexer staat # ; en newline niet toe in het titel-deel.</summary>
    private static string EscapeTitle(string? name)
    {
        var t = string.IsNullOrWhiteSpace(name) ? "Project" : name.Trim();
        return t.Replace("\"", "'", StringComparison.Ordinal)
            .Replace("\u2014", "-", StringComparison.Ordinal)
            .Replace("\u2013", "-", StringComparison.Ordinal)
            .Replace("#", string.Empty, StringComparison.Ordinal)
            .Replace(";", ",", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
    }
}
