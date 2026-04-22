using System.Globalization;

namespace DesignGuard.Services;

/// <summary>Curve + pijlkop apart (geen gecombineerde fill → geen dikke 'lint').</summary>
public static class DiagramEdgeGeometry
{
    public const double NodeW = 196;
    public const double NodeH = 64;

    /// <param name="lateralStart">Verticale shift exit-punt bron (uitwaaiers vanaf zelfde component).</param>
    /// <param name="lateralEnd">Verticale shift entry-punt doel (meerdere inkomende stromen).</param>
    /// <param name="labelT">Positie label langs curve (0=bron, 1=doel). Default 0.5 = midden.</param>
    public static (string CurvePath, string ArrowPath, double LabelX, double LabelY) Build(
        double fromX,
        double fromY,
        double toX,
        double toY,
        string? label,
        double lateralStart = 0,
        double lateralEnd = 0,
        double labelT = 0.5)
    {
        _ = label;
        var sx = fromX + NodeW;
        var sy = fromY + NodeH / 2 + lateralStart;
        var ex = toX;
        var ey = toY + NodeH / 2 + lateralEnd;
        // Iets minder agressieve bochten; cap op lange horizontale arm
        var dx = Math.Clamp(Math.Abs(ex - sx) * 0.36, 44, 120);
        var c1x = sx + dx;
        var c1y = sy;
        var c2x = ex - dx;
        var c2y = ey;
        var curvePath =
            $"M {F(sx)},{F(sy)} C {F(c1x)},{F(c1y)} {F(c2x)},{F(c2y)} {F(ex)},{F(ey)}";
        var dirLen = Math.Sqrt((ex - c2x) * (ex - c2x) + (ey - c2y) * (ey - c2y));
        if (dirLen < 0.001) dirLen = 1;
        var ux = (ex - c2x) / dirLen;
        var uy = (ey - c2y) / dirLen;
        const double tipInset = 10;
        const double halfW = 4;
        var backX = ex - ux * tipInset;
        var backY = ey - uy * tipInset;
        var leftX = backX - uy * halfW;
        var leftY = backY + ux * halfW;
        var rightX = backX + uy * halfW;
        var rightY = backY - ux * halfW;
        var arrowPath =
            $"M {F(leftX)},{F(leftY)} L {F(ex)},{F(ey)} L {F(rightX)},{F(rightY)} Z";

        // Evalueer punt op cubic Bezier met parameter t
        var t = Math.Clamp(labelT, 0.2, 0.8);
        var mt = 1 - t;
        var bx = mt * mt * mt * sx + 3 * mt * mt * t * c1x + 3 * mt * t * t * c2x + t * t * t * ex;
        var by = mt * mt * mt * sy + 3 * mt * mt * t * c1y + 3 * mt * t * t * c2y + t * t * t * ey;
        // Label iets boven de curve zelf, zodat de lijn er niet doorheen snijdt
        var labelX = bx;
        var labelY = by - 8;
        return (curvePath, arrowPath, labelX, labelY);
    }

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}
