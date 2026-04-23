using System.IO;
using System.Windows;

namespace DesignGuard.Services;

/// <summary>Laadt de embedded Mermaid WebView2-shell (zelfde resource als ArchitectureDiagramPanel).</summary>
internal static class MermaidViewerHtmlLoader
{
    public static string Load()
    {
        var uri = new Uri("pack://application:,,,/Resources/MermaidViewer.html", UriKind.Absolute);
        var info = Application.GetResourceStream(uri)
            ?? throw new InvalidOperationException("MermaidViewer.html resource niet gevonden.");
        using var reader = new StreamReader(info.Stream);
        return reader.ReadToEnd();
    }
}
