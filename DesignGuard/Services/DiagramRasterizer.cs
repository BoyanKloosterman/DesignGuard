using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfPath = System.Windows.Shapes.Path;
using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>Rasteriseert diagram naar PNG voor PDF (los van interactieve view).</summary>
public sealed class DiagramRasterizer
{
    private readonly DiagramLayoutService _layout;

    public DiagramRasterizer(DiagramLayoutService layout)
    {
        _layout = layout;
    }

    public byte[] RenderPng(ProjectModel project, double scale = 1.2)
    {
        var result = _layout.Layout(project);
        var w = Math.Max(400, result.ContentWidth * scale);
        var h = Math.Max(300, result.ContentHeight * scale);
        var canvas = new Canvas { Width = w, Height = h, Background = new SolidColorBrush(Color.FromRgb(248, 249, 251)) };

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
                Foreground = Brushes.DimGray,
                FontSize = 11,
                Margin = new Thickness(6, 4, 0, 0)
            };
            Canvas.SetLeft(tb, o.X * scale + 6);
            Canvas.SetTop(tb, o.Y * scale + 4);
            canvas.Children.Add(tb);
        }

        foreach (var e in result.Edges)
        {
            var lineBrush = new SolidColorBrush(Color.FromRgb(100, 116, 139));
            var headBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105));
            var tf = new ScaleTransform(scale, scale);
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
                Stroke = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                StrokeThickness = 0.35,
                RenderTransform = tf
            });
        }

        foreach (var n in result.Nodes)
        {
            var border = new Border
            {
                Width = DiagramEdgeGeometry.NodeW * scale,
                Height = DiagramEdgeGeometry.NodeH * scale,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(43, 55, 72)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8)
            };
            border.Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = n.Name, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap,
                        FontSize = 12
                    },
                    new TextBlock { Text = n.Tag, Foreground = Brushes.Gray, FontSize = 10 }
                }
            };
            Canvas.SetLeft(border, n.X * scale);
            Canvas.SetTop(border, n.Y * scale);
            canvas.Children.Add(border);
        }

        canvas.Measure(new Size(w, h));
        canvas.Arrange(new Rect(0, 0, w, h));
        var bmp = new RenderTargetBitmap((int)Math.Ceiling(w), (int)Math.Ceiling(h), 96, 96, PixelFormats.Pbgra32);
        bmp.Render(canvas);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        using var ms = new MemoryStream();
        enc.Save(ms);
        return ms.ToArray();
    }
}
