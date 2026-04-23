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
        foreach (var c in ctx.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
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
        foreach (var c in ctx.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
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
        foreach (var c in ctr.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
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

        foreach (var rel in rels.OrderBy(r => r.Id).ThenBy(r => r.Label, StringComparer.OrdinalIgnoreCase))
        {
            var a = ResolveRelationEndpointAlias(rel.FromElementId, band, p, valid);
            var b = ResolveRelationEndpointAlias(rel.ToElementId, band, p, valid);
            if (a == null || b == null || a == b) continue;

            var lbl = string.IsNullOrWhiteSpace(rel.Label) ? " " : rel.Label.Trim();
            sb.AppendLine($"Rel({a}, {b}, {Q(lbl)})");
        }
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

    private static string EscapeTitle(string? name)
    {
        var t = string.IsNullOrWhiteSpace(name) ? "Project" : name.Trim();
        return t.Replace("\"", "'", StringComparison.Ordinal)
            .Replace("\u2014", "-", StringComparison.Ordinal)
            .Replace("\u2013", "-", StringComparison.Ordinal);
    }
}
