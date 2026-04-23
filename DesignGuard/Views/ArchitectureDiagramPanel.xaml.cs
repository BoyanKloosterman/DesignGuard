using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using DesignGuard.Services;
using DesignGuard.ViewModels;
using Microsoft.Web.WebView2.Core;

namespace DesignGuard.Views;

/// <summary>UserControl met Mermaid-editor links en live WebView2-preview rechts.</summary>
public partial class ArchitectureDiagramPanel : UserControl
{
    // Debouncer: voorkomt een render bij elke toetsaanslag; render pas na korte pauze.
    private readonly DispatcherDebouncer _renderDebounce = new(TimeSpan.FromMilliseconds(300));

    // JavaScript-runtime pas klaar zodra de HTML-shell de 'ready'-boodschap post.
    private bool _webViewReady;
    private bool _webViewInitStarted;

    // Laatst bekende code; klaargezet totdat de WebView klaar is om te renderen.
    private string _pendingCode = string.Empty;

    // Houdt de DataContext-property-changed aan de lijn voor subscribe/unsubscribe
    private INotifyPropertyChanged? _subscribedVm;

    public ArchitectureDiagramPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_webViewInitStarted) return;
        _webViewInitStarted = true;

        try
        {
            // Expliciete user-data-folder in schrijfbare map: voorkomt E_ACCESSDENIED wanneer
            // de app vanuit een read-only locatie draait (Program Files, publish-folder, etc.).
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DesignGuard", "WebView2");
            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder);
            await PreviewWebView.EnsureCoreWebView2Async(env);

            PreviewWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            PreviewWebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            // Rechts-klik menu, dev-tools etc. in normale app uitzetten
            PreviewWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            PreviewWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;

            PreviewWebView.NavigateToString(MermaidViewerHtmlLoader.Load());
        }
        catch (Exception ex)
        {
            // Falen van WebView2 mag de app niet crashen; toon fout in het foutvak.
            if (DataContext is MainViewModel vm)
                vm.MermaidSyntaxError = "WebView2 kon niet starten: " + ex.Message;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnsubscribeFromVm();
        if (PreviewWebView?.CoreWebView2 != null)
        {
            PreviewWebView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            PreviewWebView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
        }
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        // Vangnet: ook direct na navigatie een render triggeren zodat de eerste paint niet afhangt
        // van volgorde van ready-message vs VM-binding.
        _webViewReady = true;
        var current = (DataContext as MainViewModel)?.MermaidCode ?? _pendingCode;
        _pendingCode = current ?? string.Empty;
        _ = RenderAsync(_pendingCode);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UnsubscribeFromVm();
        if (DataContext is INotifyPropertyChanged npc)
        {
            _subscribedVm = npc;
            npc.PropertyChanged += OnVmPropertyChanged;
        }
        // Direct na binding een eerste render-trigger zetten
        if (DataContext is MainViewModel vm)
            ScheduleRender(vm.MermaidCode);
    }

    private void UnsubscribeFromVm()
    {
        if (_subscribedVm != null)
        {
            _subscribedVm.PropertyChanged -= OnVmPropertyChanged;
            _subscribedVm = null;
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.MermaidCode)) return;
        if (DataContext is not MainViewModel vm) return;
        ScheduleRender(vm.MermaidCode);
    }

    private void ScheduleRender(string code)
    {
        _pendingCode = code ?? string.Empty;
        _renderDebounce.Trigger(() =>
        {
            if (_webViewReady)
                _ = RenderAsync(_pendingCode);
        });
    }

    private async System.Threading.Tasks.Task RenderAsync(string code)
    {
        if (PreviewWebView.CoreWebView2 == null) return;
        // Mermaid-code via JSON veilig naar JavaScript tillen (escapet quotes/newlines).
        var jsArg = JsonSerializer.Serialize(code ?? string.Empty);
        try
        {
            await PreviewWebView.CoreWebView2.ExecuteScriptAsync("window.renderMermaid(" + jsArg + ")");
        }
        catch (Exception ex)
        {
            if (DataContext is MainViewModel vm)
                vm.MermaidSyntaxError = "Render mislukt: " + ex.Message;
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // WebMessageAsJson bevat al JSON; parse en dispatch naar UI-thread.
        try
        {
            if (!WebView2MessageJson.TryParse(e.WebMessageAsJson, out var type, out var message))
                return;
            switch (type)
            {
                case "ready":
                    _webViewReady = true;
                    // Lees de meest recente MermaidCode direct uit de VM, niet uit een oude _pendingCode.
                    var currentCode = (DataContext as MainViewModel)?.MermaidCode ?? _pendingCode;
                    _pendingCode = currentCode ?? string.Empty;
                    _ = RenderAsync(_pendingCode);
                    break;
                case "ok":
                    if (DataContext is MainViewModel vmOk)
                        vmOk.MermaidSyntaxError = string.Empty;
                    break;
                case "error":
                    if (DataContext is MainViewModel vmErr)
                        vmErr.MermaidSyntaxError = message ?? "";
                    break;
            }
        }
        catch
        {
            // Ongevormde post-messages negeren
        }
    }
}
