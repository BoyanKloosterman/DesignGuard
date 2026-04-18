using DesignGuard.Models;

namespace DesignGuard.Rules.ThreatRules;

/// <summary>Extra STRIDE-scenario's voor secrets en supply chain.</summary>
public sealed class OperationalSecretsThreatRule : IThreatRule
{
    public IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.HasExternalService && !ctx.HasDatabase && !ctx.EffectiveSensitiveStorage) yield break;

        yield return new ThreatModel
        {
            Title = "Lekkage of misbruik van operationele geheimen (API-keys, connection strings)",
            StrideCategory = StrideCategory.InformationDisclosure,
            Severity = ctx.Project.InternetExposed ? SeverityEstimate.High : SeverityEstimate.Medium,
            Description =
                "Geheimen in code, logs of config kunnen worden uitgelezen; gestolen sleutels geven direct toegang tot data of externe diensten.",
            AffectedComponents = ctx.NamesOfDatabaseComponents().Concat(ctx.NamesOfExternalishComponents())
                .Distinct(StringComparer.OrdinalIgnoreCase).DefaultIfEmpty("Config / secrets").Take(5).ToList(),
            GenerationReason =
                "Database, externe integratie of gevoelige opslag staat aan — typische geheimen-risico's.",
            SuggestedMitigations = new List<string>
            {
                "Secret manager / vault, geen secrets in repo",
                "Rotatie en least-privilege scopes op API-keys",
                "Scannen op hardcoded secrets in CI"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Wachtwoorden en sleutels liggen op de verkeerde plek of zijn te breed.",
                WhyItMatters = "Eén gelekte sleutel kan databases en externe accounts openzetten.",
                WhyIncluded = "Je model bevat DB, externe koppelingen of gevoelige opslag."
            }
        };
    }
}

public sealed class SupplyChainPipelineThreatRule : IThreatRule
{
    public IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.Project.CriticalBusinessFunction && !ctx.HasExternalService) yield break;

        yield return new ThreatModel
        {
            Title = "Aanval via build-pijplijn of afhankelijkheden (supply chain)",
            StrideCategory = StrideCategory.Tampering,
            Severity = ctx.Project.CriticalBusinessFunction ? SeverityEstimate.High : SeverityEstimate.Medium,
            Description =
                "Gecompromitteerde packages, build-stappen of artifacts kunnen kwaadaardige code in productie brengen zonder dat de applicatie zelf direct wordt aangevallen.",
            AffectedComponents = new List<string> { "CI/CD", "Artifact registry", "Dependencies" },
            GenerationReason =
                "Bedrijfskritische scope of externe integraties verhogen het belang van integriteit van build en dependencies.",
            SuggestedMitigations = new List<string>
            {
                "Pin/lock dependencies, SBOM en vulnerability scanning",
                "Getekende builds, beschermde pijplijn-secrets",
                "Review van third-party updates vóór productie"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "De leverketen van software (packages, builds) wordt een aanvalsoppervlak.",
                WhyItMatters = "Eén kwaadaardige dependency kan alle klanten raken.",
                WhyIncluded = "Kritieke business of externe keten vraagt expliciet supply-chain-denken."
            }
        };
    }
}
