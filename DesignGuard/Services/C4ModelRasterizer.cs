using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DesignGuard.Export;
using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>Zelfde C4-band/kaart-layout als de app, gerasterd naar PNG voor PDF (alleen UI-thread).</summary>
public sealed class C4ModelRasterizer
{
    private const double ContentWidth = 920;

    public byte[] RenderPng(ProjectModel project, IReadOnlyList<ThreatModel> threats)
    {
        var idToName = C4ExportPresentation.BuildIdToNameMap(project.C4Elements);

        var root = new StackPanel
        {
            Width = ContentWidth,
            Background = Brushes.White,
            UseLayoutRounding = true
        };

        root.Children.Add(new TextBlock
        {
            Text = "C4-overzicht",
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10)
        });

        if (project.C4Elements.Count == 0)
        {
            root.Children.Add(new TextBlock
            {
                Text = "Geen C4-elementen vastgelegd in dit dossier.",
                Foreground = Brushes.Gray,
                FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap
            });
        }
        else
        {
            foreach (var band in C4Bands)
            {
                var els = project.C4Elements
                    .Where(e => e.Level == band.Level)
                    .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (els.Count == 0) continue;

                var outer = new Border
                {
                    Background = band.Background,
                    BorderBrush = band.Border,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var sp = new StackPanel();
                sp.Children.Add(new TextBlock
                {
                    Text = C4LevelFormatting.ShortLabel(band.Level),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = band.TitleBrush,
                    FontSize = 13
                });

                var wrap = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
                foreach (var el in els)
                    wrap.Children.Add(BuildCard(el, idToName, threats));

                sp.Children.Add(wrap);
                outer.Child = sp;
                root.Children.Add(outer);
            }
        }

        root.Measure(new Size(ContentWidth, double.PositiveInfinity));
        root.Arrange(new Rect(0, 0, ContentWidth, root.DesiredSize.Height));
        var h = Math.Max(40, Math.Ceiling(root.DesiredSize.Height));

        var bmp = new RenderTargetBitmap(
            (int)Math.Ceiling(ContentWidth),
            (int)h,
            96,
            96,
            PixelFormats.Pbgra32);
        bmp.Render(root);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        using var ms = new MemoryStream();
        enc.Save(ms);
        return ms.ToArray();
    }

    private static Border BuildCard(
        C4ElementModel el,
        IReadOnlyDictionary<int, string> idToName,
        IReadOnlyList<ThreatModel> threats)
    {
        var card = new Border
        {
            MinWidth = 150,
            MaxWidth = 260,
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 10, 10)
        };

        var inner = new StackPanel();
        inner.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(el.Name) ? "(geen naam)" : el.Name,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        });

        if (!string.IsNullOrWhiteSpace(el.Description))
        {
            inner.Children.Add(new TextBlock
            {
                Text = el.Description,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(86, 101, 115)),
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 48,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        }

        if (!string.IsNullOrWhiteSpace(el.Technology))
        {
            inner.Children.Add(new TextBlock
            {
                Text = el.Technology,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        var parentLine = C4ExportPresentation.FormatC4ParentLabelCard(el, idToName);
        if (!string.IsNullOrEmpty(parentLine))
        {
            inner.Children.Add(new TextBlock
            {
                Text = parentLine,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(108, 52, 131)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        var openN = C4ExportPresentation.CountOpenThreatNameMatches(el, threats);
        if (openN > 0)
        {
            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(211, 84, 0)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(5, 2, 5, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 6, 0, 0)
            };
            badge.Child = new TextBlock
            {
                Text = $"Open dreig.: {openN}",
                Foreground = Brushes.White,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold
            };
            inner.Children.Add(badge);
        }

        card.Child = inner;
        return card;
    }

    private readonly record struct BandStyle(
        C4Level Level,
        Brush Background,
        Brush Border,
        Brush TitleBrush);

    private static readonly BandStyle[] C4Bands =
    {
        new(C4Level.Context,
            new SolidColorBrush(Color.FromRgb(232, 244, 252)),
            new SolidColorBrush(Color.FromRgb(52, 152, 219)),
            new SolidColorBrush(Color.FromRgb(26, 82, 118))),
        new(C4Level.Container,
            new SolidColorBrush(Color.FromRgb(233, 247, 239)),
            new SolidColorBrush(Color.FromRgb(39, 174, 96)),
            new SolidColorBrush(Color.FromRgb(20, 90, 50))),
        new(C4Level.Component,
            new SolidColorBrush(Color.FromRgb(254, 249, 231)),
            new SolidColorBrush(Color.FromRgb(243, 156, 18)),
            new SolidColorBrush(Color.FromRgb(125, 102, 8))),
        new(C4Level.Code,
            new SolidColorBrush(Color.FromRgb(253, 237, 236)),
            new SolidColorBrush(Color.FromRgb(231, 76, 60)),
            new SolidColorBrush(Color.FromRgb(120, 40, 31)))
    };
}
