using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>Koppelt eisen ruwweg aan dreigingen voor traceability (heuristiek).</summary>
public static class RequirementThreatLinker
{
    public static void Link(ProjectModel project)
    {
        foreach (var r in project.Requirements)
        {
            r.LinkedThreatIds.Clear();
            foreach (var t in project.Threats)
            {
                if (Matches(r, t))
                    r.LinkedThreatIds.Add(t.Id);
            }
        }
    }

    private static bool Matches(RequirementModel r, ThreatModel t)
    {
        var cat = r.Category.ToLowerInvariant();
        var title = t.Title.ToLowerInvariant();

        if (cat.Contains("authenticatie") || cat.Contains("session"))
        {
            if (title.Contains("identiteit") || title.Contains("login") || title.Contains("sessie") ||
                title.Contains("brute"))
                return true;
            if (t.StrideCategory == StrideCategory.Spoofing) return true;
        }

        if (cat.Contains("toegang") || cat.Contains("autorisatie"))
        {
            if (title.Contains("admin") || title.Contains("privilege") || title.Contains("autorisatie"))
                return true;
            if (t.StrideCategory == StrideCategory.ElevationOfPrivilege) return true;
        }

        if (cat.Contains("gegevens") || cat.Contains("privacy") || cat.Contains("bescherming"))
        {
            if (title.Contains("data") || title.Contains("persoon") || title.Contains("lek") ||
                t.StrideCategory == StrideCategory.InformationDisclosure)
                return true;
        }

        if (cat.Contains("logging") || cat.Contains("detectie"))
        {
            if (title.Contains("audit") || title.Contains("log") || title.Contains("ontkennen"))
                return true;
            if (t.StrideCategory == StrideCategory.Repudiation) return true;
        }

        if (cat.Contains("integratie"))
        {
            if (title.Contains("extern") || title.Contains("api") || title.Contains("vertrouwensgrens"))
                return true;
        }

        if (cat.Contains("applicatie") || cat.Contains("invoer"))
        {
            if (title.Contains("injectie") || title.Contains("upload") || title.Contains("invoer"))
                return true;
            if (t.StrideCategory == StrideCategory.Tampering) return true;
        }

        if (cat.Contains("beschikbaar"))
        {
            if (t.StrideCategory == StrideCategory.DenialOfService) return true;
        }

        if (cat.Contains("configuratie") || cat.Contains("leveringsketen"))
        {
            if (title.Contains("transport") || title.Contains("tls") || title.Contains("afhankelijk"))
                return true;
        }

        return false;
    }
}
