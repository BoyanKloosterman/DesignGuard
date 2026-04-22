using System.Globalization;

namespace DesignGuard.Services;

/// <summary>Curve + pijlkop apart (geen gecombineerde fill → geen dikke 'lint').</summary>
public static class DiagramEdgeGeometry
{
    public const double NodeW = 196;
    public const double NodeH = 64;

    /// <param name="lateralStart">Verschuiving exit-punt langs de randzijde (spreiding bij fan-out).</param>
    /// <param name="lateralEnd">Verschuiving entry-punt langs de randzijde (spreiding bij fan-in).</param>
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
        // Centra van bron- en doelknoop
        var sCx = fromX + NodeW / 2;
        var sCy = fromY + NodeH / 2;
        var tCx = toX + NodeW / 2;
        var tCy = toY + NodeH / 2;
        var ddx = tCx - sCx;
        var ddy = tCy - sCy;

        // Kies uitgaande/inkomende zijde op basis van dominante richting → pijl wijst altijd
        // naar het andere component, geen rare lussen bij backward of verticale edges.
        double sx, sy, ex, ey;
        double sNx, sNy; // normaalvector bij de bron (richting waarin we de knoop verlaten)
        double eNx, eNy; // normaalvector bij het doel (richting vanaf doel naar buiten)
        if (Math.Abs(ddx) >= Math.Abs(ddy))
        {
            // Oost/West aanhechting
            if (ddx >= 0)
            {
                sx = fromX + NodeW; sy = sCy + lateralStart; sNx = 1; sNy = 0;
                ex = toX; ey = tCy + lateralEnd; eNx = -1; eNy = 0;
            }
            else
            {
                sx = fromX; sy = sCy + lateralStart; sNx = -1; sNy = 0;
                ex = toX + NodeW; ey = tCy + lateralEnd; eNx = 1; eNy = 0;
            }
        }
        else
        {
            // Noord/Zuid aanhechting (lateral-offset wordt dan horizontaal toegepast)
            if (ddy >= 0)
            {
                sx = sCx + lateralStart; sy = fromY + NodeH; sNx = 0; sNy = 1;
                ex = tCx + lateralEnd; ey = toY; eNx = 0; eNy = -1;
            }
            else
            {
                sx = sCx + lateralStart; sy = fromY; sNx = 0; sNy = -1;
                ex = tCx + lateralEnd; ey = toY + NodeH; eNx = 0; eNy = 1;
            }
        }

        // Controlepunten langs de normaal: curve start/eind loodrecht op de randzijde,
        // lengte schaalt met de afstand tussen knopen zodat korte edges niet over-bochten.
        var dist = Math.Sqrt(ddx * ddx + ddy * ddy);
        var cd = Math.Clamp(dist * 0.38, 48, 140);
        var c1x = sx + sNx * cd;
        var c1y = sy + sNy * cd;
        var c2x = ex + eNx * cd;
        var c2y = ey + eNy * cd;

        var curvePath =
            $"M {F(sx)},{F(sy)} C {F(c1x)},{F(c1y)} {F(c2x)},{F(c2y)} {F(ex)},{F(ey)}";

        // Pijlkop: richting = tangent van curve bij eindpunt (c2 → eindpunt)
        var tipDx = ex - c2x;
        var tipDy = ey - c2y;
        var dirLen = Math.Sqrt(tipDx * tipDx + tipDy * tipDy);
        if (dirLen < 0.001) dirLen = 1;
        var ux = tipDx / dirLen;
        var uy = tipDy / dirLen;
        const double tipInset = 10;
        const double arrowHalfW = 4;
        var backX = ex - ux * tipInset;
        var backY = ey - uy * tipInset;
        var leftX = backX - uy * arrowHalfW;
        var leftY = backY + ux * arrowHalfW;
        var rightX = backX + uy * arrowHalfW;
        var rightY = backY - ux * arrowHalfW;
        var arrowPath =
            $"M {F(leftX)},{F(leftY)} L {F(ex)},{F(ey)} L {F(rightX)},{F(rightY)} Z";

        // Label-positie via echte cubic Bezier-evaluatie bij parameter t
        var t = Math.Clamp(labelT, 0.2, 0.8);
        var mt = 1 - t;
        var bx = mt * mt * mt * sx + 3 * mt * mt * t * c1x + 3 * mt * t * t * c2x + t * t * t * ex;
        var by = mt * mt * mt * sy + 3 * mt * mt * t * c1y + 3 * mt * t * t * c2y + t * t * t * ey;
        var labelX = bx;
        var labelY = by - 8;
        return (curvePath, arrowPath, labelX, labelY);
    }

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}
