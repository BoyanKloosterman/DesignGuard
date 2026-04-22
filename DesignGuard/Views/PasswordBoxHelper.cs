using System.Windows;
using System.Windows.Controls;

namespace DesignGuard.Views;

/// <summary>Tweeweg-binding PasswordBox naar ViewModel (Password is geen DP).</summary>
public static class PasswordBoxHelper
{
    private static readonly DependencyProperty UpdatingPasswordProperty =
        DependencyProperty.RegisterAttached(
            "UpdatingPassword",
            typeof(bool),
            typeof(PasswordBoxHelper),
            new PropertyMetadata(false));

    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxHelper),
            new FrameworkPropertyMetadata(
                "",
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnBoundPasswordChanged));

    public static string GetBoundPassword(DependencyObject d) => (string)d.GetValue(BoundPasswordProperty);

    public static void SetBoundPassword(DependencyObject d, string value) => d.SetValue(BoundPasswordProperty, value);

    private static bool GetUpdatingPassword(PasswordBox pb) => (bool)pb.GetValue(UpdatingPasswordProperty);

    private static void SetUpdatingPassword(PasswordBox pb, bool value) => pb.SetValue(UpdatingPasswordProperty, value);

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox pb) return;
        pb.PasswordChanged -= PasswordBoxOnPasswordChanged;
        try
        {
            SetUpdatingPassword(pb, true);
            var n = e.NewValue as string ?? "";
            if (pb.Password != n) pb.Password = n;
        }
        finally
        {
            SetUpdatingPassword(pb, false);
            pb.PasswordChanged += PasswordBoxOnPasswordChanged;
        }
    }

    private static void PasswordBoxOnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox pb || GetUpdatingPassword(pb)) return;
        SetBoundPassword(pb, pb.Password);
    }
}
