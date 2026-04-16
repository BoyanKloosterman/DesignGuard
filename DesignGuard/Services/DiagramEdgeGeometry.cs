using System.Globalization;

namespace DesignGuard.Services;

/// <summary>Padstring met pijl voor datastromen (WPF Path.Data).</summary>
public static class DiagramEdgeGeometry
{
    public const double NodeW = 196;
    public const double NodeH = 64;

    public static (string PathData, double LabelX, double LabelY) Build(
        double fromX,
        double fromY,
        double toX,
        double toY,
        string? label)
    {
        _ = label;
        var sx = fromX + NodeW;
        var sy = fromY + NodeH / 2;
        var ex = toX;
        var ey = toY + NodeH / 2;
        var dx = Math.Max(48, Math.Abs(ex - sx) * 0.45);
        var c1x = sx + dx;
        var c1y = sy;
        var c2x = ex - dx;
        var c2y = ey;
        var path =
            $"M {F(sx)},{F(sy)} C {F(c1x)},{F(c1y)} {F(c2x)},{F(c2y)} {F(ex)},{F(ey)}";
        var dirLen = Math.Sqrt((ex - c2x) * (ex - c2x) + (ey - c2y) * (ey - c2y));
        if (dirLen < 0.001) dirLen = 1;
        var ux = (ex - c2x) / dirLen;
        var uy = (ey - c2y) / dirLen;
        var backX = ex - ux * 12;
        var backY = ey - uy * 12;
        var leftX = backX - uy * 5;
        var leftY = backY + ux * 5;
        var rightX = backX + uy * 5;
        var rightY = backY - ux * 5;
        var arrow =
            $" M {F(leftX)},{F(leftY)} L {F(ex)},{F(ey)} L {F(rightX)},{F(rightY)} Z";
        var labelX = (sx + ex) / 2;
        var labelY = (sy + ey) / 2 - 14;
        return (path + arrow, labelX, labelY);
    }

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}
