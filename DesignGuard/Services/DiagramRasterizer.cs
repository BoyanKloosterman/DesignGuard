using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfPath = System.Windows.Shapes.Path;
using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>Rasteriseert architectuurdiagram naar PNG voor PDF (zelfde elementen als Ontwerp-canvas).</summary>
public sealed class DiagramRasterizer
{
    private readonly DiagramLayoutService _layout;

    public DiagramRasterizer(DiagramLayoutService layout)
    {
        _layout = layout;
    }

    public byte[] RenderPng(ProjectModel project, double scale = 1.35)
    {
        var result = _layout.Layout(project);
        var w = Math.Max(400, result.ContentWidth * scale);
        var h = Math.Max(300, result.ContentHeight * scale);
        // Licht thema: leesbaar op witte PDF-pagina
        var canvas = new Canvas { Width = w, Height = h, Background = new SolidColorBrush(Color.FromRgb(238, 242, 246)) };

        foreach (var o in result.TrustOverlays)
        {
            var brush = (Brush)new BrushConverter().ConvertFromString(o.ColorHint)!;
            var b = new Border
            {
                Width = o.Width * scale,
                Height = o.Height * scale,
                BorderBrush = brush,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(20, 59, 91, 140))
            };
            Canvas.SetLeft(b, o.X * scale);
            Canvas.SetTop(b, o.Y * scale);
            canvas.Children.Add(b);
            var tb = new TextBlock
            {
                Text = o.Name,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                FontSize = 11 * Math.Min(scale, 1.25),
                Margin = new Thickness(6, 4, 0, 0)
            };
            Canvas.SetLeft(tb, o.X * scale + 6);
            Canvas.SetTop(tb, o.Y * scale + 4);
            canvas.Children.Add(tb);
        }

        var tf = new ScaleTransform(scale, scale);
        var lineBrush = new SolidColorBrush(Color.FromRgb(100, 116, 139));
        var headBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105));
        var arrowStroke = new SolidColorBrush(Color.FromRgb(51, 65, 85));

        foreach (var e in result.Edges)
        {
            canvas.Children.Add(new WpfPath
            {
                Data = Geometry.Parse(e.CurvePath),
                Stroke = lineBrush,
                StrokeThickness = 1,
                Fill = Brushes.Transparent,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                RenderTransform = tf
            });
            canvas.Children.Add(new WpfPath
            {
                Data = Geometry.Parse(e.ArrowPath),
                Fill = headBrush,
                Stroke = arrowStroke,
                StrokeThickness = 0.35,
                RenderTransform = tf
            });
        }

        var labelFg = new SolidColorBrush(Color.FromRgb(45, 55, 72));
        var nodeFill = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        var nodeBorder = new SolidColorBrush(Color.FromRgb(45, 55, 72));
        var tagMuted = new SolidColorBrush(Color.FromRgb(113, 128, 150));
        var sensSecondary = new SolidColorBrush(Color.FromRgb(100, 116, 139));
        var fsName = 12 * Math.Min(scale, 1.25);
        var fsTag = 10 * Math.Min(scale, 1.25);
        var fsSens = 9 * Math.Min(scale, 1.25);
        var pad = new Thickness(8 * scale, 6 * scale, 8 * scale, 6 * scale);
        var stripeW = 4 * scale;
        var stripeGap = 6 * scale;

        foreach (var n in result.Nodes)
        {
            var showSens = DesignOntwerpWaarden.IsDataSensitivityVisuallyElevated(n.StoresOrProcessesLabel);
            Brush stripeBrush;
            if (n.IsEntryPoint)
                stripeBrush = new SolidColorBrush(Color.FromRgb(56, 161, 105));
            else if (showSens)
                stripeBrush = new SolidColorBrush(Color.FromRgb(214, 158, 46));
            else
                stripeBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));

            var outer = new Border
            {
                Width = DiagramEdgeGeometry.NodeW * scale,
                Height = DiagramEdgeGeometry.NodeH * scale,
                Background = nodeFill,
                BorderBrush = nodeBorder,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = pad,
                SnapsToDevicePixels = true
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(stripeW) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var stripe = new Border
            {
                Background = stripeBrush,
                CornerRadius = new CornerRadius(2, 0, 0, 2),
                Margin = new Thickness(0, 0, stripeGap, 0),
                SnapsToDevicePixels = true
            };
            Grid.SetColumn(stripe, 0);

            var stack = new StackPanel();
            Grid.SetColumn(stack, 1);
            stack.Children.Add(new TextBlock
            {
                Text = n.Name,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                FontSize = fsName,
                Foreground = labelFg
            });
            stack.Children.Add(new TextBlock { Text = n.Tag, Foreground = tagMuted, FontSize = fsTag });
            if (showSens && !string.IsNullOrWhiteSpace(n.StoresOrProcessesLabel))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = n.StoresOrProcessesLabel,
                    FontSize = fsSens,
                    Foreground = sensSecondary
                });
            }

            grid.Children.Add(stripe);
            grid.Children.Add(stack);
            outer.Child = grid;

            Canvas.SetLeft(outer, n.X * scale);
            Canvas.SetTop(outer, n.Y * scale);
            canvas.Children.Add(outer);
        }

        var labelBg = new SolidColorBrush(Color.FromRgb(248, 250, 252));
        var labelBorder = new SolidColorBrush(Color.FromRgb(226, 232, 240));
        var fsLabel = 9 * Math.Min(scale, 1.25);

        foreach (var e in result.Edges)
        {
            if (string.IsNullOrWhiteSpace(e.Label)) continue;
            var bd = new Border
            {
                Background = labelBg,
                BorderBrush = labelBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 2, 4, 2),
                MaxWidth = 200 * scale,
                Child = new TextBlock
                {
                    Text = e.Label,
                    FontSize = fsLabel,
                    Foreground = labelFg,
                    TextWrapping = TextWrapping.Wrap
                }
            };
            bd.Measure(new Size(200 * scale, double.PositiveInfinity));
            Canvas.SetLeft(bd, e.LabelDrawLeft * scale);
            Canvas.SetTop(bd, e.LabelDrawTop * scale);
            canvas.Children.Add(bd);
        }

        canvas.Measure(new Size(w, h));
        canvas.Arrange(new Rect(0, 0, w, h));
        canvas.UpdateLayout();
        var bmp = new RenderTargetBitmap((int)Math.Ceiling(w), (int)Math.Ceiling(h), 96, 96, PixelFormats.Pbgra32);
        bmp.Render(canvas);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        using var ms = new MemoryStream();
        enc.Save(ms);
        return ms.ToArray();
    }
}
