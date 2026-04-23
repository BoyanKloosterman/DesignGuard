using System.Globalization;
using System.Text;
using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>Bouwt Mermaid flowchart-code uit een ProjectModel voor live preview in WebView2.</summary>
public sealed class MermaidDiagramBuilder
{
    /// <summary>Genereert een flowchart LR met subgraphs per trust boundary, componenten en data flows.</summary>
    public string Build(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var sb = new StringBuilder();
        sb.AppendLine("flowchart LR");

        // Algemene stijl-klassen voor entry points en gevoelige data-componenten
        sb.AppendLine("    classDef entry fill:#ecfdf5,stroke:#10b981,stroke-width:2px,color:#065f46;");
        sb.AppendLine("    classDef sensitive fill:#fffbeb,stroke:#d97706,stroke-width:2px,color:#7c2d12;");
        sb.AppendLine("    classDef normal fill:#ffffff,stroke:#475569,stroke-width:1px,color:#1f2937;");

        // Leeg model: placeholder node zodat Mermaid niet leeg hoeft te renderen.
        if (project.Components.Count == 0)
        {
            sb.AppendLine("    empty[\"(geen componenten gedefinieerd)\"]");
            return sb.ToString();
        }

        var componentsById = project.Components.ToDictionary(c => c.Id);
        var componentsByTbId = project.Components
            .GroupBy(c => c.TrustBoundaryId ?? 0)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Subgraph per trust boundary
        foreach (var tb in project.TrustBoundaries)
        {
            if (!componentsByTbId.TryGetValue(tb.Id, out var members) || members.Count == 0)
                continue;

            var tbNodeId = "TB_" + tb.Id.ToString(CultureInfo.InvariantCulture);
            sb.Append("    subgraph ").Append(tbNodeId).Append('[').Append(EscapeLabel(tb.Name)).AppendLine("]");
            sb.AppendLine("    direction LR");
            foreach (var c in members)
                sb.AppendLine(BuildComponentLine(c));
            sb.AppendLine("    end");
        }

        // Componenten zonder trust boundary direct op top-level plaatsen
        if (componentsByTbId.TryGetValue(0, out var orphans))
        {
            foreach (var c in orphans)
                sb.AppendLine(BuildComponentLine(c));
        }

        // Data flows als pijlen met optioneel label
        foreach (var f in project.DataFlows)
        {
            if (!componentsById.ContainsKey(f.FromComponentId) || !componentsById.ContainsKey(f.ToComponentId))
                continue;
            var from = "C" + f.FromComponentId.ToString(CultureInfo.InvariantCulture);
            var to = "C" + f.ToComponentId.ToString(CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(f.Label))
                sb.Append("    ").Append(from).Append(" --> ").AppendLine(to);
            else
                sb.Append("    ").Append(from).Append(" -- \"").Append(EscapeEdgeLabel(f.Label)).Append("\" --> ").AppendLine(to);
        }

        // Component-classes toewijzen voor visuele accenten
        foreach (var c in project.Components)
        {
            var cid = "C" + c.Id.ToString(CultureInfo.InvariantCulture);
            if (c.IsEntryPoint)
                sb.Append("    class ").Append(cid).AppendLine(" entry;");
            else if (DesignOntwerpWaarden.IsDataSensitivityVisuallyElevated(c.StoresOrProcesses))
                sb.Append("    class ").Append(cid).AppendLine(" sensitive;");
            else
                sb.Append("    class ").Append(cid).AppendLine(" normal;");
        }

        return sb.ToString();
    }

    private static string BuildComponentLine(ComponentModel c)
    {
        // Node-tekst: naam + optioneel tag-regel voor snelle herkenning
        var id = "C" + c.Id.ToString(CultureInfo.InvariantCulture);
        var label = EscapeLabel(string.IsNullOrWhiteSpace(c.Tag)
            ? c.Name
            : c.Name + "<br/><small><i>" + c.Tag + "</i></small>");
        return "        " + id + "[\"" + label + "\"]";
    }

    private static string EscapeLabel(string text)
    {
        // Mermaid breekt bij ongeëscapeerde quotes en hash-tekens binnen labels
        return (text ?? string.Empty)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("#", "&#35;", StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", "<br/>", StringComparison.Ordinal);
    }

    private static string EscapeEdgeLabel(string text)
    {
        // Edge-label zit tussen quotes, dus dezelfde quote-escape en geen newlines
        return (text ?? string.Empty)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("#", "&#35;", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
    }
}
