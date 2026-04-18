using System.Text;
using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>Indicatieve koppeling aan referentiekaders — geen conformiteitsclaim.</summary>
public static class NormativeCoverageService
{
    public static string BuildMarkdownAppendix(ProjectModel project, IReadOnlyList<RequirementModel> requirements)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Normatieve dekking (indicatief)");
        sb.AppendLine();
        sb.AppendLine(
            "Onderstaande tabel is **richtinggevend**: samenvatting van bron-tags op gegenereerde eisen en typische aandachtsgebieden. " +
            "**Geen** claim op volledige OWASP ASVS-, NIST-, AVG- of CRA-dekking; gebruik primaire bronnen voor audits.");
        sb.AppendLine();

        var tagCounts = requirements
            .SelectMany(r => r.SourceTags)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .GroupBy(t => t.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Tag: g.Key, Count: g.Count()))
            .ToList();

        if (tagCounts.Count == 0)
        {
            sb.AppendLine("*Nog geen eisen met bron-tags — voer analyse opnieuw uit of voeg eisen toe.*");
            sb.AppendLine();
            return sb.ToString();
        }

        sb.AppendLine("| Bron-tag (richtinggevend) | Aantal eisen | Toelichting (inspiratie) |");
        sb.AppendLine("|---|---|---|");

        foreach (var (tag, count) in tagCounts)
        {
            var hint = tag.ToUpperInvariant() switch
            {
                "OWASP" => "OWASP ASVS / Top 10 — applicatiehardening, auth, input",
                "NIS2" => "NIS2-achtige eisen rond risico, logging, keten (EU-context)",
                "CRA" => "EU Cyber Resilience Act–achtige product- en update-eisen (inspiratie)",
                "AVG" or "GDPR" => "AVG/GDPR — privacy, minimalisatie, rechten betrokkenen",
                _ => "Raadpleeg je knowledge packs en primaire normteksten."
            };
            sb.AppendLine($"| {tag} | {count} | {hint} |");
        }

        sb.AppendLine();
        sb.AppendLine("### Systeemcontext in dit dossier");
        sb.AppendLine($"- Internetblootstelling: {(project.InternetExposed ? "ja" : "nee")}");
        sb.AppendLine($"- Persoonsgegevens: {(project.PersonalDataProcessed ? "ja" : "nee")}");
        sb.AppendLine($"- Admin / authenticatie: {(project.HasAdmin ? "admin ja" : "admin nee")} / {(project.HasAuthentication ? "auth ja" : "auth nee")}");
        sb.AppendLine();

        return sb.ToString();
    }
}
