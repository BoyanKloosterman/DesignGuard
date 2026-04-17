using System.Globalization;

namespace DesignGuard.Services;

/// <summary>Curve + pijlkop apart (geen gecombineerde fill → geen dikke ‘lint’).</summary>
public static class DiagramEdgeGeometry
{
    public const double NodeW = 196;
    public const double NodeH = 64;

    /// <param name="lateralStart">Verticale shift exit-punt bron (uitwaaiers vanafzelfde component).</param>
    /// <param name="lateralEnd">Verticale shift entry-punt doel (meerdere inkomende stromen).</param>
    public static (string CurvePath, string ArrowPath, double LabelX, double LabelY) Build(
        double fromX,
        double fromY,
        double toX,
        double toY,
        string? label,
        double lateralStart = 0,
        double lateralEnd = 0)
    {
        _ = label;
        var sx = fromX + NodeW;
        var sy = fromY + NodeH / 2 + lateralStart;
        var ex = toX;
        var ey = toY + NodeH / 2 + lateralEnd;
        // Iets minder agressieve bochten; cap op lange horizontale arm
        var dx = Math.Clamp(Math.Abs(ex - sx) * 0.38, 40, 100);
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
        var labelX = (sx + ex) / 2;
        var labelY = (sy + ey) / 2 - 16;
        return (curvePath, arrowPath, labelX, labelY);
    }

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}
