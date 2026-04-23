// Mermaid-generatie vanuit het editor-model voor live preview in de WebView2.
using CommunityToolkit.Mvvm.Input;
using DesignGuard.Services;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
    private static readonly MermaidDiagramBuilder _mermaidBuilder = new();

    /// <summary>Regenereert MermaidCode op basis van het huidige editor-model.</summary>
    private void RefreshDiagram()
    {
        try
        {
            var m = BuildModelFromEditor();
            MermaidCode = _mermaidBuilder.Build(m);
        }
        catch (Exception ex)
        {
            // Faalt nooit de editor; rapporteer in het foutvak
            MermaidSyntaxError = "Kan Mermaid niet genereren: " + ex.Message;
        }
    }

    [RelayCommand]
    private void RefreshDiagramLayout() => RefreshDiagram();

    /// <summary>Gooit eventuele handmatig getypte Mermaid-code weg en rebuild vanuit model.</summary>
    [RelayCommand]
    private void ResetDiagramPositions() => RefreshDiagram();

    partial void OnMermaidSyntaxErrorChanged(string value)
    {
        HasMermaidError = !string.IsNullOrWhiteSpace(value);
    }
}
