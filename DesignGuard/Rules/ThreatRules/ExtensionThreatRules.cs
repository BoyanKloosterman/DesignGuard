using DesignGuard.Models;

namespace DesignGuard.Rules.ThreatRules;

public sealed class TrustBoundaryCrossingThreatRule : IThreatRule
{
    public IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.HasTrustBoundaryCrossing) yield break;

        yield return new ThreatModel
        {
            Title = "Misbruik van data die trust boundaries kruist",
            StrideCategory = StrideCategory.Tampering,
            Severity = SeverityEstimate.High,
            Description =
                "Datastromen tussen vertrouwenszones kunnen onterecht worden gewijzigd of afgeluisterd als grenzen niet strikt worden afgedwongen.",
            AffectedComponents = ctx.Project.Components.Select(c => c.Name).Take(6).ToList(),
            AffectedAssets = ctx.Project.Assets.Select(a => a.Name).Take(4).ToList(),
            GenerationReason =
                "Er is minstens één datastroom tussen componenten in verschillende trust boundaries.",
            SuggestedMitigations = new List<string>
            {
                "Schema-validatie en authenticatie op iedere grens",
                "Geen vertrouwelijke payloads in onbeveiligde tussenlagen",
                "Expliciet threat model per boundary (ingress/egress)"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Gegevens passeren een grens tussen meer en minder vertrouwde delen.",
                WhyItMatters = "Hier gaan architectuurfouten vaak mis: te veel vertrouwen in 'intern' verkeer.",
                WhyIncluded = "Je model kruist trust boundaries volgens de component-koppelingen."
            }
        };
    }
}

public sealed class InternetExposureThreatRule : IThreatRule
{
    public IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.Project.InternetExposed) yield break;

        yield return new ThreatModel
        {
            Title = "Grotere aanvalsoppervlakte door internetblootstelling",
            StrideCategory = StrideCategory.DenialOfService,
            Severity = ctx.InternetFacingHighRisk ? SeverityEstimate.High : SeverityEstimate.Medium,
            Description =
                "Publiek bereikbare interfaces worden continu gescand op bekende kwetsbaarheden en configuratiefouten.",
            AffectedComponents = ctx.Project.Components
                .Where(c => c.IsEntryPoint || TagLike(c.Tag, "frontend", "api", "gateway"))
                .Select(c => c.Name).DefaultIfEmpty("Publieke interface").Take(5).ToList(),
            GenerationReason = "Het systeem is als internetblootgesteld gemarkeerd.",
            SuggestedMitigations = new List<string>
            {
                "Minimaal open poorten en services",
                "WAF / rate limits / bot-bescherming waar passend",
                "Hardening-baseline en vulnerability management"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Iedereen op internet kan je entry points proberen.",
                WhyItMatters = "Zelfs kleine bugs worden sneller gevonden en misbruikt.",
                WhyIncluded = "Je gaf aan dat het systeem internetblootstelling heeft."
            }
        };
    }

    private static bool TagLike(string tag, params string[] needles)
    {
        if (string.IsNullOrWhiteSpace(tag)) return false;
        return needles.Any(n => tag.Contains(n, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class MissingLoggingThreatRule : IThreatRule
{
    public IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx)
    {
        if (ctx.Project.LoggingMonitoringPresent) yield break;
        if (!ctx.Project.HasAuthentication && !ctx.HasAdminSurface) yield break;

        yield return new ThreatModel
        {
            Title = "Blinde vlekken bij detectie (logging/monitoring onvoldoende)",
            StrideCategory = StrideCategory.Repudiation,
            Severity = SeverityEstimate.Medium,
            Description =
                "Zonder betrouwbare telemetrie worden aanvallen en misconfiguraties laat of niet ontdekt.",
            AffectedComponents = new List<string> { "Operatie / platform" },
            GenerationReason =
                "Logging/monitoring is uitgeschakeld of ontbreekt volgens je wizard-keuze, terwijl er wel accounts of admin zijn.",
            SuggestedMitigations = new List<string>
            {
                "Security events naar centrale log / SIEM",
                "Alerts op admin-acties en authenticatie-anomalieën",
                "Retentie en toegang tot logs afschermen"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Je ziet niet tijdig wat er misgaat.",
                WhyItMatters = "Detectie is nodig om impact te beperken en te leren van incidenten.",
                WhyIncluded = "Je markeerde logging/monitoring als afwezig of beperkt."
            }
        };
    }
}

public sealed class BusinessCriticalThreatRule : IThreatRule
{
    public IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.Project.CriticalBusinessFunction) yield break;

        yield return new ThreatModel
        {
            Title = "Ransomware of sabotage op bedrijfskritisch proces",
            StrideCategory = StrideCategory.DenialOfService,
            Severity = SeverityEstimate.High,
            Description =
                "Uitval of versleuteling van kernfunctionaliteit heeft disproportionele impact op de organisatie.",
            AffectedComponents = ctx.Project.Components.Select(c => c.Name).Take(5).ToList(),
            GenerationReason = "Je markeerde de functionaliteit als bedrijfskritisch.",
            SuggestedMitigations = new List<string>
            {
                "Back-ups en herstel testen (RTO/RPO)",
                "Offline of air-gapped kopieën waar nodig",
                "Segmentatie zodat één fout niet alles platlegt"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Als dit stuk uitvalt, staat de business stil.",
                WhyItMatters = "Aanvallers richten zich graag op het maximale effect.",
                WhyIncluded = "Bedrijfskritische scope verhoogt de prioriteit van beschikbaarheid."
            }
        };
    }
}
