using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using DesignGuard.Services;
using DesignGuard.ViewModels;
using Microsoft.Web.WebView2.Core;

namespace DesignGuard.Views;

/// <summary>Mermaid C4-preview (WebView2) + editor; los van het flowchart architectuurpaneel.</summary>
public partial class C4MermaidPanel : UserControl
{
    private readonly DispatcherDebouncer _renderDebounce = new(TimeSpan.FromMilliseconds(320));
    private bool _webViewReady;
    private bool _webViewInitStarted;
    private string _pendingCode = string.Empty;
    private INotifyPropertyChanged? _subscribedVm;

    public C4MermaidPanel()
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
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DesignGuard", "WebView2_C4");
            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await PreviewWebView.EnsureCoreWebView2Async(env);

            PreviewWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            PreviewWebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            PreviewWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            PreviewWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;

            PreviewWebView.NavigateToString(MermaidViewerHtmlLoader.Load());
        }
        catch (Exception ex)
        {
            if (DataContext is MainViewModel vm)
                vm.C4MermaidSyntaxError = "WebView2 kon niet starten: " + ex.Message;
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
        _webViewReady = true;
        var current = (DataContext as MainViewModel)?.C4MermaidCode ?? _pendingCode;
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

        if (DataContext is MainViewModel vm)
            ScheduleRender(vm.C4MermaidCode);
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
        if (e.PropertyName != nameof(MainViewModel.C4MermaidCode)) return;
        if (DataContext is not MainViewModel vm) return;
        ScheduleRender(vm.C4MermaidCode);
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
        var jsArg = JsonSerializer.Serialize(code ?? string.Empty);
        try
        {
            await PreviewWebView.CoreWebView2.ExecuteScriptAsync("window.renderMermaid(" + jsArg + ")");
        }
        catch (Exception ex)
        {
            if (DataContext is MainViewModel vm)
                vm.C4MermaidSyntaxError = "Render mislukt: " + ex.Message;
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            if (!WebView2MessageJson.TryParse(e.WebMessageAsJson, out var type, out var message))
                return;
            switch (type)
            {
                case "ready":
                    _webViewReady = true;
                    var currentCode = (DataContext as MainViewModel)?.C4MermaidCode ?? _pendingCode;
                    _pendingCode = currentCode ?? string.Empty;
                    _ = RenderAsync(_pendingCode);
                    break;
                case "ok":
                    if (DataContext is MainViewModel vmOk)
                        vmOk.C4MermaidSyntaxError = string.Empty;
                    break;
                case "error":
                    if (DataContext is MainViewModel vmErr)
                        vmErr.C4MermaidSyntaxError = message ?? "";
                    break;
            }
        }
        catch
        {
            // genegeerd
        }
    }
}
