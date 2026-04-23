using System.Windows.Threading;

namespace DesignGuard.Services;

/// <summary>Eenvoudige dispatcher-debouncer voor UI-events zoals typen in een editor.</summary>
public sealed class DispatcherDebouncer
{
    private readonly DispatcherTimer _timer;
    private Action? _pending;

    public DispatcherDebouncer(TimeSpan delay, Dispatcher? dispatcher = null)
    {
        _timer = new DispatcherTimer(DispatcherPriority.Background,
            dispatcher ?? Dispatcher.CurrentDispatcher)
        {
            Interval = delay
        };
        _timer.Tick += OnTick;
    }

    /// <summary>Vervangt de vorige pending actie en (her)start de timer.</summary>
    public void Trigger(Action action)
    {
        _pending = action;
        _timer.Stop();
        _timer.Start();
    }

    /// <summary>Stopt de timer en voert een eventuele pending actie direct uit.</summary>
    public void FlushNow()
    {
        _timer.Stop();
        var a = _pending;
        _pending = null;
        a?.Invoke();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        var a = _pending;
        _pending = null;
        a?.Invoke();
    }
}
