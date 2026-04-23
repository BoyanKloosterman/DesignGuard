using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace DesignGuard.Services;

/// <summary>Mermaid naar PNG via WebView2 (zelfde shell als live preview) voor PDF-export.</summary>
public sealed class MermaidDiagramRasterizer
{
    private static readonly Regex C4DiagramStart = new(
        @"^\s*C4(Context|Container|Component|Dynamic|Deployment)\b",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private static bool IsC4Mermaid(string? code) =>
        !string.IsNullOrWhiteSpace(code) && C4DiagramStart.IsMatch(code);

    /// <summary>Moet op de WPF UI-thread draaien (WebView2).</summary>
    public async Task<byte[]> RenderToPngAsync(string mermaidCode, CancellationToken cancellationToken = default)
    {
        if (!Application.Current.Dispatcher.CheckAccess())
            throw new InvalidOperationException("MermaidDiagramRasterizer vereist de UI-thread.");

        // Smalle viewport + latere SVG-schaal: past beter op PDF (A4) dan een brede LR-capture.
        var w = new Window
        {
            Width = 880,
            Height = 1100,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
            Left = -10000,
            Top = -10000
        };

        var webView = new WebView2();
        w.Content = webView;
        w.Show();

        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DesignGuard", "WebView2");
            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder)
                .ConfigureAwait(true);
            await webView.EnsureCoreWebView2Async(env).ConfigureAwait(true);

            var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var reg = cancellationToken.Register(() => ready.TrySetCanceled(cancellationToken));

            void OnMessage(object? _, CoreWebView2WebMessageReceivedEventArgs e)
            {
                if (WebView2MessageJson.TryParse(e.WebMessageAsJson, out string? kind, out string? _) &&
                    kind == "ready")
                    ready.TrySetResult(true);
            }

            webView.CoreWebView2.WebMessageReceived += OnMessage;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

            webView.NavigateToString(MermaidViewerHtmlLoader.Load());

            await ready.Task.WaitAsync(TimeSpan.FromSeconds(60), cancellationToken).ConfigureAwait(true);

            // Toolbar weglaten op de PNG
            await webView.CoreWebView2.ExecuteScriptAsync(
                "document.getElementById('toolbar').style.display='none';").ConfigureAwait(true);

            var codeJson = JsonSerializer.Serialize(mermaidCode ?? string.Empty);
            var script =
                "(async () => { await window.renderMermaid(" + codeJson + "); " +
                "var e = document.getElementById('err'); " +
                "if (e && e.style.display === 'block') throw new Error(e.textContent || 'Mermaid'); })()";

            await webView.CoreWebView2.ExecuteScriptAsync(script).ConfigureAwait(true);

            await Task.Delay(200, cancellationToken).ConfigureAwait(true);

            // C4: sizeC4Svg in de HTML zet breedte/hoogte; extra transform hier breekt dat af.
            if (!IsC4Mermaid(mermaidCode))
            {
                // Flowchart: SVG passend binnen export-paneel (voorkomt extreem brede PNG).
                await webView.CoreWebView2.ExecuteScriptAsync(
                    "(function(){var s=document.querySelector('#container svg'),c=document.getElementById('container');" +
                    "if(!s||!c)return;s.style.transform='';s.style.transformOrigin='top left';" +
                    "var pad=20,b;try{b=s.getBBox();}catch(e){return;}if(b.width<2||b.height<2)return;" +
                    "var cw=c.clientWidth-pad,ch=c.clientHeight-pad,sc=Math.min(1,cw/b.width,ch/b.height);" +
                    "if(sc<1)s.style.transform='scale('+sc+')';})();").ConfigureAwait(true);

                await Task.Delay(80, cancellationToken).ConfigureAwait(true);
            }
            else
                await Task.Delay(120, cancellationToken).ConfigureAwait(true);

            await using var ms = new MemoryStream();
            await webView.CoreWebView2
                .CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, ms)
                .ConfigureAwait(true);

            return ms.ToArray();
        }
        finally
        {
            w.Close();
        }
    }
}
